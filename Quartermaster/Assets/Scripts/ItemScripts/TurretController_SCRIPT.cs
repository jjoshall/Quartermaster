using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;


[RequireComponent(typeof(SphereCollider))]
public class TurretController_SCRIPT : NetworkBehaviour
{
    private List<NetworkObject> _InRange;   // all valid enemies in range of detection
    public NetworkObject target;    // current target to attack
    public Transform BulletSpawnPoint;  // empty game object to calculate what turret can "see" and fire from
    private float BulletRange;  //differs from sphere collider range because bullets spawn from nozzle, not center of model
    private float DetectionRadius;
    private readonly int BulletLayerMask = LayerMask.GetMask("Enemy","Bulding");
    //  public Item weapon;         // current item given to turret to use
    /*
    NOTE:
    Cannot find a way to make sure the item is considered a weapon
    Alternatively can let the turret use any item
    Furthermore, item's are coupled to a Player, so reuing their code is difficult
    */

    // Subscribe to OnEnemyDespawn event to handle removing enemies from _inRange
    private EnemySpawner enemySpawner;


    public override void OnNetworkSpawn() {
        enemySpawner = EnemySpawner.instance;
        if (!enemySpawner){
            Debug.LogError("EnemySpawner singleton not found!");
        }
        enemySpawner.OnEnemyDespawn += OnEnemyDespawn;

        Vector3 worldDistance = BulletSpawnPoint.position - transform.position;
        worldDistance.y = 0;
        float bulletSpawnOffset = worldDistance.magnitude;
        DetectionRadius = GetComponent<SphereCollider>().radius;
        BulletRange = DetectionRadius - bulletSpawnOffset;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        enemySpawner.OnEnemyDespawn -= OnEnemyDespawn;
    }

    void Update() {
        /*
        Lock On to target if needed
        Fire projectile at target
        */
        UpdateTarget();
    }

    void UpdateTarget(){
        if (target == null){
            List<(float distance, NetworkObject target)> orderedTargets = GetOrderedTargets();
            RaycastHit hit;
            foreach((float _, NetworkObject potentialTarget) in orderedTargets){
                if (Physics.Raycast(transform.position, (transform.position - potentialTarget.transform.position).normalized, out hit, DetectionRadius, BulletLayerMask)){
                    if (hit.collider.CompareTag("Enemy")){
                        target = hit.collider.GetComponent<NetworkObject>();
                        break;
                    }
                }
            }
        }
        // if no target, loop through list, finding closest
        // "look at" the target and maintain raycast from firing position
        transform.LookAt(target.transform);
        // if raycast hits building, get new target
        // if raycast hits a different enemy, set that enemy to new target

        // next, rotate angle accordingly / look at target
    }
    List<(float distance, NetworkObject target)> GetOrderedTargets(){
        List<(float distance, NetworkObject target)> orderedTargets = new List<(float distance, NetworkObject target)>();
        foreach (NetworkObject potentialTarget in _InRange){
            float sqrDistance = (potentialTarget.transform.position - transform.position).sqrMagnitude;
            bool added = false;
            for (int i = 0; i < orderedTargets.Count; i+=1){
                if (sqrDistance < orderedTargets[i].distance){
                    orderedTargets.Insert(i, (sqrDistance , potentialTarget));
                    added = true;
                    break;
                }
            }
            if (!added){
                orderedTargets.Add((sqrDistance , potentialTarget));
            }
        }
        return orderedTargets;
    }

    #region Enemy Detection
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && other.CompareTag("Enemy")){
            _InRange.Add(netObj);
        }
    }
    
    private void OnTriggerExit(Collider other){
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && _InRange.Contains(netObj)){
            if (target == netObj){
                target = null;
            }
            _InRange.Remove(netObj);
        }
    }

    void OnEnemyDespawn(NetworkObject enemyObject){
        // if the object == any object in _inRange, remove from _inRange list
        if (_InRange.Contains(enemyObject)){
            if (target == enemyObject){
                target = null;
            }
            _InRange.Remove(enemyObject);
        }
    }
    #endregion
}
