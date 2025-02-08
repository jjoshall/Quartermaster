// Code is inspired from Unity's 3D FPS template
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler)/*, typeof(AudioSource)*/)]
public class PlayerController : MonoBehaviour
{
    private CharacterController Controller;
    private PlayerInputHandler InputHandler;

    [Header("Movement")]
    private Vector3 worldspaceMove = Vector3.zero;
    public const float k_GravityForce = 20f;
    public float groundSpeed = 5f;
    [Tooltip("Speed multiplier when holding sprint key")]
    public float sprintModifier = 2f;
    private float speedModifier = 1f;
    public float maxAirSpeed = 7.5f;
    public float airAcceleration = 15f;
    [Tooltip("Sharpness affects acceleration/deceleration. Low values mean slow acceleration/deceleration and vice versa")]
    public float groundSharpness = 15f;
    private Vector3 playerVelocity;

    [Header("Looking")]
    public Camera PlayerCamera;
    [SerializeField] private bool invertVerticalInput;
    [SerializeField] private bool invertHorizontalInput;
    [SerializeField] private float vertical_sens = 100f;
    [SerializeField] private float horizontal_sens = 100f;
    private float cameraVerticalAngle = 0f;
    private float rotationMultiplier = 1f;

    [Header("Ground Based Interactions")]
    [SerializeField] private LayerMask GroundLayers = 3;
    public float jumpForce = 5f;
    private bool IsGrounded = true;
    private float lastTimeJumped;
    private bool jumpedThisFrame = false;
    private Vector3 lastImpactSpeed = Vector3.zero;
    private const float k_JumpGroundingPreventionTime = 0.2f;
    private const float k_GroundCheckDistance = 0.07f;
    private const float k_GroundCheckDistanceInAir = 0.1f;
    private Vector3 GroundNormal = Vector3.up;

    [Header("State Machine")]
    private StateMachine stateMachine;

    void Start()
    {
        Controller = GetComponent<CharacterController>();
        InputHandler = GetComponent<PlayerInputHandler>();
        if (!PlayerCamera){
            Debug.LogError("No Camera detected on player!");
        }
        lastTimeJumped = Time.time;

        // State Maching
        stateMachine = new StateMachine();
        WalkState walkState = new WalkState(this);
        AirborneState airborneState = new AirborneState(this);

        // State transitions
        Any(airborneState, new FuncPredicate(() => !IsGrounded));
        At(airborneState, walkState, new FuncPredicate(() => IsGrounded));

        // Set initial state
        stateMachine.SetState(walkState);

    }

    // Update is called once per frame
    void Update()
    {
        jumpedThisFrame = false;
        GroundCheck();
        // TODO: landing from a fall logic
        // TODO: crouching
        speedModifier = InputHandler.IsSprinting ? sprintModifier : 1f;
        worldspaceMove = transform.TransformVector(InputHandler.move_vector);
        HandleLook();
        //HandleMovement();
        stateMachine.Update();
    }

    void FixedUpdate(){
        stateMachine.FixedUpdate();
    }

    // allow transform teleporting.
    public bool toggleCharacterController(){
        Controller.enabled = !Controller.enabled;
        if (Controller.enabled){
            return true;
        }
        return false;
    }

    void HandleLook(){
        // horizontal
        transform.Rotate(
            new Vector3(0f, (InputHandler.GetHorizontalLook() * horizontal_sens * rotationMultiplier * (invertHorizontalInput ? -1 : 1)), 0f), Space.Self
        );
        // vertical
        cameraVerticalAngle += InputHandler.GetVerticalLook() * vertical_sens * rotationMultiplier * (invertVerticalInput ? -1 : 1);
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle,-89f,89f);
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
    }

    public void HandleGroundMovement(){
        Vector3 targetVelocity = worldspaceMove * groundSpeed * speedModifier;
        targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, GroundNormal)
                        * targetVelocity.magnitude;
        playerVelocity = Vector3.Lerp(playerVelocity, targetVelocity, groundSharpness * Time.deltaTime);
        if (IsGrounded && InputHandler.jumped){
            playerVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
            playerVelocity += Vector3.up * jumpForce;
            lastTimeJumped = Time.time;
            jumpedThisFrame = true;
            IsGrounded = false;
            GroundNormal = Vector3.up;
        }
        MovePlayer();
    }

    public void HandleAirMovement(){
        playerVelocity += worldspaceMove * airAcceleration * Time.deltaTime;
        // limit horizontal air speed if needed (hence gap in playerVelocity assignments)
        float verticalVelocity = playerVelocity.y;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed * speedModifier);
        playerVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
        playerVelocity += Vector3.down * k_GravityForce * Time.deltaTime;
        MovePlayer();
    }

    void MovePlayer(){
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere();
        Controller.Move(playerVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        lastImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, Controller.radius,
            playerVelocity.normalized, out RaycastHit hit, playerVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            lastImpactSpeed = playerVelocity;

            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, hit.normal);
        }
    }

    void HandleMovement(){
        // handle rotation
        // horizontal
        transform.Rotate(
            new Vector3(0f, (InputHandler.GetHorizontalLook() * horizontal_sens * rotationMultiplier * (invertHorizontalInput ? -1 : 1)), 0f), Space.Self
        );
        // vertical
        cameraVerticalAngle += InputHandler.GetVerticalLook() * vertical_sens * rotationMultiplier * (invertVerticalInput ? -1 : 1);
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle,-89f,89f);
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);

        //HANDLE_LOOK END


        // handle physical movement
        float speedModifier = InputHandler.IsSprinting ? sprintModifier : 1f;

        // CharacterController component moves via world space vectors, not local space
        Vector3 worldspaceMove = transform.TransformVector(InputHandler.move_vector);
        if (IsGrounded){
            // Ground Movement
            Vector3 targetVelocity = worldspaceMove * groundSpeed * speedModifier;
            targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, GroundNormal)
                            * targetVelocity.magnitude;
            playerVelocity = Vector3.Lerp(playerVelocity, targetVelocity, groundSharpness * Time.deltaTime);
            
            // Jumping
            if (IsGrounded && InputHandler.jumped){
                playerVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
                playerVelocity += Vector3.up * jumpForce;
                lastTimeJumped = Time.time;
                jumpedThisFrame = true;
                IsGrounded = false;
                GroundNormal = Vector3.up;
            }

        }else{
            // Air Movement
            playerVelocity += worldspaceMove * airAcceleration * Time.deltaTime;
            // limit horizontal air speed if needed (hence gap in playerVelocity assignments)
            float verticalVelocity = playerVelocity.y;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed * speedModifier);
            playerVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
            playerVelocity += Vector3.down * k_GravityForce * Time.deltaTime;
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere();
        Controller.Move(playerVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        lastImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, Controller.radius,
            playerVelocity.normalized, out RaycastHit hit, playerVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            lastImpactSpeed = playerVelocity;

            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, hit.normal);
        }

    }

    // Code from Unity FPS template
    void GroundCheck(){
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance =
            IsGrounded ? (Controller.skinWidth + k_GroundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        IsGrounded = false;
        GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= lastTimeJumped + k_JumpGroundingPreventionTime){
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(),
                Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                // storing the upward direction for the surface found
                GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(GroundNormal))
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

    // HELPER FUNCTIONS
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal){
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }
    bool IsNormalUnderSlopeLimit(Vector3 normal){
        return Vector3.Angle(transform.up, normal) <= Controller.slopeLimit;
    }
    Vector3 GetCapsuleBottomHemisphere(){
        return transform.position + (-1 * transform.up * Controller.radius);
    }   
    Vector3 GetCapsuleTopHemisphere(){
        return transform.position + (transform.up * Controller.radius);
    }
    void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);
}
