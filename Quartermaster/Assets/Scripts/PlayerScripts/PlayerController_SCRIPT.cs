// Code is inspired from Unity's 3D FPS template
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(Health))]
public class PlayerController : NetworkBehaviour {
    #region Variables
    
    [Header("Required Components")]
    private CharacterController Controller;
    private PlayerInputHandler InputHandler;
    private PlayerInput PlayerInput;
    private Health health;

    [Header("Player Spawn Settings")]
    [SerializeField] private Transform spawnLocation;

    [Header("Movement")]
    private Vector3 worldspaceMove = Vector3.zero;
    public const float k_GravityForce = 20f;

    [Tooltip("Speed multiplier when holding sprint key")]
    public const float k_SprintSpeedModifier = 2f;
    public const float k_CrouchSpeedModifier = 0.5f;
    private float speedModifier = 1f;
    public float groundSpeed = 5f;
    public float maxAirSpeed = 7.5f;
    public float airAcceleration = 15f;
    public float minSlideSpeed = 0.5f;
    public float slideDeceleration = 5f;

    [Tooltip("Sharpness affects acceleration/deceleration. Low values mean slow acceleration/deceleration and vice versa")]
    public float groundSharpness = 15f;
    private Vector3 playerVelocity;

    [Header("Looking")]
    public Camera PlayerCamera;
    private Vector3 cameraOffset = new Vector3(0f, 0f, 0.36f);
    [SerializeField] private bool invertVerticalInput;
    [SerializeField] private bool invertHorizontalInput;
    [SerializeField] private float vertical_sens = 2f;
    [SerializeField] private float horizontal_sens = 2f;
    private float cameraVerticalAngle = 0f;
    private float rotationMultiplier = 1f;
    private float CameraHeightRatio = 0.8f;

    [Header("Ground Based Interactions")]
    [SerializeField] private LayerMask GroundLayers = 3;
    public float jumpForce = 5f;
    private bool IsGrounded = true;
    private float lastTimeJumped;
    private Vector3 lastImpactSpeed = Vector3.zero;
    private const float k_JumpGroundingPreventionTime = 0.2f;
    private const float k_GroundCheckDistance = 0.07f;
    private const float k_GroundCheckDistanceInAir = 0.1f;
    private Vector3 GroundNormal = Vector3.up;

    [Header("State Machine")]
    private StateMachine stateMachine;

    [Header("Crouching")]
    [SerializeField] private Transform visualTransform;
    private float targetHeight;
    private float CapsuleHeightStanding = 2f;
    private float CapsuleHeightCrouching = 1f;
    [SerializeField] private float crouchingSharpness = 10f;
    private bool IsCrouched = false;

    #endregion

    #region Start Up Functions
    private void EnablePlayerControls() {
        // Camera and Audio Listener
        if (PlayerCamera != null) {
            PlayerCamera.gameObject.SetActive(true);
            PlayerCamera.GetComponent<AudioListener>().enabled = true;
        }

        // Player Input
        if (PlayerInput != null) { PlayerInput.enabled = true; }

    }

    private void DisablePlayerControls() {
        // Camera and Audio Listener
        if (PlayerCamera != null) {
            PlayerCamera.gameObject.SetActive(false);
            PlayerCamera.GetComponent<AudioListener>().enabled = false;
        }

        // Player Input
        if (PlayerInput != null) {
            PlayerInput.enabled = false;
        }
    }

    private void InitializeStateMachine() {
        stateMachine = new StateMachine();

        WalkState walkState = new WalkState(this);
        AirborneState airborneState = new AirborneState(this);
        SlideState slideState = new SlideState(this);
        SprintState sprintState = new SprintState(this);
        CrouchState crouchState = new CrouchState(this);

        // Transitions
        Any(airborneState, new FuncPredicate(() => !IsGrounded));
        At(airborneState, walkState, new FuncPredicate(() => IsGrounded));

        At(walkState, sprintState, new FuncPredicate(() => InputHandler != null && InputHandler.isSprinting));
        At(walkState, crouchState, new FuncPredicate(() => IsCrouched));

        At(sprintState, walkState, new FuncPredicate(() => InputHandler != null && !InputHandler.isSprinting));
        At(sprintState, slideState, new FuncPredicate(() => IsCrouched));

        At(slideState, walkState, new FuncPredicate(() => !IsCrouched));
        At(slideState, crouchState, new FuncPredicate(() => !InputHandler.isCrouching && IsCrouched));
        At(slideState, crouchState, new FuncPredicate(() => playerVelocity.sqrMagnitude < minSlideSpeed));

        At(crouchState, walkState, new FuncPredicate(() => !IsCrouched));

        // Default state
        stateMachine.SetState(walkState);
    }

    #endregion

    #region Unity Functions
    void Start() {
        // Initialize default values
        lastTimeJumped = Time.time;
        targetHeight = CapsuleHeightStanding;
    }

    public override void OnNetworkSpawn() {
        if (!IsOwner) {
            DisablePlayerControls();
            return;
        }

        Controller = GetComponent<CharacterController>();
        InputHandler = GetComponent<PlayerInputHandler>();
        PlayerInput = GetComponent<PlayerInput>();
        health = GetComponent<Health>();

        EnablePlayerControls();

        if (health != null) {
            health.OnDie += OnDie;
            health.OnDamaged += OnDamaged;
            health.OnHealed += OnHealed;
        }

        InitializeStateMachine();
        UpdateHeight(true);
    }

    void Update() {
        if (!IsOwner) return;

        // If player falls too far, kill them
        if (transform.position.y <= -25f) {
            if (health != null) { health.Kill(); }
        }

        GroundCheck();

        if (InputHandler != null) {
            worldspaceMove = transform.TransformVector(InputHandler.move_vector);
            SetCrouchingState(InputHandler.isCrouching, false);
        }

        UpdateHeight(false);

        MovePlayer();

        HandleLook();

        if (stateMachine != null) { stateMachine.Update(); }
    }

    void FixedUpdate() {
        if (!IsOwner) return;
        MovePlayer();
    }

    #endregion

    #region Movement Functions
    void HandleLook() {
        transform.Rotate(
            new Vector3(
                0f,
                InputHandler.GetHorizontalLook() * horizontal_sens * rotationMultiplier * (invertHorizontalInput ? -1 : 1),
                0f
            ),
            Space.Self
        );

        cameraVerticalAngle += InputHandler.GetVerticalLook() * vertical_sens * rotationMultiplier * (invertVerticalInput ? -1 : 1);
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -89f, 89f);
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
    }

    public void HandleGroundMovement() {
        Vector3 targetVelocity = worldspaceMove * groundSpeed * speedModifier;
        targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, GroundNormal) * targetVelocity.magnitude;
        playerVelocity = Vector3.Lerp(playerVelocity, targetVelocity, groundSharpness * Time.deltaTime);

        JumpCheck();
        MovePlayer();
    }

    public void HandleSlideMovement() {
        playerVelocity -= playerVelocity * slideDeceleration * Time.deltaTime;
        JumpCheck();
        MovePlayer();
    }

    public void HandleAirMovement() {
        playerVelocity += worldspaceMove * airAcceleration * Time.deltaTime;
        float verticalVelocity = playerVelocity.y;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed * speedModifier);
        playerVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        playerVelocity += Vector3.down * k_GravityForce * Time.deltaTime;
        MovePlayer();
    }

    void MovePlayer() {
        if (Controller == null) return;

        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(Controller.height);

        Controller.Move(playerVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        lastImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, Controller.radius,
            playerVelocity.normalized, out RaycastHit hit, playerVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            lastImpactSpeed = playerVelocity;

            // Project our velocity on the plane defined by the hit normal
            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, hit.normal);
        }
    }

    void JumpCheck() {
        if (IsGrounded && InputHandler != null && InputHandler.jumped) {
            playerVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
            playerVelocity += Vector3.up * jumpForce;
            lastTimeJumped = Time.time;
            IsGrounded = false;
            GroundNormal = Vector3.up;
        }
    }

    void GroundCheck() {
        // Make sure that the ground check distance while already in air is very small, to prevent snapping to ground
        float chosenGroundCheckDistance =
            IsGrounded ? (Controller.skinWidth + k_GroundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        IsGrounded = false;
        GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short time since last jump
        if (Time.time >= lastTimeJumped + k_JumpGroundingPreventionTime) {
            // if we're grounded, collect info about the ground normal with a downward capsule cast
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(Controller.height),
                                    Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance,
                                    GroundLayers, QueryTriggerInteraction.Ignore))
            {
                GroundNormal = hit.normal;
                // Only consider valid ground if the normal is mostly up and slope angle is lower than limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(GroundNormal)) {
                    IsGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > Controller.skinWidth) {
                        Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    void UpdateHeight(bool force) {
        if (Controller == null || PlayerCamera == null || visualTransform == null) return;

        if (force) {
            Controller.height = targetHeight;
            Controller.center = Vector3.up * (Controller.height - CapsuleHeightStanding) / CapsuleHeightStanding;
            PlayerCamera.transform.localPosition = cameraOffset + Vector3.up * ((targetHeight * CameraHeightRatio) - 1);
            visualTransform.localPosition = Controller.center;
            visualTransform.localScale = new Vector3(1f, Controller.height / CapsuleHeightStanding, 1f);
        } else if (Controller.height != targetHeight) {
            if (Mathf.Abs(Controller.height - targetHeight) < 0.0001f) {
                UpdateHeight(true);
                return;
            }

            // Smoothly transition to target height
            Controller.height = Mathf.Lerp(Controller.height, targetHeight, crouchingSharpness * Time.deltaTime);
            Controller.center = Vector3.up * (Controller.height - CapsuleHeightStanding) / CapsuleHeightStanding;
            visualTransform.localPosition = Controller.center;
            visualTransform.localScale = new Vector3(1f, Controller.height / CapsuleHeightStanding, 1f);
            PlayerCamera.transform.localPosition = Vector3.Lerp(
                PlayerCamera.transform.localPosition,
                cameraOffset + Vector3.up * ((targetHeight * CameraHeightRatio) - 1),
                crouchingSharpness * Time.deltaTime
            );
        }
    }

    bool SetCrouchingState(bool crouched, bool ignoreObstructions) {
        if (IsCrouched == crouched) return true;

        if (crouched) {
            targetHeight = CapsuleHeightCrouching;
        } else {
            if (!ignoreObstructions) {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(CapsuleHeightStanding),
                    Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore
                );

                foreach (Collider c in standingOverlaps) {
                    if (!c.transform.root.CompareTag("Player")) {
                        Debug.Log($"[{Time.time}] Cannot stand: obstruction in the way.");
                        return false;
                    }
                }
            }

            targetHeight = CapsuleHeightStanding;
        }

        IsCrouched = crouched;
        return true;
    }

    void OnDie() {
        Debug.Log($"[{Time.time}] {gameObject.name} died. Respawning...");
        if (health != null) health.Invincible = true;

        playerVelocity = Vector3.zero;
        targetHeight = CapsuleHeightStanding;
        disableCharacterController();
        transform.position = Vector3.zero;
        enableCharacterController();

        if (health != null) {
            health.HealServerRpc(1000f);
            health.Invincible = false;
        }
    }

    void OnDamaged(float damage, GameObject damageSource) {
        Debug.Log($"[{Time.time}] {gameObject.name} took {damage} damage. Health Ratio: {health.GetRatio()}");
    }

    void OnHealed(float healAmount) {
        Debug.Log($"[{Time.time}] {gameObject.name} healed for {healAmount} health. Health Ratio: {health.GetRatio()}");
    }

    #endregion

    #region Helper Functions

    // public bool toggleCharacterController() {
    //     if (Controller != null) {
    //         Controller.enabled = !Controller.enabled;
    //         return Controller.enabled;
    //     }

    //     return false; // false if null.
    // }

    public void disableCharacterController(){
        if (Controller != null) {
            Controller.enabled = false;
        }
    }

    public void enableCharacterController(){
        if (Controller != null) {
            Controller.enabled = true;
        }
    }

    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal) {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    bool IsNormalUnderSlopeLimit(Vector3 normal) {
        return Vector3.Angle(transform.up, normal) <= Controller.slopeLimit;
    }

    Vector3 GetCapsuleBottomHemisphere() {
        return transform.position + (transform.up * (Controller.center.y - Mathf.Max(Controller.height / 2, Controller.radius) + Controller.radius));
    }

    Vector3 GetCapsuleTopHemisphere(float atHeight) {
        // Controller.center depends on height, so we recalc a "virtual" center for atHeight
        float atCenterY = (atHeight - CapsuleHeightStanding) / CapsuleHeightStanding;
        return transform.position + (
            transform.up * (
                atCenterY + Mathf.Max(atHeight / 2, Controller.radius) - Controller.radius
            )
        );
    }

    public void SetSpeedModifier(float modifier) {
        speedModifier = modifier;
    }

    public void ScalePlayerVelocity(float scale) {
        MovePlayer();
        playerVelocity *= scale;
    }

    #endregion

    #region State Machine Utility Functions
    void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);
    
    #endregion
}
