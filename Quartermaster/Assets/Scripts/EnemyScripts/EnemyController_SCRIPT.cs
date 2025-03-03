// Code help from: https://www.youtube.com/watch?v=Srg6GRFspps&ab_channel=Eincode
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Health))]
public class EnemyController : NetworkBehaviour {
     [SerializeField] private float _speed = 5f;
     [SerializeField] private float _rotationSpeed = 10f;
     [SerializeField] private float _stoppingDistance = 1.5f;

     [Header("Boids")]
     [SerializeField] private float _detectionDistance = 1f;
     [SerializeField] private float _separationWeight = 1f;
     [SerializeField] private float _alignmentWeight = 1f;
     [SerializeField] private float _cohesionWeight = 1f;

     // This is when we get the model of the enemy
     //private Animator animator;

     private Transform target;
     private Vector3 direction;
     private Quaternion targetRotation;

     private float movementSpeedBlend;
     private Vector3 separationForce;


     public Health health;

     private void Awake() {
          target = GameObject.FindFirstObjectByType<PlayerController>().transform;
          //animator = GetComponent<Animator>();
     }

    void Start()
    {
          if (!health){
               health = GetComponent<Health>();
          }
          health.OnDie += OnDie;
          health.OnDamaged += OnDamaged;
    }

    private void Update() {
          if (target != null) {
               FollowTarget();
          }
     }

     private void FollowTarget() {
          separationForce = Vector3.zero;
          direction = (target.position - transform.position);
          var newVector = new Vector3(direction.x, 0, direction.z);
          float distance = direction.magnitude;

          var neighbors = GetNeighbors();

          if (neighbors.Length > 0) {
               CalculateSeparationForce(neighbors);
               ApplyAllignment(neighbors);
               ApplyCohesion(neighbors);
          }

          if (distance > _stoppingDistance) {
               MoveTowardsTarget();
          }

          RotateTowardsTarget();
     }

     private void ApplyCohesion(Collider[] neighbors) {
          Vector3 averagePosition = Vector3.zero;
          
          foreach (var neighbor in neighbors) {
               averagePosition += neighbor.transform.position;
          }

          averagePosition /= neighbors.Length;
          Vector3 cohesionDir = (averagePosition - transform.position).normalized;
          separationForce += cohesionDir * _cohesionWeight;
     }

     private void ApplyAllignment(Collider[] neighbors) {
          Vector3 neighborsForward = Vector3.zero;

          foreach (var neighbor in neighbors) {
               neighborsForward += neighbor.transform.forward;
          }

          if (neighborsForward != Vector3.zero) {
               neighborsForward.Normalize();
          }

          separationForce += neighborsForward * _alignmentWeight;
     }

     private void CalculateSeparationForce(Collider[] neighbors) {
          foreach(var neighbor in neighbors) {
               var dir = neighbor.transform.position - transform.position;
               var distance = dir.magnitude;
               var away = -dir.normalized;

               if (distance > 0) {
                    separationForce += (away / distance) * _separationWeight;
               }
          }
     }

     private Collider[] GetNeighbors() {
          var enemyMask = LayerMask.GetMask("Enemy");
          return Physics.OverlapSphere(transform.position, _detectionDistance, enemyMask);
     }

     private void MoveTowardsTarget() {     
          direction = direction.normalized;
          var combinedDirection = (direction + separationForce).normalized;
          var movement = combinedDirection * _speed * Time.deltaTime;
          transform.position += movement;
          //movementSpeedBlend = Mathf.Lerp(movementSpeedBlend, 1, Time.deltaTime * speed);
          //animator.SetFloat("Speed", movementSpeedBlend);
     }

     private void RotateTowardsTarget() {
          targetRotation = Quaternion.LookRotation(direction);
          transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
     }

     void OnDamaged(float damage, GameObject source){
          Debug.Log("enemy took damage");
     }
     
     void OnDie(){
          EnemySpawner.instance.destroyEnemyServerRpc(GetComponent<NetworkObject>());
     }
}
