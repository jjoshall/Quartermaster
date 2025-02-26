using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    [Header("Enemy Settings")]
    public float attackRange = 2f;
    public int damage = 1;
    public EnemyType enemyType;
    private float _nextTargetUpdateTime;

    protected NavMeshAgent agent;
    protected Transform target;
    protected Health health;
    public EnemySpawner enemySpawner;

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            if (agent != null) agent.enabled = false;
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

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
        // Update target every .2 seconds
        if (Time.time >= _nextTargetUpdateTime) {
            _nextTargetUpdateTime = Time.time + 0.2f;
            UpdateTarget();
        }

        if (target == null) return;

        //float distance = Vector3.Distance(transform.position, target.position);
        bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

        if (inRange) {
            Attack();
        }
        else {
            agent.SetDestination(target.position);
        }
    }

    protected void UpdateTarget() {
        if (enemySpawner == null || enemySpawner.playerList == null) return;

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