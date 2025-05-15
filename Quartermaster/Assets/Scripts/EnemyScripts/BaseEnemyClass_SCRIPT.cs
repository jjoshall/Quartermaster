using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Analytics;
using Unity.Services.Analytics;

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
    public GameObject originalPrefab; // this is for the object pooling to know to use this

    [Header("Required Scripts for Enemies")]
    protected NavMeshAgent agent;
    protected Health health;
    public EnemySpawner enemySpawner;
    protected SoundEmitter[] soundEmitters;
    protected Animator animator;

    [Header("Enemy pathing")]
    [SerializeField] private float _localDetectionRange = 20f; // how far to switch from global to direct aggro
    
    [Header("Separation Pathing")]
    [SerializeField, Tooltip("Higher neighbor counts computation heavy.")] private bool _useSeparation = false;
    [SerializeField, Tooltip("Higher neighbor counts computation heavy.")] private int _maxNeighbors = 3; // max number of neighbors to consider for separation
    [SerializeField] private float _separationRadius = 10f;
    [SerializeField] private float _separationStrength = 3f;
    [SerializeField, Range(0.0f, 1.0f)] private float _separationDecay = 0.9f; // per frame multiplier on velocity vector
    private Vector3 _velocityVector = Vector3.zero; // used for boids separation

    [Header("For Animation Culling")]
    private float _distanceToNearestPlayer = Mathf.Infinity; // used to determine if we should animate or not
    private float _distanceCheckTimer = 0f;
    private const float _distanceCheckInterval = 1f; // how often to check distance to nearest player

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
    public EnemyType enemyType;

    protected Vector3 targetPosition; 
    protected bool targetIsPlayer;

    protected List<GameObject> playersThatHitMe;

    public override void OnNetworkSpawn() {
        agent = GetComponent<NavMeshAgent>();
        _baseSpeed = agent.speed;
        _baseAcceleration = agent.acceleration;
        _velocityVector = Vector3.zero;

        health = GetComponent<Health>();

        playersThatHitMe = new List<GameObject>();

        animator = GetComponentInChildren<Animator>();
        if (animator != null) {
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        soundEmitters = GetComponentsInChildren<SoundEmitter>(true);

        if (!IsServer) {
            agent.enabled = false;
            enabled = false;
        }
        else {
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

    #region PathingLogic
    protected virtual void Update() {
        if (!IsServer) return;

        // Timer-based distance check
        _distanceCheckTimer += Time.deltaTime;
        if (_distanceCheckTimer >= _distanceCheckInterval) {
            _distanceCheckTimer = 0f; // reset timer
            UpdateDistanceToNearestPlayer();
        }

        UpdateTarget(); // sets targetPosition to closest player within localDetectionRange, else global target
        Pathing(); // if in attackRange
    }

    public void OnEnable() {
        if (agent != null) {
            agent.enabled = true;
        }
    }

    public void OnDisable() {
        if (agent != null) {
            agent.enabled = false;
        }
    }

    private void UpdateDistanceToNearestPlayer() {
        _distanceToNearestPlayer = Mathf.Infinity; // reset distance to nearest player

        foreach (var player in enemySpawner.activePlayerList) {
            if (player == null) continue;
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < _distanceToNearestPlayer) {
                _distanceToNearestPlayer = distance;
            }
        }
    }

    private void Pathing() {        
        if (targetPosition != null) {
            // Each enemy has a different attack range
            bool inRange = Vector3.Distance(transform.position, targetPosition) <= attackRange;

            // If a player's in range for that enemy, attack.
            if (targetIsPlayer && inRange && _lastAttackTime + attackCooldown < Time.time) {
                // Enemies in range to attack look at the player.
                Vector3 lookPosition = targetPosition;
                lookPosition.y = transform.position.y;
                transform.LookAt(lookPosition);
                // Attack & set last attack time for timer.
                Attack();
                _lastAttackTime = Time.time;
            }
            else {
                agent.SetDestination(targetPosition);  
            }
        }
    }
    #endregion 

    #region BoidSeparation
    private void LateUpdate() {
        if (!IsServer) return;
        if (_useSeparation) {
            ApplySeparationForce(); // Max neighbors also parameterized. Higher values computation heavy.
                                    // Creates slight fluid-like expansion of enemy packs.
        }
    }
    // Apply boids separation for fluid-like emergent behavior.
    private void ApplySeparationForce() {
        int count = 0;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int enemyLayerMask = 1 << enemyLayer;

        if (_velocityVector == null) {
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

            count++;
            if (count > _maxNeighbors) break;
        }

        separationVector *= _separationStrength;

        // apply the separation vector with the current velocity vector of the navmesh agent, agent.velocity
        _velocityVector *= _separationDecay;
        _velocityVector += separationVector;

        agent.Move(_velocityVector * Time.deltaTime);  // move the agent with the velocity vector
    }
    #endregion

    // IMPLEMENT THIS METHOD FOR NEW/EACH ENEMIES
    protected abstract void Attack();

    protected bool ShouldAnimate() {
        return _distanceToNearestPlayer < 30f; // if the player is within 30 units, animate
    }

    // If player in localDetectionRange, target closest.
    // Else target global.
    protected void UpdateTarget() {
        if (enemySpawner == null || enemySpawner.activePlayerList == null) return;

        // Lowest priority = global target.
        GameObject closestPlayer = null;
        targetPosition = EnemySpawner.instance.globalAggroTarget;
        targetIsPlayer = false;

        // Second priority = any players that have hit this enemy, possibly outside of detection range.
        // WIP: We can possibly abstract this function to create enemies that can propagate aggro.
        if (playersThatHitMe.Count > 0) {
            closestPlayer = ClosestInPlayersThatHitMe();
            if (closestPlayer){
                targetPosition = closestPlayer.transform.position;
                targetIsPlayer = true;
            }
        }
        
        // Highest priority = closest player in localDetectionRange.
        if (enemySpawner.activePlayerList.Count > 0){
            closestPlayer = ClosestPlayerInLocalDetectionRange();
            if (closestPlayer){
                targetPosition = closestPlayer.transform.position;
                targetIsPlayer = true;
            }
        }
    }

    private GameObject ClosestInPlayersThatHitMe(){
        GameObject closestPlayer = null;

        foreach (GameObject obj in playersThatHitMe) {
            if (obj == null) continue;
            if (closestPlayer == null) {
                closestPlayer = obj;
            }
            else if (Vector3.Distance(transform.position, obj.transform.position) < Vector3.Distance(transform.position, closestPlayer.transform.position)) {
                closestPlayer = obj;
            }
        }
        return closestPlayer;
    }

    private GameObject ClosestPlayerInLocalDetectionRange(){
        GameObject closestPlayer = null;

        foreach (GameObject obj in enemySpawner.activePlayerList) {
            if (obj == null) continue;
            if (Vector3.Distance(transform.position, obj.transform.position) <= _localDetectionRange) {
                if (closestPlayer == null) {
                    closestPlayer = obj;
                }
                else if (Vector3.Distance(transform.position, obj.transform.position) < Vector3.Distance(transform.position, closestPlayer.transform.position)) {
                    closestPlayer = obj;
                }
            }
        }
        return closestPlayer;
    }

    // For sound
    protected void PlaySoundForEmitter(string emitterId, Vector3 position) {
        foreach (SoundEmitter emitter in soundEmitters) {
            if (emitter.emitterID == emitterId) {
                emitter.PlayNetworkedSound(position);
                return;
            }
        }
    }

    // Called when enemy takes damage
    protected virtual void OnDamaged(float damage, GameObject damageSource) {
        Vector3 floatingTextPosition = transform.position;
        
        if (floatingTextPrefab != null) {
            enemySpawner.SpawnDamageNumberFromPool(floatingTextPrefab, floatingTextPosition, damage);  // show floating damage numbers with pooling
        }

        PlaySoundForEmitter("melee_damaged", transform.position);

        //GameManager.instance.AddEnemyDamageServerRpc(damage);   // tracks total damage dealt to enemies
        if (!playersThatHitMe.Contains(damageSource)) {
            playersThatHitMe.Add(damageSource);
        } // prevent multiple hits from same player
    }

    // Called when enemy dies
    protected virtual void OnDie() {
        if (AnalyticsManager_SCRIPT.Instance != null && AnalyticsManager_SCRIPT.Instance.IsAnalyticsReady()) {
            AnalyticsService.Instance.RecordEvent("EnemyKilled");
        }
        else {
            Debug.LogWarning("Analytics not ready or instance is null.");
        }

        ItemManager.instance.RollDropTable(transform.position);    // norman added this, has a chance to burst drop items
        GameManager.instance.IncrementEnemyKillsServerRpc();    // add to enemy kill count
        enemySpawner.RemoveEnemyFromList(gameObject);   // remove enemy from list of enemies
        enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());  // remove enemy from scene
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

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
        agent.speed = finalSpeed;
        agent.acceleration = finalAcceleration;
        agent.velocity = agent.velocity.normalized * finalSpeed;
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
    Ranged,
    Explosive
}