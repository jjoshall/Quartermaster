using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
     [Header("General")]
     public Transform shootPoint;
     public Transform gunPoint;
     public LayerMask layerMask;
     public int damage = 15;
     private PlayerHealth _playerHealth;

     [Header("Gun")]
     [SerializeField] private float shootDelay = 0.5f;
     public Vector3 spread = new Vector3(0.06f, 0.06f, 0.06f);
     public TrailRenderer bulletTrailPrefab;
     private float _lastShootTime;

     private void Awake() {
          _playerHealth = FindAnyObjectByType<PlayerHealth>();
     }

     public void Shoot() {
          if (_lastShootTime + shootDelay < Time.time) {
               Vector3 direction = GetDirection();

               if (Physics.Raycast(shootPoint.position, direction, 
                                   out RaycastHit hit, float.MaxValue, 
                                   layerMask)) {
                    //Debug.DrawLine(shootPoint.position, shootPoint.position + direction * 10f, Color.red, 1f);

                    // Get a bullet trail from the pool, set its position, and start the coroutine
                    TrailRenderer trail = BulletPools.instance.GetBulletTrail();
                    trail.transform.position = gunPoint.position;
                    StartCoroutine(SpawnTrail(trail, hit));

                    _lastShootTime = Time.time;
               }
          }
     }

     private Vector3 GetDirection() {
          Vector3 direction = transform.forward;
          direction += new Vector3(
               Random.Range(-spread.x, spread.x),
               Random.Range(-spread.y, spread.y),
               Random.Range(-spread.z, spread.z)
          );

          direction.Normalize();
          return direction;
     }

     private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit) {
          float time = 0f;
          Vector3 startPosition = trail.transform.position;

          while (time < 1f) {
               trail.transform.position = Vector3.Lerp(startPosition, hit.point, time);
               time += Time.deltaTime / trail.time;

               yield return null;
          }

          trail.transform.position = hit.point;
          
          // Deal damage to the player
          _playerHealth.Damage(damage, gunPoint.position);

          // Wait for trail to finish before returning it to the pool
          yield return new WaitForSeconds(trail.time);

          // Return the bullet trail to the pool
          BulletPools.instance.ReturnBulletTrail(trail);
     }
}
