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

    // All enemies will use this
    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            agent.enabled = false;
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        renderer = GetComponent<Renderer>();

        if (health != null) {
            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;
        }

        enemySpawner = EnemySpawner.instance;
    }

    private void ClientDisconnected(ulong u) {
        target = null;
    }

    protected virtual void Update() {
        // Update target every .15 seconds
        if (Time.time >= _nextTargetUpdateTime) {
            _nextTargetUpdateTime = Time.time + 0.15f;
            UpdateTarget();
        }

        bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

        if (inRange) {
            Attack();
        }
        else {
            agent.SetDestination(target.position);
        }
    }

    protected abstract void UpdateTarget();

    protected abstract void Attack();

    protected virtual void OnDamaged(float damage, GameObject damageSource) {
        Debug.Log(enemyType + " took " + damage + " damage!");
    }

    protected virtual void OnDie() {
        enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }
}

public enum EnemyType {
    Melee,
    Ranged
}