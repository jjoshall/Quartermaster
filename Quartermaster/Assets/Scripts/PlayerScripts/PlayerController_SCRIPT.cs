// Code is inspired from Unity's 3D FPS template
using UnityEngine;
// include network behavior
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(Health))]
public class PlayerController : NetworkBehaviour
{
    // -- References to required Components
    private CharacterController Controller;
    private PlayerInputHandler InputHandler;
    private Health health;

    [SerializeField] private Transform spawnLocation;

    // -- MOVEMENT FIELDS
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

    // -- LOOKING / CAMERA
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

    // -- GROUND INTERACTIONS
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

    // -- STATE MACHINE
    [Header("State Machine")]
    private StateMachine stateMachine;

    // -- CROUCHING
    [Header("Crouching")]
    [SerializeField] private Transform visualTransform;
    private float targetHeight;
    private float CapsuleHeightStanding = 2f;
    private float CapsuleHeightCrouching = 1f;
    [SerializeField] private float crouchingSharpness = 10f;
    private bool IsCrouched = false;

    // -- NETWORK VARIABLES
    [Header("Network Variables")]
    // Example network variable (integer)
    private NetworkVariable<int> testNetworkVar = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Custom network data struct example
    private NetworkVariable<CustomNetworkData> testCustomNetworkVar =
        new NetworkVariable<CustomNetworkData>(
            new CustomNetworkData { _int = 4, _bool = true, message = "tee hee" },
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    // This struct must implement INetworkSerializable
    public struct CustomNetworkData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        // For strings, you must use a FixedString type
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        }
    }

    // SERVER RPC EXAMPLE
    [ServerRpc]
    private void TestServerRpc(string message, ServerRpcParams serverRpcParams)
    {
        Debug.Log("TestServerRpc called by " + OwnerClientId + " with message: " + message
            + " and from: " + serverRpcParams.Receive.SenderClientId);
    }

    // CLIENT RPC EXAMPLE
    [ClientRpc]
    private void TestClientRpc(string message, ClientRpcParams clientRpcParams)
    {
        Debug.Log("TestClientRpc called by " + OwnerClientId + " with message: " + message + " and from: " + clientRpcParams);
    }

    [ServerRpc]
    void UpdatePositionServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        if (Vector3.Distance(transform.position, position) > 0.01f || Quaternion.Angle(transform.rotation, rotation) > 0.1f)
        {
            transform.position = position;
            transform.rotation = rotation;
            UpdatePositionClientRpc(position, rotation);
        }
    }

    [ClientRpc]
    void UpdatePositionClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
        {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 10f); // Smooth movement
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 10f);
        }
    }



    // -------------------------
    // LIFE CYCLE
    // -------------------------

    /// <summary>
    /// Called when the player prefab is first instantiated (before NetworkSpawn).
    /// </summary>
    void Start()
    {

        // Check if required components exist
        if (PlayerCamera == null) Debug.LogError($"[{Time.time}] ERROR: No Camera detected on {gameObject.name}");
        if (visualTransform == null) Debug.LogError($"[{Time.time}] ERROR: No visual mesh attached to {gameObject.name}");

        // Initialize default values
        lastTimeJumped = Time.time;
        targetHeight = CapsuleHeightStanding;

        Debug.Log($"[{Time.time}] Start() completed on {gameObject.name}");
    }

    /// <summary>
    /// This is called once the object is fully spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // temp GetComponent<NetworkTransform>().enabled = false;
        Debug.Log($"[{Time.time}] OnNetworkSpawn() called on {gameObject.name} | OwnerClientId: {OwnerClientId} | LocalClientId: {NetworkManager.Singleton.LocalClientId}");


        // Check ownership: only the owner can control movement and camera
        if (!IsOwner)
        {
            Debug.Log($"[{Time.time}] {gameObject.name} is NOT the owner, disabling player-specific components.");
            DisablePlayerControls();
            return;
        }

        Debug.Log($"[{Time.time}] {gameObject.name} is the OWNER, initializing components...");

        // -- Assign Components
        Controller = GetComponent<CharacterController>();
        InputHandler = GetComponent<PlayerInputHandler>();
        health = GetComponent<Health>();

        Debug.Log("owner: " + OwnerClientId + "character controller enabled: " + Controller.enabled);


        // Validate presence
        if (Controller == null) Debug.LogError($"[{Time.time}] ERROR: CharacterController missing on {gameObject.name}!");
        if (InputHandler == null) Debug.LogError($"[{Time.time}] ERROR: PlayerInputHandler missing on {gameObject.name}!");
        if (health == null) Debug.LogError($"[{Time.time}] ERROR: Health component missing on {gameObject.name}!");

        // Enable components if they exist
        EnablePlayerControls();

        // Subscribe to health events if available
        if (health != null)
        {
            health.OnDie += OnDie;
            health.OnDamaged += OnDamaged;
            Debug.Log($"[{Time.time}] Health event listeners assigned for {gameObject.name}");
        }

        // Initialize the State Machine
        InitializeStateMachine();

        // Force player to correct height
        UpdateHeight(true);
        Debug.Log($"[{Time.time}] OnNetworkSpawn() completed for {gameObject.name}");
    }

    /// <summary>
    /// Enable movement, input, camera, etc. for the owner.
    /// </summary>
    private void EnablePlayerControls()
    {
        // Camera
        if (PlayerCamera != null)
        {
            PlayerCamera.gameObject.SetActive(true);
            PlayerCamera.GetComponent<AudioListener>().enabled = true;
        }

        // Character Controller
        if (Controller != null)
        {
            Debug.Log($"[{Time.time}] Enabling CharacterController for {gameObject.name} with owner {OwnerClientId}");
            Controller.enabled = true;
            Debug.Log("owner: " + OwnerClientId + "character controller enabled: " + Controller.enabled);

        } else {
            Debug.Log($"[{Time.time}] CharacterController is NULL for {gameObject.name} with owner {OwnerClientId}");
        }

        // Input Handler
        if (InputHandler != null)
        {
            Debug.Log($"[{Time.time}] Enabling InputHandler for {gameObject.name} with owner {OwnerClientId}");
            InputHandler.enabled = true;
        } else {
            Debug.Log($"[{Time.time}] InputHandler is NULL for {gameObject.name} with owner {OwnerClientId}");
        }

        Debug.Log($"[{Time.time}] Player controls enabled for {gameObject.name} with owner {OwnerClientId}");
    }

    /// <summary>
    /// Disable movement, input, camera, etc. for non-owners.
    /// </summary>
    private void DisablePlayerControls()
    {
        // Camera
        if (PlayerCamera != null)
        {
            PlayerCamera.gameObject.SetActive(false);
            PlayerCamera.GetComponent<AudioListener>().enabled = false;
        }

        // Character Controller
        if (Controller != null)
        {
            Controller.enabled = false;
        }

        // Input Handler
        if (InputHandler != null)
        {
            InputHandler.enabled = false;
        }

        Debug.Log($"[{Time.time}] Player controls disabled for {gameObject.name}.");
    }

    /// <summary>
    /// Sets up our finite state machine with all states and transitions.
    /// </summary>
    private void InitializeStateMachine()
    {
        Debug.Log($"[{Time.time}] Initializing State Machine for {gameObject.name}");

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

        Debug.Log($"[{Time.time}] State Machine initialized and initial state set for {gameObject.name}");
    }

    // -------------------------
    // UPDATE METHODS
    // -------------------------

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    void Update()
    {
        // Only the owner can handle movement and input
        if (!IsOwner) return;

        Debug.Log("owner: " + OwnerClientId + "character controller enabled: " + Controller.enabled);

        if (Input.GetKey(KeyCode.F)) {
            playerVelocity = Vector3.forward * 5f;
            Debug.Log($"[Test] Simulated velocity: {playerVelocity}");
        } else {
            if (InputHandler != null) {
                worldspaceMove = transform.TransformVector(InputHandler.move_vector);
            }
        }


        // Examples of using network variables & RPCs
        if (Input.GetKeyDown(KeyCode.T))
        {
            testNetworkVar.Value = Random.Range(0, 100);
            Debug.Log($"[{Time.time}] testNetworkVar changed to: {testNetworkVar.Value}");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            TestServerRpc("test message", new ServerRpcParams());
        }

        // Example of a client RPC to only send to client 1
        if (Input.GetKeyDown(KeyCode.U))
        {
            TestClientRpc("test message",
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { 1 }
                    }
                }
            );
        }

        // If player falls too far, kill them
        if (transform.position.y <= -25f)
        {
            if (health != null)
            {
                health.Kill();
            }
        }

        // Ground check and movement
        GroundCheck();

        // Convert local input to world movement
        if (InputHandler != null)
        {
            worldspaceMove = transform.TransformVector(InputHandler.move_vector);
            SetCrouchingState(InputHandler.isCrouching, false);
        }

        // Update player height
        UpdateHeight(false);

        MovePlayer();
        UpdatePositionServerRpc(transform.position, transform.rotation);

        // Handle camera rotation
        HandleLook();

        // State machine update
        if (stateMachine != null)
        {
            stateMachine.Update();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        MovePlayer(); // Ensure movement logic is executed

        // Send position updates to the server
        UpdatePositionServerRpc(transform.position, transform.rotation);
    }


    // -------------------------
    // MOVEMENT / LOOK METHODS
    // -------------------------

    /// <summary>
    /// Handles looking around with mouse input.
    /// </summary>
    void HandleLook()
    {
        if (InputHandler == null) {
            Debug.Log("No inut handler");
            return;
        }

        // Horizontal
        transform.Rotate(
            new Vector3(
                0f,
                InputHandler.GetHorizontalLook() * horizontal_sens * rotationMultiplier * (invertHorizontalInput ? -1 : 1),
                0f
            ),
            Space.Self
        );

        //Debug.Log("handling look and client id is " + NetworkManager.Singleton.LocalClientId);

        // Vertical
        cameraVerticalAngle += InputHandler.GetVerticalLook() * vertical_sens * rotationMultiplier * (invertVerticalInput ? -1 : 1);
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -89f, 89f);
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
        //Debug.Log("Camera angle is " + cameraVerticalAngle + "for client id " + NetworkManager.Singleton.LocalClientId);
    }

    // -------------------------
    // STATE MACHINE ACTIONS
    // -------------------------

    public void HandleGroundMovement()
    {
        Vector3 targetVelocity = worldspaceMove * groundSpeed * speedModifier;
        targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, GroundNormal) * targetVelocity.magnitude;
        playerVelocity = Vector3.Lerp(playerVelocity, targetVelocity, groundSharpness * Time.deltaTime);

        //Debug.Log("Trying to move at client id " + NetworkManager.Singleton.LocalClientId);

        JumpCheck();
        MovePlayer();
    }

    public void HandleSlideMovement()
    {
        playerVelocity -= playerVelocity * slideDeceleration * Time.deltaTime;
        JumpCheck();
        MovePlayer();
    }

    public void HandleAirMovement()
    {
        playerVelocity += worldspaceMove * airAcceleration * Time.deltaTime;
        float verticalVelocity = playerVelocity.y;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed * speedModifier);
        playerVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        // Gravity
        playerVelocity += Vector3.down * k_GravityForce * Time.deltaTime;
        MovePlayer();
    }

    // -------------------------
    // MOVEMENT HELPERS
    // -------------------------

    void MovePlayer()
    {
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

    void JumpCheck()
    {
        if (IsGrounded && InputHandler != null && InputHandler.jumped)
        {
            playerVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
            playerVelocity += Vector3.up * jumpForce;
            lastTimeJumped = Time.time;
            IsGrounded = false;
            GroundNormal = Vector3.up;
        }
    }

    /// <summary>
    /// Checks if the player is grounded by casting a capsule downward.
    /// </summary>
    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent snapping to ground
        float chosenGroundCheckDistance =
            IsGrounded ? (Controller.skinWidth + k_GroundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        IsGrounded = false;
        GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short time since last jump
        if (Time.time >= lastTimeJumped + k_JumpGroundingPreventionTime)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(Controller.height),
                                    Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance,
                                    GroundLayers, QueryTriggerInteraction.Ignore))
            {
                GroundNormal = hit.normal;
                // Only consider valid ground if the normal is mostly up and slope angle is lower than limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(GroundNormal))
                {
                    IsGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > Controller.skinWidth)
                    {
                        Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    // -------------------------
    // CROUCHING / HEIGHT
    // -------------------------

    void UpdateHeight(bool force)
    {
        if (Controller == null || PlayerCamera == null || visualTransform == null) return;

        if (force)
        {
            Controller.height = targetHeight;
            Controller.center = Vector3.up * (Controller.height - CapsuleHeightStanding) / CapsuleHeightStanding;
            PlayerCamera.transform.localPosition = cameraOffset + Vector3.up * ((targetHeight * CameraHeightRatio) - 1);
            visualTransform.localPosition = Controller.center;
            visualTransform.localScale = new Vector3(1f, Controller.height / CapsuleHeightStanding, 1f);
        }
        else if (Controller.height != targetHeight)
        {
            if (Mathf.Abs(Controller.height - targetHeight) < 0.0001f)
            {
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

    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        if (IsCrouched == crouched) return true;

        if (crouched)
        {
            targetHeight = CapsuleHeightCrouching;
        }
        else
        {
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(CapsuleHeightStanding),
                    Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore
                );
                foreach (Collider c in standingOverlaps)
                {
                    if (!c.transform.root.CompareTag("Player"))
                    {
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

    // -------------------------
    // HEALTH EVENT HANDLERS
    // -------------------------

    void OnDie()
    {
        Debug.Log($"[{Time.time}] {gameObject.name} died. Respawning...");
        if (health != null) health.Invincible = true;

        playerVelocity = Vector3.zero;
        targetHeight = CapsuleHeightStanding;
        toggleCharacterController();
        transform.position = Vector3.zero;
        toggleCharacterController();

        if (health != null)
        {
            health.Heal(1000f);
            health.Invincible = false;
        }
    }

    void OnDamaged(float damage, GameObject damageSource)
    {
        Debug.Log($"[{Time.time}] {gameObject.name} took {damage} damage. Health Ratio: {health.GetRatio()}");
    }

    // -------------------------
    // HELPER FUNCTIONS
    // -------------------------

    /// <summary>
    /// Allows us to temporarily disable the CharacterController to teleport.
    /// </summary>
    public bool toggleCharacterController()
    {
        if (Controller != null)
        {
            Controller.enabled = !Controller.enabled;
            return Controller.enabled;
        }
        return false;
    }

    /// <summary>
    /// Reorients the movement direction so it sticks to the slope.
    /// </summary>
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= Controller.slopeLimit;
    }

    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * (Controller.center.y - Mathf.Max(Controller.height / 2, Controller.radius) + Controller.radius));
    }

    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        // Controller.center depends on height, so we recalc a "virtual" center for atHeight
        float atCenterY = (atHeight - CapsuleHeightStanding) / CapsuleHeightStanding;
        return transform.position + (
            transform.up * (
                atCenterY + Mathf.Max(atHeight / 2, Controller.radius) - Controller.radius
            )
        );
    }

    public void SetSpeedModifier(float modifier)
    {
        speedModifier = modifier;
    }

    public void ScalePlayerVelocity(float scale)
    {
        MovePlayer();
        playerVelocity *= scale;
    }

    // Utility shortcuts for StateMachine
    void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);
}
