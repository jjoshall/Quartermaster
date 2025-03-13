using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;
using TMPro;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    protected abstract float GetAttackCooldown();
    protected abstract float GetAttackRange();
    protected abstract int GetDamage();
    protected abstract float GetAttackRadius();
    protected abstract bool GetUseGlobalTarget();
    protected abstract float GetInitialHealth();
    
    [Header("Enemy Settings")]
    protected virtual float attackCooldown => GetAttackCooldown();
    protected virtual float attackRange => GetAttackRange();
    protected virtual int damage => GetDamage();
    protected virtual float attackRadius => GetAttackRadius();
    protected virtual bool useGlobalTarget => GetUseGlobalTarget();
    public EnemyType enemyType;
    [SerializeField] private float _separationRadius = 10f;
    [SerializeField] private float _separationStrength = 3f;
    protected NavMeshAgent agent;
    protected Health health;
    protected Renderer renderer;
    public EnemySpawner enemySpawner;

    [SerializeField] private GameObject floatingTextPrefab;
    private bool _isAttacking = false;
    private Vector3 enemySeparationVector;
    protected Transform target;

    // Speed run-time variables
    protected float _baseSpeed = 0.0f;
    protected float _baseAcceleration = 0.0f;
    protected NetworkVariable<int> n_isSlowed = new NetworkVariable<int>(0); // int is used in case of multiple slow traps.
    protected NetworkVariable<float> n_slowMultiplier = new NetworkVariable<float>(0.0f);


    // Used to sync color across clients
    private NetworkVariable<float> healthRatio = new NetworkVariable<float>(1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn() {
        agent = GetComponent<NavMeshAgent>();
        _baseSpeed = agent.speed;
        _baseAcceleration = agent.acceleration;


        health = GetComponent<Health>();
        renderer = GetComponent<Renderer>();

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
        }

        // Subscribe to the network variable change event on all clients
        healthRatio.OnValueChanged += OnHealthRatioChanged;
    }

    private void OnHealthRatioChanged(float previousValue, float newValue) {
        if (renderer != null) {
            renderer.material.color = new Color(newValue, 0f, 0f);
        }
    }

    private void ClientDisconnected(ulong u) {
        target = null;
    }

    protected virtual void Update() {
        if (!IsServer) return;

        if (health != null) {
            healthRatio.Value = health.GetRatio();
        }

        UpdateTarget();

        if (target != null) {
            bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

            if (inRange && !_isAttacking) {
                // Add a delay before attacking
                StartCoroutine(DelayAttack());
            }
            else {
                CalculateSeparationOffset();
                if (useGlobalTarget) {
                    Vector3 destination = enemySpawner.GetGlobalAggroTarget();
                    agent.SetDestination(destination);
                }
                else {
                    agent.SetDestination(target.position);
                }              

                this.gameObject.transform.position += enemySeparationVector * Time.deltaTime;
            }
        }
    }

    // IMPLEMENT THIS METHOD FOR NEW ENEMIES
    protected abstract void Attack();

    protected virtual void UpdateTarget() {
        if (enemySpawner == null || enemySpawner.playerList == null) return;

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

    protected virtual IEnumerator DelayAttack() {
        _isAttacking = true;
        yield return new WaitForSeconds(attackCooldown);
        Attack();
        _isAttacking = false;
    }

    [ServerRpc(RequireOwnership = false)]
    protected virtual void AttackServerRpc(bool destroyAfterAttack = false) {
        if (!IsServer) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (var hitCollider in hitColliders) {
            if (hitCollider.CompareTag("Player")) {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }

        if (destroyAfterAttack) {
            enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
        }
    }

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

    protected virtual void OnDamaged(float damage, GameObject damageSource) {
        //Debug.Log(enemyType + " took " + damage + " damage!");

        if (floatingTextPrefab != null) {
            ShowFloatingTextServerRpc(damage);
        }
        
        GameManager.instance.AddEnemyDamageServerRpc(damage);
        //Debug.Log("Total damage taken by enemies: " + GameManager.instance.totalDamageDealtToEnemies.Value);
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

    protected virtual void OnDie() {
        ItemManager.instance.ThresholdBurstDrop(transform.position);

        GameManager.instance.IncrementEnemyKillsServerRpc();
        GameManager.instance.AddScoreServerRpc(50);
        Debug.Log("Total score " + GameManager.instance.totalScore.Value);

        enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        healthRatio.OnValueChanged -= OnHealthRatioChanged;

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

public enum EnemyType {
    Melee,
    Ranged
}