using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;


[RequireComponent(typeof(SphereCollider))]
public class TurretController_SCRIPT : NetworkBehaviour
{
    private List<NetworkObject> _InRange;   // all valid enemies in range of detection
    public NetworkObject target;    // current target to attack
    private bool _IsNewTarget = false;
    public Transform BulletSpawnPoint;  // empty game object to calculate what turret can "see" and fire from
    private float BulletRange;  //differs from sphere collider range because bullets spawn from nozzle, not center of model
    private float DetectionRadius;
    [SerializeField] private float RotationSpeed = 2f;    // how fast turret rotates to lock on targets
    private Coroutine RotateCoroutine;
    private int BulletLayerMask;
    private string _TargetTag = "Player";    // tag given to what to be considered a valid target
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
        if (!IsServer) return;
        Debug.Log("turret spawned in network");
        enemySpawner = EnemySpawner.instance;
        if (!enemySpawner){
            Debug.LogError("EnemySpawner singleton not found!");
        }
        enemySpawner.OnEnemyDespawn += OnEnemyDespawn;

        _InRange = new List<NetworkObject>();
        BulletLayerMask = LayerMask.GetMask("Enemy","Bulding");

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
        if (!IsServer) return;
        /*
        Lock On to target if needed
        Fire projectile at target
        */
        UpdateTarget();
        if (target){
            if (_IsNewTarget){
                StartRotating();
            }
            Shoot();
        }
    }

    void UpdateTarget(){
        if (target == null){
            List<(float distance, NetworkObject target)> orderedTargets = GetOrderedTargets();
            RaycastHit hit;
            foreach((float _, NetworkObject potentialTarget) in orderedTargets){
                if (Physics.Raycast(transform.position, (transform.position - potentialTarget.transform.position).normalized, out hit, DetectionRadius, BulletLayerMask)){
                    if (hit.collider.CompareTag(_TargetTag)){
                        target = hit.collider.GetComponent<NetworkObject>();
                        _IsNewTarget = true;
                        break;
                    }
                }
            }
        }
        _IsNewTarget = false;
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

    void StartRotating(){
        if (RotateCoroutine != null){
            StopCoroutine(RotateCoroutine);
        }
        RotateCoroutine = StartCoroutine(RotateToTarget());
    }

    private IEnumerator RotateToTarget(){
        // only doing horizontal rotation for now until turret model is deicded on and pivot location is known
        Quaternion lookRotation = Quaternion.LookRotation(target.transform.position - transform.position);
        // ensure this rotation is only horizontal
        lookRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, lookRotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        float time = 0;
        while (time < 1){
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, time);
            time += Time.deltaTime * RotationSpeed;
            yield return null;
        }
    }

    void Shoot(){
        // raycast in forward direction
        // if raycast hits, do damage
        if (!IsServer) return;
        Debug.Log("TURRET: shooting at target");
    }

    #region Enemy Detection
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && other.CompareTag(_TargetTag)){
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
