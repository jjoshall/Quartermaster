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

    [Header("Separation Pathing")]
    [SerializeField] private float _separationRadius = 10f;
    [SerializeField] private float _separationStrength = 3f;
    private Vector3 enemySeparationVector;

    [Header("Required Scripts for Enemies")]
    protected NavMeshAgent agent;
    protected Health health;
    public EnemySpawner enemySpawner;

    // Speed run-time variables, think Norman added this
    protected float _baseSpeed = 0.0f;
    protected float _baseAcceleration = 0.0f;
    protected NetworkVariable<int> n_isSlowed = new NetworkVariable<int>(0); // int is used in case of multiple slow traps.
    protected NetworkVariable<float> n_slowMultiplier = new NetworkVariable<float>(0.0f);

    [SerializeField] private GameObject floatingTextPrefab;     // to spawn floating damage numbers
    private bool _isAttacking = false;      // to prevent multiple attacks happening at once
    private float _attackTimer = 0.0f;      // to prevent attacks happening too quickly
    public EnemyType enemyType;     // two enemy types at the moment: Melee and Ranged
    protected Transform target;     // for pathing to player

    public override void OnNetworkSpawn() {
       
        agent = GetComponent<NavMeshAgent>();
        _baseSpeed = agent.speed;
        _baseAcceleration = agent.acceleration;

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
        target = null;
    }

    protected virtual void Update() {
        if (!IsServer) return;

        UpdateTarget();

        if (target != null) {
            // Each enemy has a different attack range
            bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

            // If a player's in range for that enemy, attack.
            if (inRange && !_isAttacking) {
                // Add a delay before attacking
                /// Changing to timer instead of coroutine (WIP)
                //if (_attackTimer <= 0) {
                //    _isAttacking = true;
                //    OnAttackStart();    // for explosive enemy, sets isBlinking to true to make sound/animation synced
                //    Attack();
                //    _isAttacking = false;
                //    _attackTimer = attackCooldown;
                //}
                //_attackTimer -= Time.deltaTime;

                StartCoroutine(DelayAttack());
            }
            else {
                // If not in range, path to target
                CalculateSeparationOffset();
                // Ranged and explosive enemies use global target, so they'll go to past positions of player
                if (useGlobalTarget) {
                    Vector3 destination = enemySpawner.GetGlobalAggroTarget();
                    agent.SetDestination(destination);
                }
                // Normal melee enemies will just follow closest player
                else {
                    agent.SetDestination(target.position);
                }              

                this.gameObject.transform.position += enemySeparationVector * Time.deltaTime;
            }
        }
    }

    // IMPLEMENT THIS METHOD FOR NEW/EACH ENEMIES
    protected abstract void Attack();

    // How enemies find their target player
    protected virtual void UpdateTarget() {
        if (enemySpawner == null || enemySpawner.playerList == null) return;

        // Norman's global aggro: ranged and explosive enemies use this,
        // will make enemies pick a random player's position every 10 seconds to go to that position
        if (useGlobalTarget) {
            GameObject closestPlayerToGlobalTarget = null;
            float closestDistance = float.MaxValue;
            Vector3 globalTarget = enemySpawner.GetGlobalAggroTarget();

            foreach (GameObject player in enemySpawner.playerList) {
                if (player == null) continue;

                float distance = Vector3.Distance(player.transform.position, globalTarget);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestPlayerToGlobalTarget = player;
                }
            }

            target = closestPlayerToGlobalTarget != null ? closestPlayerToGlobalTarget.transform : null;
        }
        // Melee enemies use this, just follows closest player
        else {
            GameObject closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject obj in enemySpawner.playerList) {
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance < closestDistance) {
                    closestPlayer = obj;
                    closestDistance = distance;
                }
            }

            target = closestPlayer != null ? closestPlayer.transform : null;
        }
    }

    // Can use this to do anything you need before attacking, like setting a bool for animation
    //protected virtual void OnAttackStart() {}

    // Changing soon to timer instead of coroutine
    protected virtual IEnumerator DelayAttack() {
        _isAttacking = true;
        yield return new WaitForSeconds(attackCooldown);
        Attack();
        _isAttacking = false;
    }

    // Make sure attacks are actually happening on the server, melee and explosive enemies use this
    [ServerRpc(RequireOwnership = false)]
    protected virtual void AttackServerRpc(bool destroyAfterAttack = false) {
        if (!IsServer) return;
        // OverlapSphere will find all colliders in the attackRadius around the enemy
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (var hitCollider in hitColliders) {
            if (hitCollider.CompareTag("Player")) {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }

        // For explosive enemies, this will just remove the explosive enemy from scene
        if (destroyAfterAttack) {
            enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
        }
    }

    // Norman's separation pathing: enemies will apply a force to
    // other enemies in a separation radius to move away from each other
    private void CalculateSeparationOffset() {
        Vector3 separationForce = Vector3.zero;
        int count = 0;
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        Collider[] neighbors = Physics.OverlapSphere(transform.position, _separationRadius, enemyLayer);

        foreach (var neighbor in neighbors) {
            if (neighbor.gameObject == gameObject) continue;

            var dir = neighbor.transform.position - transform.position;
            var distance = dir.magnitude;
            if (distance < _separationRadius && distance > 0.1f) {
                var away = -dir.normalized;
                separationForce += (away / distance) * _separationStrength;
                count++;
            }
        }

        if (count > 0) {
            separationForce /= count;
        }

        enemySeparationVector = separationForce;
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
    protected virtual void UpdateSpeedServerRpc(){
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
        Debug.Log ("Updating speed client rpc, original speed & acceleration: " + agent.speed + ", " + agent.acceleration);
        agent.speed = finalSpeed;
        agent.acceleration = finalAcceleration;
        agent.velocity = agent.velocity.normalized * finalSpeed;
        Debug.Log ("Updated speed & acceleration: " + agent.speed + ", " + agent.acceleration);
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