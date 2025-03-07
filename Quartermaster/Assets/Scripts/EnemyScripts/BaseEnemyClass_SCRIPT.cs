using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections;

public abstract class BaseEnemyClass_SCRIPT : NetworkBehaviour {
    [Header("Enemy Settings")]
    protected virtual float attackCooldown => 2f;
    protected virtual float attackRange => 2f;
    protected virtual int damage => 2;
    protected virtual float attackRadius => 2f;
    public EnemyType enemyType;
    public GameObject floatingTextPrefab;
    private bool _isAttacking = false;
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

        UpdateTarget();

        if (target != null) {
            bool inRange = Vector3.Distance(transform.position, target.position) <= attackRange;

            if (inRange && !_isAttacking) {
                // Add a delay before attacking
                StartCoroutine(DelayAttack());
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

        if (floatingTextPrefab != null)
        {
            ShowFloatingText();
        }
        
        GameManager.instance.AddEnemyDamageServerRpc(damage);
        //Debug.Log("Total damage taken by enemies: " + GameManager.instance.totalDamageDealtToEnemies.Value);
    }

    void ShowFloatingText()
    {
        var go = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity, transform);
        go.GetComponent<TextMesh>().text = health.CurrentHealth.Value.ToString();
    }

    protected virtual void OnDie() {
        ItemManager.instance.ThresholdBurstDrop(transform.position);

        GameManager.instance.IncrementEnemyKillsServerRpc();
        //Debug.Log("Total enemy kills: " + GameManager.instance.totalEnemyKills.Value);

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