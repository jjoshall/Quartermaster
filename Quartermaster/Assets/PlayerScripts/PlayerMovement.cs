// Code help from https://www.youtube.com/watch?v=f473C43s8nE&t=398s&ab_channel=Dave%2FGameDevelopment
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
     [Header("Movement")]
     public float moveSpeed;

     public float groundDrag;

     public float jumpForce;
     public float jumpCooldown;
     public float airMultiplier;   // movement modifier in air
     bool readyToJump;

     [Header("Keybinds")]
     public KeyCode jumpKey = KeyCode.Space;


     [Header("Ground Check")]
     public Transform groundCheck;
     public float groundDistance = 0.4f;     // extra distance to check for ground from bottom of player
     public float playerHeight;
     public LayerMask whatIsGround;
     bool grounded;

     public Transform orientation;

     float horizontalInput;
     float verticalInput;

     Vector3 moveDirection;

     Rigidbody rb;

     private void Start()
     {
          rb = GetComponent<Rigidbody>();
          rb.freezeRotation = true;

          readyToJump = true;
     }

     private void Update()
     {
          // Ground Check
          grounded = Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround);

          MyInput();
          SpeedControl();

          // Handle drag
          if (grounded)
               rb.linearDamping = groundDrag;
          else
               rb.linearDamping = 0;
     }

     private void FixedUpdate()
     {
          MovePlayer();
     }

     private void MyInput()
     {
          horizontalInput = Input.GetAxis("Horizontal");
          verticalInput = Input.GetAxis("Vertical");

          // If jump key is pressed and player is grounded, jump
          if (Input.GetKey(jumpKey) && readyToJump && grounded)
          {
               Jump();
               readyToJump = false;
               Invoke(nameof(ResetJump), jumpCooldown);
          }
     }

     private void MovePlayer()
     {
          // Calculate move direction
          moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

          // Move player on the ground
          if (grounded)
               rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

          // Move player in the air
          else if(!grounded)
               rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier * 10f, ForceMode.Force);
     }

     private void SpeedControl()
     {
          Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

          // Limit the player's speed if they are going too fast
          if (flatVel.magnitude > moveSpeed)
          {
               Vector3 limitedVel = flatVel.normalized * moveSpeed;
               rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
          }
     }

     private void Jump()
     {
          // Reset Y velocity so the jump is consistent
          rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

          rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
     }

     private void ResetJump()
     {
          readyToJump = true;
     }
}
