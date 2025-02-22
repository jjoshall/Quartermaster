using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    [Header("Enemy Settings")]
    public float attackRange = 2f;
    public int damage = 1;
    public EnemyType enemyType;

    protected NavMeshAgent agent;
    protected Transform target;
    protected Health health;

    protected virtual void Awake() {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

        if (health != null) {
            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;
        }
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        FindNearestPlayer();
        InvokeRepeating(nameof(UpdateTarget), 0, 0.1f);
    }

    protected virtual void Update() {
        if (!IsServer || target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange) {
            Attack();
        }
        else {
            agent.SetDestination(target.position);
        }
    }

    protected void FindNearestPlayer() {
        // CHANGE THIS TO NAVSCRIPT???

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        target = players[0].transform;
        foreach (GameObject player in players) {
            if (Vector3.Distance(transform.position, player.transform.position) < Vector3.Distance(transform.position, target.position)) {
                target = player.transform;
            }
        }
    }

    protected void UpdateTarget() {
        FindNearestPlayer();
    }

    protected abstract void Attack();

    protected virtual void OnDamaged(float damage, GameObject damageSource) {
        Debug.Log(enemyType + " took " + damage + " damage!");
    }

    protected virtual void OnDie() {
        EnemySpawner.instance.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }
}

public enum EnemyType {
    Melee,
    Ranged
}