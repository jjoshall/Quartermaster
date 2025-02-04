// Code help from: https://www.youtube.com/watch?v=Srg6GRFspps&ab_channel=Eincode
using UnityEngine;

public class EnemyController : MonoBehaviour
{
     [SerializeField] private float speed = 5f;
     [SerializeField] private float rotationSpeed = 10f;
     [SerializeField] private float stoppingDistance = 1.5f;

     // This is when we get the model of the enemy
     //private Animator animator;

     private Transform target;
     private Vector3 direction = Vector3.zero;
     private Quaternion targetRotation;

     private float movementSpeedBlend;

     private void Awake()
     {
          target = GameObject.FindFirstObjectByType<PlayerMovement>().transform;
          //animator = GetComponent<Animator>();
     }

     private void Update()
     {
          if (target != null)
          {
               FollowTarget();
          }
     }

     private void FollowTarget()
     {
          direction = (target.position - transform.position).normalized;
          float distance = direction.magnitude;

          if (distance > stoppingDistance)
          {
               MoveTowardsTarget();
          }
          else
          {
               StopMove();
          }

          RotateTowardsTarget();
     }

     private void MoveTowardsTarget()
     {
          
          direction = direction.normalized;
          Vector3 movement = direction * speed * Time.deltaTime;
          transform.position += transform.forward * movement.magnitude;
          movementSpeedBlend = Mathf.Lerp(movementSpeedBlend, 1, Time.deltaTime * speed);
          //animator.SetFloat("Speed", movementSpeedBlend);
     }

     private void StopMove()
     {
          movementSpeedBlend = Mathf.Lerp(movementSpeedBlend, 0, Time.deltaTime * speed);
          //animator.SetFloat("Speed", movementSpeedBlend);
     }

     private void RotateTowardsTarget()
     {
          targetRotation = Quaternion.LookRotation(direction);
          transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
     }
}
