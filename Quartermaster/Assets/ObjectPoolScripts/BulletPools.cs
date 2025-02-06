using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class BulletPools : MonoBehaviour
{
     public static BulletPools instance;

     [Header("Pool Settings")]
     [SerializeField] private TrailRenderer bulletTrailPrefab;
     [SerializeField] private int initialPoolSize = 10;

     private Queue<TrailRenderer> bulletTrails = new Queue<TrailRenderer>();

     private void Awake()
     {
          instance = this;

          // Pre-populate the pool with the initial size
          for (int i = 0; i < initialPoolSize; i++)
          {
               TrailRenderer trail = Instantiate(bulletTrailPrefab, transform);
               trail.gameObject.SetActive(false);
               bulletTrails.Enqueue(trail);
          }
     }

     // Get a bullet trail from the pool (or create a new one if necessary)
     public TrailRenderer GetBulletTrail()
     {
          // If we have available objects in the pool
          if (bulletTrails.Count > 0)
          {
               var trail = bulletTrails.Dequeue();
               trail.gameObject.SetActive(true);

               // It's often useful to clear the trail so old data doesn't linger.
               // Doing it here ensures a fresh trail whenever you fetch one.
               trail.Clear();

               return trail;
          }

          TrailRenderer newTrail = Instantiate(bulletTrailPrefab, transform);
          return newTrail;
     }

     // Return a bullet trail to the pool to be reused
     public void ReturnBulletTrail(TrailRenderer trail)
     {
          trail.gameObject.SetActive(false);
          bulletTrails.Enqueue(trail);
     }
}
