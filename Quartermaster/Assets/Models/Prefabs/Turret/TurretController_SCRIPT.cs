using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using UnityEngine.Events;


[RequireComponent(typeof(SphereCollider))]
public class TurretController_SCRIPT : NetworkBehaviour
{
    [SerializeField] Transform StemPivot;
    [SerializeField] Transform NozzlePivot;
    private List<NetworkObject> _InRange;   // all valid enemies in range of detection
    public NetworkObject target = null;    // current target to attack
    private bool _IsNewTarget = false;
    public Transform BulletSpawnPoint;  // empty game object to calculate what turret can "see" and fire from
    private float BulletRange;  //differs from sphere collider range because bullets spawn from nozzle, not center of model
    private float DetectionRadius;
    [SerializeField] private float RotationSpeed = 10f;    // how fast turret rotates to lock on targets
    private Coroutine RotateCoroutine;
    private Coroutine LifetimeCoroutine;
    private float _lifetime = 30f;
    private float _elapsedTime = 0f;
    public float _timeLastUsed = 0f;
    public int BulletLayerMask;
    public string _TargetTag {get; private set;} = "Enemy";    // tag given to what to be considered a valid target
    //  public Item weapon;         // current item given to turret to use
    /*
    NOTE:
    Cannot find a way to make sure the item is considered a weapon
    Alternatively can let the turret use any item
    Furthermore, item's are coupled to a Player, so reuing their code is difficult
    */

    // Subscribe to OnEnemyDespawn event to handle removing enemies from _inRange
    private EnemySpawner enemySpawner;
    //private List<GameObject> _items = new List<GameObject>();
    private Item _weapon;


    public void InitDeactivateEventSubscription(UnityEvent deactivateEvent)
    {
        if (deactivateEvent == null)
        {
            Debug.LogError("TurretController: InitDeactivateEventSubscription() deactivateEvent is null.");
            return;
        }
        deactivateEvent.AddListener(DespawnServerRpc);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnServerRpc()
    {
        if (!IsServer) return;
        Debug.Log("TurretController: DespawnServerRpc called");
        enemySpawner.OnEnemyDespawn -= OnEnemyDespawn;
        _InRange.Clear();
        target = null;
        if (RotateCoroutine != null)
        {
            StopCoroutine(RotateCoroutine);
            RotateCoroutine = null;
        }
        if (LifetimeCoroutine != null)
        {
            StopCoroutine(LifetimeCoroutine);
            LifetimeCoroutine = null;
        }
        NetworkObject.Despawn();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Debug.Log("turret spawned in network");
        enemySpawner = EnemySpawner.instance;
        if (!enemySpawner)
        {
            Debug.LogError("EnemySpawner singleton not found!");
        }
        enemySpawner.OnEnemyDespawn += OnEnemyDespawn;

        if (!StemPivot)
        {
            Debug.LogError("Turret: Stem Pivot not assigned");
        }
        if (!NozzlePivot)
        {
            Debug.LogError("Turret: Nozzle Pivot not assigned");
        }

        _InRange = new List<NetworkObject>();
        BulletLayerMask = LayerMask.GetMask(_TargetTag, "Building");

        Vector3 worldDistance = BulletSpawnPoint.position - transform.position;
        worldDistance.y = 0;
        float bulletSpawnOffset = worldDistance.magnitude;
        DetectionRadius = GetComponent<SphereCollider>().radius;
        BulletRange = DetectionRadius - bulletSpawnOffset;
        //_items.Clear();
        //_weapon = gameObject.AddComponent(typeof(Pistol_MONO)) as Pistol_MONO;
        _weapon = GetComponent<Pistol_MONO>();
        if (!_weapon)
        {
            Debug.LogError("Turret: Pistol not detected!");
        }

        // register enemies that are already inside detection radius when turret spawns
        // layer name and tag name are same for enemies so reusing _TargetTag is fine
        Collider[] targets = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius, LayerMask.GetMask(_TargetTag));
        foreach (Collider potentialTarget in targets)
        {
            if (potentialTarget.CompareTag(_TargetTag) && potentialTarget.TryGetComponent(out NetworkObject netobj))
            {
                AddUnique(netobj);
            }
        }
        //Debug.Log("TurretController: Number of targets already in range: " + _InRange.Count);

        LifetimeCoroutine = StartCoroutine(DespawnTimer());
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        enemySpawner.OnEnemyDespawn -= OnEnemyDespawn;
    }

    void Update() {
        if (!IsServer) return;
        UpdateTarget();
        if (target){
            /*if (_IsNewTarget){
                StartRotating();
            }*/
            //Debug.DrawRay(NozzlePivot.position, (target.transform.position - NozzlePivot.position).normalized*DetectionRadius, Color.red, 2f);
            StartRotating();
            Shoot();    // server rpc?
        }
    }

    void UpdateTarget(){
        if (target == null){
            //Debug.Log("TurretController: target was null");
            List<(float distance, NetworkObject target)> orderedTargets = GetOrderedTargets();
            RaycastHit hit;
            foreach((float _, NetworkObject potentialTarget) in orderedTargets){
                //Debug.DrawRay(NozzlePivot.position, (potentialTarget.transform.position - NozzlePivot.position).normalized*DetectionRadius, Color.red, 2f);
                if (Physics.Raycast(transform.position, (potentialTarget.transform.position - transform.position).normalized, out hit, DetectionRadius, BulletLayerMask)){
                    if (hit.collider.CompareTag(_TargetTag)){
                        //Debug.Log("TurretController: found unobstructed new target");
                        target = hit.collider.GetComponent<NetworkObject>();
                        _IsNewTarget = true;
                        break;
                    }
                }else{
                    //Debug.Log("TurretController: raycast obstructed");
                }
            }
        }else{
            _IsNewTarget = false;
        }
        // do new raycast to see if current target is behind a building
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
        //Debug.Log("TurretController: ordered targets size: "+orderedTargets.Count);
        /*foreach((float _, NetworkObject potentialTarget) in orderedTargets){
            Debug.Log(potentialTarget.name);
        }*/
        //Debug.Log("TurretController: _InRange size: "+_InRange.Count);
        return orderedTargets;
    }

    // NOTE: This method causes the turret to rotate once towards the target in a fluid motion
    //       However, this may not "feel" good in terms of the game and may need a complete redo
    //       will probably swap to LookAt() in the future
    void StartRotating(){
        if (RotateCoroutine == null){
            RotateCoroutine = StartCoroutine(RotateToTarget());
        }
    }

    private IEnumerator RotateToTarget(){
        // only doing horizontal rotation for now until turret model is deicded on and pivot location is known
        Quaternion lookRotationStem = Quaternion.LookRotation(target.transform.position - StemPivot.position);
        Quaternion lookRotationNozzle = Quaternion.LookRotation(target.transform.position - NozzlePivot.position);
        // ensure this rotation is only horizontal
        Quaternion lookRotationY = Quaternion.Euler(0, lookRotationStem.eulerAngles.y, 0);
        Quaternion lookRotationX = Quaternion.Euler(lookRotationNozzle.eulerAngles.x, 0, 0);
        float time = 0;
        while (time < 1){
            StemPivot.rotation = Quaternion.Slerp(StemPivot.rotation, lookRotationY, time);
            NozzlePivot.localRotation = Quaternion.Slerp(NozzlePivot.localRotation, lookRotationX, time);
            time += Time.deltaTime * RotationSpeed;
            yield return null;
        }
        RotateCoroutine = null;
    }

    private IEnumerator DespawnTimer(){
        while (_elapsedTime <= _lifetime){
            _elapsedTime += Time.deltaTime;
            yield return null;
        }
        Debug.Log("TURRET: turret lifetime completed with total lifetime of: "+_lifetime);
        //Despawn(gameObject);      // tell game manager to do this instead?
    }

    public void ExtendTurretLifetime(float seconds){
        _lifetime += seconds;
    }

    void Shoot(){
        // raycast in forward direction
        // if raycast hits, do damage
        if (!IsServer) return;
        //Debug.Log("TURRET: shooting at target");
        _weapon.TurretItemLoopBehavior(gameObject, Time.time);
    }

    #region Enemy Detection
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && other.CompareTag(_TargetTag)){
            //Debug.Log("TurretController: OnTriggerEnter called with tag: "+other.gameObject.tag);
            //Debug.Log("TurretController: Target Tag is "+_TargetTag);
            AddUnique(netObj);
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

    #region helpers
    // prevent duplicate entries into target list
   void AddUnique(NetworkObject netobj){
        if (!_InRange.Contains(netobj)){
            _InRange.Add(netobj);
        }
    }


    #endregion
}
