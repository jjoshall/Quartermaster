using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;
using TMPro;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    // THIS IS FOR GAME MANAGER, you can change values in the
    // inspector on the 'GameManager' gameobject for each enemy
    #region Enemy Values For Game Manager
    protected abstract float GetAttackCooldown();
    protected abstract float GetAttackRange();
    protected abstract int GetDamage();
    protected abstract float GetAttackRadius();
    protected abstract bool GetUseGlobalTarget();
    protected abstract float GetInitialHealth();

    // Assigning values from GameManager to the enemy
    protected virtual float attackCooldown => GetAttackCooldown();
    protected virtual float attackRange => GetAttackRange();
    protected virtual int damage => GetDamage();
    protected virtual float attackRadius => GetAttackRadius();
    protected virtual bool useGlobalTarget => GetUseGlobalTarget();

    #endregion

    [Header("Enemy Settings")]
    [SerializeField] private float _attackDelay = 2.0f;
    private float _lastAttackTime = 0.0f;


    [Header("Required Scripts for Enemies")]
    protected NavMeshAgent agent;
    protected Health health;
    public EnemySpawner enemySpawner;

    [Header("Enemy pathing")]
    [SerializeField] private float _localDetectionRange = 20f; // how far to switch from global to direct aggro
    
    [Header("Separation Pathing")]
    [SerializeField] private float _separationRadius = 10f;
    [SerializeField] private float _separationStrength = 3f;
    private Vector3 _velocityVector = Vector3.zero; // used for boids separation

    // Speed run-time variables, think Norman added this
    // Yes I did - Norman
    protected float _baseSpeed = 0.0f;
    protected float _baseAcceleration = 0.0f;
    protected NetworkVariable<int> n_isSlowed = new NetworkVariable<int>(0); // int is used in case of multiple slow traps.
    protected NetworkVariable<float> n_slowMultiplier = new NetworkVariable<float>(0.0f);
    [HideInInspector] public float AISpeedMultiplier = 1.0f; // don't change. set by AIDirector at run-time.
    [HideInInspector] public float AIDmgMultiplier = 1.0f;

    [SerializeField] private GameObject floatingTextPrefab;     // to spawn floating damage numbers
    private bool _isAttacking = false;      // to prevent multiple attacks happening at once
    private float _attackTimer = 0.0f;      // to prevent attacks happening too quickly
    public EnemyType enemyType;     // two enemy types at the moment: Melee and Ranged
    protected Vector3 targetPosition; 

    public override void OnNetworkSpawn() {
       
        agent = GetComponent<NavMeshAgent>();
        _baseSpeed = agent.speed;
        _baseAcceleration = agent.acceleration;
        _velocityVector = Vector3.zero;

        health = GetComponent<Health>();

        if (!IsServer) {
            agent.enabled = false;
            enabled = false;
        }
        else {
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

            if (health != null) {
                health.OnDamaged += OnDamaged;
                health.OnDie += OnDie;
                health.CurrentHealth.Value = GetInitialHealth();
            }
            enemySpawner = EnemySpawner.instance;

            // used to switch out coroutine for timer (got confused bc of explosive enemy anims lol and its 4 am so ill figure it out later)
            _attackTimer = attackCooldown;
        }
    }

    private void ClientDisconnected(ulong u) {
        // target = null;
    }

    protected virtual void Update() {
        if (!IsServer) return;
        // Debug.Log ("BaseEnemy Update()");
        UpdateTarget(); // sets targetPosition to closest player within localDetectionRange, else global target

        if (targetPosition != null) {
            // Each enemy has a different attack range
            bool inRange = Vector3.Distance(transform.position, targetPosition) <= attackRange;

            // If a player's in range for that enemy, attack.
            if (inRange && _lastAttackTime + attackCooldown < Time.time) {
                Debug.Log ("BaseEnemy Update() in range for attack.");
                Attack();
                _lastAttackTime = Time.time;
            }
            else {
                // Debug.Log ("BaseEnemy Update(), not in range: " + attackRange + ", calling SetDestination()");
                agent.SetDestination(targetPosition);  
            }
        }
    }
    private void LateUpdate() {
        ApplySeparationForce(); // boids separation, capped to X neighbors    
    }

    // IMPLEMENT THIS METHOD FOR NEW/EACH ENEMIES
    protected abstract void Attack();

    // If player in localDetectionRange, target closest.
    // Else target global.
    protected void UpdateTarget() {
        if (enemySpawner == null || enemySpawner.playerList == null) return;
        // Debug.Log ("BaseEnemy UpdateTarget()"); 

        GameObject closestPlayer = null;
        foreach (GameObject obj in enemySpawner.playerList) {
            if (Vector3.Distance(transform.position, obj.transform.position) <= _localDetectionRange) {
                if (closestPlayer == null) {
                    closestPlayer = obj;
                }
                else if (Vector3.Distance(transform.position, obj.transform.position) < Vector3.Distance(transform.position, closestPlayer.transform.position)) {
                    closestPlayer = obj;
                }
            }
        }

        if (closestPlayer == null){
            targetPosition = EnemySpawner.instance.globalAggroTarget;
        } else {
            targetPosition = closestPlayer.transform.position;
        }
    }


    // Apply boids separation for fluid-like emergent behavior.
    private void ApplySeparationForce() {
        // Vector3 separationForce = Vector3.zero;
        int count = 0;
        int maxCount = 20; // cap it in case of performance issues.
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int enemyLayerMask = 1 << enemyLayer;

        if (_velocityVector == null) {
            // Debug.Log ("SeparationForce() velocityVector is null, setting to Vector3.zero");
            _velocityVector = Vector3.zero;
        }
        Vector3 separationVector = Vector3.zero;

        Collider[] neighbors = Physics.OverlapSphere(transform.position, _separationRadius, enemyLayerMask);
        if (neighbors.Length == 0) return; // no neighbors, no separation
        // combine the current velocity vector 
        foreach (var neighbor in neighbors) {
            if (neighbor.gameObject == gameObject) continue; // don't apply force to self.
            if (!neighbor.gameObject.CompareTag("Enemy")) continue;

            Vector3 dir = transform.position - neighbor.transform.position; // direction towards neighbor
            float magnitude = dir.magnitude;
            if (magnitude <= 0.1) magnitude = 0.1f;
            Vector3 separationForce = dir.normalized / magnitude;       // inverse proportional to distance
            
            separationVector += separationForce;
            // neighbor.transform.GetComponent<Rigidbody>().AddForce(separationForce); 

            count++;
            if (count > maxCount) break;
        }

        separationVector *= _separationStrength;

        // apply the separation vector with the current velocity vector of the navmesh agent, agent.velocity
        _velocityVector *= 0.5f;
        _velocityVector += separationVector;

        // Debug.Log ("velocityVector is now: " + _velocityVector);

        agent.Move(_velocityVector * Time.deltaTime);  // move the agent with the velocity vector

        // agent.velocity is constantly the path direction after calling setDestination
        // velocityVector stores the separation force 
    }

    // Called when enemy takes damage
    protected virtual void OnDamaged(float damage, GameObject damageSource) {
        if (floatingTextPrefab != null) {
            ShowFloatingTextServerRpc(damage);  // show floating damage numbers on server/client
        }
        
        GameManager.instance.AddEnemyDamageServerRpc(damage);   // tracks total damage dealt to enemies
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShowFloatingTextServerRpc(float damage) {
        ShowFloatingTextClientRpc(damage);
    }

    [ClientRpc]
    void ShowFloatingTextClientRpc(float damage) {
        var go = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity, transform);
        go.GetComponent<TextMeshPro>().SetText(damage.ToString());
    }

    // Called when enemy dies
    protected virtual void OnDie() {
        ItemManager.instance.ThresholdBurstDrop(transform.position);    // norman added this, has a chance to burst drop items
        GameManager.instance.IncrementEnemyKillsServerRpc();    // add to enemy kill count
        enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());  // remove enemy from scene
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsServer && NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
        }

        if (health != null) {
            health.OnDamaged -= OnDamaged;
            health.OnDie -= OnDie;
        }
    }

    #region SpeedChanged
    [ServerRpc(RequireOwnership = false)]   
    public virtual void UpdateSpeedServerRpc(){
        float finalSpeed = _baseSpeed;
        float finalAcceleration = _baseAcceleration;

        if (n_isSlowed.Value > 0){
            finalSpeed *= 1 - n_slowMultiplier.Value;
            finalAcceleration *= 1 - n_slowMultiplier.Value;
        }

        UpdateSpeedClientRpc(finalSpeed, finalAcceleration);
    }

    [ClientRpc]
    private void UpdateSpeedClientRpc(float finalSpeed, float finalAcceleration){
        // Debug.Log ("Updating speed client rpc, original speed & acceleration: " + agent.speed + ", " + agent.acceleration);
        agent.speed = finalSpeed;
        agent.acceleration = finalAcceleration;
        agent.velocity = agent.velocity.normalized * finalSpeed;
        // Debug.Log ("Updated speed & acceleration: " + agent.speed + ", " + agent.acceleration);
    }

    [ServerRpc(RequireOwnership = false)]   
    public void ApplySlowDebuffServerRpc(){
        n_isSlowed.Value = n_isSlowed.Value + 1;
        n_slowMultiplier.Value = GameManager.instance.SlowTrap_SlowByPct;
        UpdateSpeedServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]   
    public void RemoveSlowDebuffServerRpc(){
        n_isSlowed.Value = n_isSlowed.Value - 1;
        n_slowMultiplier.Value = GameManager.instance.SlowTrap_SlowByPct;
        UpdateSpeedServerRpc();
    }

    #endregion
}

// Might need to add a special enemy type or something for new enemies maybe even explosive enemies
public enum EnemyType {
    Melee,
    Ranged
}