using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    [Header("Enemy Settings")]
    protected virtual float attackCooldown { get; } = 2f;
    protected virtual float attackRange { get; } = 2f;
    protected virtual int damage { get; } = 10;
    [SerializeField] protected float attackRadius = 2f;
    public EnemyType enemyType;
    private float _nextTargetUpdateTime;
    [SerializeField] private float _separationRadius = 10f;
    [SerializeField] private float _separationStrength = 3f;
    private Vector3 enemySeparationVector;
    protected NavMeshAgent agent;
    protected Transform target;
    protected Health health;
    protected Renderer renderer;
    public EnemySpawner enemySpawner;

    // Used to sync color across clients
    private NetworkVariable<float> healthRatio = new NetworkVariable<float>(1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn() {
        agent = GetComponent<NavMeshAgent>();
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

        // Update target every .05 seconds
        //if (Time.time >= _nextTargetUpdateTime) {
        //    _nextTargetUpdateTime = Time.time + 0.05f;
        //    UpdateTarget();
        //}

        UpdateTarget();

        if (target != null) {
            bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

            if (inRange) {
                Attack();
            }
            else {
                CalculateSeparationOffset();
                agent.SetDestination(target.position);

                this.gameObject.transform.position += enemySeparationVector * Time.deltaTime;
            }
        }
    }

    // IMPLEMENT THESE TWO METHODS FOR NEW ENEMIES
    protected abstract void UpdateTarget();
    protected abstract void Attack();

    [ServerRpc(RequireOwnership = false)]
    protected void AttackServerRpc(bool destroyAfterAttack = false) {
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
        Debug.Log(enemyType + " took " + damage + " damage!");
    }

    protected virtual void OnDie() {
        ItemManager.instance.ThresholdBurstDrop(transform.position);
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
}

public enum EnemyType {
    Melee,
    Ranged
}