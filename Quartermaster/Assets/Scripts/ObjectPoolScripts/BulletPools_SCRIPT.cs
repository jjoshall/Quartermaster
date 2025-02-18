using System.Collections.Generic;
using UnityEngine;

public class BulletPools : MonoBehaviour {
     public static BulletPools instance;

     [Header("Pool Settings")]
     [SerializeField] private TrailRenderer _bulletTrailPrefab;
     [SerializeField] private int _initialPoolSize = 10;

     private Queue<TrailRenderer> _bulletTrails = new Queue<TrailRenderer>();

     private void Awake() {
          instance = this;

          // Pre-populate the pool with the initial size
          for (int i = 0; i < _initialPoolSize; i++) {
               TrailRenderer trail = Instantiate(_bulletTrailPrefab, transform);
               trail.gameObject.SetActive(false);
               _bulletTrails.Enqueue(trail);
          }
     }

     // Get a bullet trail from the pool (or create a new one if necessary)
     public TrailRenderer GetBulletTrail() {
          // If we have available objects in the pool
          if (_bulletTrails.Count > 0) {
               var trail = _bulletTrails.Dequeue();
               trail.gameObject.SetActive(true);

               // It's often useful to clear the trail so old data doesn't linger.
               // Doing it here ensures a fresh trail whenever you fetch one.
               trail.Clear();

               return trail;
          }

          TrailRenderer newTrail = Instantiate(_bulletTrailPrefab, transform);
          return newTrail;
     }

     // Return a bullet trail to the pool to be reused
     public void ReturnBulletTrail(TrailRenderer trail) {
          trail.gameObject.SetActive(false);
          _bulletTrails.Enqueue(trail);
     }
}
