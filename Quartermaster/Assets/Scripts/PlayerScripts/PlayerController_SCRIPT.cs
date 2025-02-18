// Code is inspired from Unity's 3D FPS template
using UnityEngine;
// include network bheaviour
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

// Replace MonoBehaviour with NetworkBehaviour

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(Health)/*, typeof(AudioSource)*/)]
public class PlayerController : NetworkBehaviour {
    private CharacterController Controller;
    private PlayerInputHandler InputHandler;

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
    private Vector3 cameraOffset = new Vector3(0f,0f,0.36f);
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
    private Health health;

    [Header("Crouching")]
    [SerializeField] private Transform visualTransform;
    private float targetHeight;
    private float CapsuleHeightStanding = 2f;
    private float CapsuleHeightCrouching = 1f;
    [SerializeField] private float crouchingSharpness = 10f;
    private bool IsCrouched = false;


    [Header("Network Variables")]
    // must set proper read/write permission depending on if server or client
    private NetworkVariable<int> testNetworkVar = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);    

    private NetworkVariable<CustomNetworkData> testCustomNetworkVar = 
        new NetworkVariable<CustomNetworkData>(new CustomNetworkData {_int = 4, _bool = true, message = "tee hee"}, 
                                                NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // in order to use the above, you need to define the struct:
    public struct CustomNetworkData: INetworkSerializable {
        public int _int;
        public bool _bool;
        // for strings you must use FixedString, pick the one with the correct number
        // of bytes for the use case (1 char = 1 byte)
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref message);
        } 
    }


    // example of a server rpc
    // when the host runs it, it runs fine
    // when a client runs it, it doesnt appear on the client side, it 
    // instead requests the host to run it, and the host runs it
    // ServerRpcParams is used to get info about the client that called the rpc
    [ServerRpc]
    private void TestServerRpc(string message, ServerRpcParams serverRpcParams) {
        Debug.Log("TestServerRpc called by " + OwnerClientId + " with message: " + message + " and from: " + serverRpcParams.Receive.SenderClientId);
    }


    // example of a client rpc
    // queued by the server to run on all clients
    // can also be run by the host
    // you can also specify if you want only specific clients to recieve a client rpc
    // by using ClientRpcParams (further below at line 161)
    [ClientRpc]
    private void TestClientRpc(string message, ClientRpcParams clientRpcParams) {
        Debug.Log("TestClientRpc called by " + OwnerClientId + " with message: " + message + " and from: " + clientRpcParams);
    }

    // allow transform teleporting.
    public bool toggleCharacterController() {
        Controller.enabled = !Controller.enabled;
        if (Controller.enabled){
            return true;
        }
        return false;
    }
    
    void Start() {
        if (!IsOwner) {
            // if not owner, disable other players cameras and audio listeners
            // they still work on other peoples clients, but it prevents the client
            // from controlling other players cameras
            PlayerCamera.gameObject.SetActive(false);
            PlayerCamera.GetComponent<AudioListener>().enabled = false;
            return;
        }

        Controller = GetComponent<CharacterController>();
        InputHandler = GetComponent<PlayerInputHandler>();
        health = GetComponent<Health>();
        health.OnDie += OnDie;
        health.OnDamaged += OnDamaged;
        if (!PlayerCamera) {
            Debug.LogError("No Camera detected on player!");
        }
        if (!visualTransform) {
            Debug.LogError("No visual mesh attached to player!");
        }
        lastTimeJumped = Time.time;
        targetHeight = CapsuleHeightStanding;

        // State Maching
        stateMachine = new StateMachine();
        WalkState walkState = new WalkState(this);
        AirborneState airborneState = new AirborneState(this);
        SlideState slideState = new SlideState(this);
        SprintState sprintState = new SprintState(this);
        CrouchState crouchState = new CrouchState(this);


        // State transitions
        Any(airborneState, new FuncPredicate(() => !IsGrounded));
        At(airborneState, walkState, new FuncPredicate(() => IsGrounded));

        At(walkState, sprintState, new FuncPredicate(() => InputHandler.isSprinting));
        At(walkState, crouchState, new FuncPredicate(() => IsCrouched));

        At(sprintState, walkState, new FuncPredicate(() => !InputHandler.isSprinting));
        At(sprintState, slideState, new FuncPredicate(() => IsCrouched));

        At(slideState, walkState, new FuncPredicate(() => !IsCrouched));
        At(slideState, crouchState, new FuncPredicate(() => !InputHandler.isCrouching && IsCrouched));
        At(slideState, crouchState, new FuncPredicate(() => playerVelocity.sqrMagnitude < minSlideSpeed));
        
        At(crouchState, walkState, new FuncPredicate(() => !IsCrouched));

        // Set initial state
        stateMachine.SetState(walkState);

    }

    // use this to do something with a network variable when the player spawns
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            Debug.Log("Player spawned");
        }
    }


    // Update is called once per frame
    void Update() {
        if (!IsOwner) return;
        
        // example usage of network var
        if (Input.GetKeyDown(KeyCode.T)) {
            testNetworkVar.Value = Random.Range(0, 100);
            Debug.Log("testNetworkVar: " + testNetworkVar.Value);
        }

        // example usage of server RPC
        if (Input.GetKeyDown(KeyCode.Y)) {
            TestServerRpc("test message", new ServerRpcParams());
        }

        // example usage of client RPC
        // uses ClientRpcParams to send to only client 1
        if (Input.GetKeyDown(KeyCode.U)) {
            TestClientRpc("test message", new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { 1 } } } );
        }

        if (transform.position.y <= -25f){
            health.Kill();
        }

        GroundCheck();
        // TODO: landing from a fall logic
        // TODO: crouching
        //speedModifier = InputHandler.IsSprinting ? sprintModifier : 1f;
        worldspaceMove = transform.TransformVector(InputHandler.move_vector);
        SetCrouchingState(InputHandler.isCrouching, false);
        UpdateHeight(false);
        HandleLook();
        //HandleMovement();
        stateMachine.Update();
    }

    void FixedUpdate() {
        if (!IsOwner) return;
        

        stateMachine.FixedUpdate();
    }

    void HandleLook() {
        if (!IsOwner) return;

        // horizontal
        transform.Rotate(
            new Vector3(0f, (InputHandler.GetHorizontalLook() * horizontal_sens * rotationMultiplier * (invertHorizontalInput ? -1 : 1)), 0f), Space.Self
        );

        // vertical
        cameraVerticalAngle += InputHandler.GetVerticalLook() * vertical_sens * rotationMultiplier * (invertVerticalInput ? -1 : 1);
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle,-89f,89f);
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
    }

    public void HandleGroundMovement() {
        if (!IsOwner) return;

        Vector3 targetVelocity = worldspaceMove * groundSpeed * speedModifier;
        targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, GroundNormal)
                        * targetVelocity.magnitude;
        /*if (IsCrouched){
            targetVelocity *= crouchModifier;
        }*/
        playerVelocity = Vector3.Lerp(playerVelocity, targetVelocity, groundSharpness * Time.deltaTime);
        JumpCheck();
        MovePlayer();
    }

    public void HandleSlideMovement() {
        /*
        - as player enters slide, save their last velocity vector as new target velocity
        - set velocity to slideForce * lastVelocity
        - lerp from that velocity to new target velocity using some slideSharpness
        */
        if (!IsOwner) return;
        playerVelocity -= playerVelocity * slideDeceleration * Time.deltaTime;
        /*if (playerVelocity.sqrMagnitude < minSlideSpeed){
            playerVelocity = Vector3.zero;
        }*/
        JumpCheck();
        MovePlayer();
    }

    public void HandleAirMovement() {
        if (!IsOwner) return;

        playerVelocity += worldspaceMove * airAcceleration * Time.deltaTime;
        // limit horizontal air speed if needed (hence gap in playerVelocity assignments)
        float verticalVelocity = playerVelocity.y;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxAirSpeed * speedModifier);
        playerVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
        // Gravity
        playerVelocity += Vector3.down * k_GravityForce * Time.deltaTime;
        MovePlayer();
    }

    void MovePlayer() {
        if (!IsOwner) return;

        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(Controller.height);
        Controller.Move(playerVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        lastImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, Controller.radius,
            playerVelocity.normalized, out RaycastHit hit, playerVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore)) {
            // We remember the last impact speed because the fall damage logic might need it
            lastImpactSpeed = playerVelocity;

            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, hit.normal);
        }
    }

    void JumpCheck() {
        if (IsGrounded && InputHandler.jumped) {
            playerVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
            playerVelocity += Vector3.up * jumpForce;
            lastTimeJumped = Time.time;
            IsGrounded = false;
            GroundNormal = Vector3.up;
        }
    }

    // Code from Unity FPS template
    void GroundCheck() {
        if (!IsOwner) return;

        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance =
            IsGrounded ? (Controller.skinWidth + k_GroundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        IsGrounded = false;
        GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= lastTimeJumped + k_JumpGroundingPreventionTime) {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(Controller.height),
                Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundLayers,
                QueryTriggerInteraction.Ignore)) {
                // storing the upward direction for the surface found
                GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(GroundNormal)) {
                    IsGrounded = true;

                    // handle snapping to the ground
                    if (hit.distance > Controller.skinWidth) {
                        Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    void UpdateHeight (bool force) {
        if (force) {
            Controller.height = targetHeight;
            Controller.center = Vector3.up * (Controller.height - CapsuleHeightStanding) / CapsuleHeightStanding;
            PlayerCamera.transform.localPosition = cameraOffset + Vector3.up * ((targetHeight*CameraHeightRatio) - 1);
            visualTransform.localPosition = Controller.center;
            visualTransform.localScale = new Vector3(1f,Controller.height / CapsuleHeightStanding,1f);
        } else if (Controller.height != targetHeight) {
            if (Mathf.Abs(Controller.height - targetHeight) < 0.0001) {
                UpdateHeight(true);
                return;
            }

            Controller.height = Mathf.Lerp(Controller.height, targetHeight, crouchingSharpness * Time.deltaTime);
            Controller.center = Vector3.up * (Controller.height - CapsuleHeightStanding) / CapsuleHeightStanding;
            visualTransform.localPosition = Controller.center;
            visualTransform.localScale = new Vector3(1f,Controller.height / CapsuleHeightStanding,1f);
            PlayerCamera.transform.localPosition = Vector3.Lerp(
                PlayerCamera.transform.localPosition, cameraOffset + Vector3.up * ((targetHeight*CameraHeightRatio) - 1), crouchingSharpness * Time.deltaTime
                );
        }
    }

    bool SetCrouchingState (bool crouched , bool ignoreObstructions) {
        if (IsCrouched == crouched) return true;
        if (crouched) {
            targetHeight = CapsuleHeightCrouching;
        }else{
            if (!ignoreObstructions) {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(CapsuleHeightStanding),
                    Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps) {
                    if (!c.transform.root.CompareTag("Player")) {
                        Debug.Log("cannot stand");
                        return false;
                    }
                }
            }

            targetHeight = CapsuleHeightStanding;
        }

        IsCrouched = crouched;
        return true;
    }

    void OnDie(){
        //Debug.Log("player died");
        playerVelocity = Vector3.zero;
        targetHeight = CapsuleHeightStanding;
        toggleCharacterController();
        transform.position = Vector3.zero;
        toggleCharacterController();
    }

    void OnDamaged(float damage, GameObject damageSource){
        Debug.Log("took damage");
    }

    // HELPER FUNCTIONS
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal) {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    bool IsNormalUnderSlopeLimit(Vector3 normal) {
        return Vector3.Angle(transform.up, normal) <= Controller.slopeLimit;
    }

    Vector3 GetCapsuleBottomHemisphere() {
        return transform.position + (transform.up * ( Controller.center.y - Mathf.Max(Controller.height/2 , Controller.radius) + Controller.radius));
    }   

    Vector3 GetCapsuleTopHemisphere(float atHeight) {
        // Controller center changes depending on height, so we make virtual center position based on atHeight given
        float atCenterY = (atHeight - CapsuleHeightStanding) / CapsuleHeightStanding;
        return transform.position + (transform.up * ( atCenterY + Mathf.Max(atHeight/2 , Controller.radius) - Controller.radius));
    }

    public void SetSpeedModifier(float modifier) {
        speedModifier = modifier;
    }

    public void ScalePlayerVelocity(float scale){
        MovePlayer();
        playerVelocity *= scale;
    }
    void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);
}
