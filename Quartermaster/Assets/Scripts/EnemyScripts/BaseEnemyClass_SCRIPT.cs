using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    [Header("Enemy Settings")]
    protected virtual float attackCooldown { get; } = 2f;
    protected virtual float attackRange { get; } = 2f;
    protected virtual int damage { get; } = 10;
    public EnemyType enemyType;
    private float _nextTargetUpdateTime;
    protected NavMeshAgent agent;
    protected Transform target;
    protected Health health;
    protected Renderer renderer;
    public EnemySpawner enemySpawner;

    // Used to sync color across clients
    private NetworkVariable<float> healthRatio = new NetworkVariable<float>(1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //public override void OnNetworkSpawn() {
    //    if (!IsServer) {
    //        enabled = false;
    //        agent.enabled = false;
    //        return;
    //    }

    //    NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

    //    agent = GetComponent<NavMeshAgent>();
    //    health = GetComponent<Health>();
    //    renderer = GetComponent<Renderer>();

    //    if (health != null) {
    //        health.OnDamaged += OnDamaged;
    //        health.OnDie += OnDie;
    //    }

    //    enemySpawner = EnemySpawner.instance;
    //}

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

    private void OnHealthRatioChanged (float previousValue, float newValue) {
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

        // Update target every .15 seconds
        if (Time.time >= _nextTargetUpdateTime) {
            _nextTargetUpdateTime = Time.time + 0.15f;
            UpdateTarget();
        }

        if (target != null) {
            bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

            if (inRange) {
                Attack();
            }
            else {
                agent.SetDestination(target.position);
            }
        }
    }

    // IMPLEMENT THESE TWO METHODS FOR NEW ENEMIES
    protected abstract void UpdateTarget();
    protected abstract void Attack();

    protected virtual void OnDamaged(float damage, GameObject damageSource) {
        Debug.Log(enemyType + " took " + damage + " damage!");
    }

    protected virtual void OnDie() {
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