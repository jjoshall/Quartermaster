using Unity.Netcode;
using System.Collections;
using UnityEngine;

public class ExplosiveMeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    protected override float attackCooldown => 2.37f;
    protected override float attackRange => 10f;
    protected override int damage => 60;
    protected override float attackRadius => 8f;

    private bool _isExploding = false;

    #region Explosion Blinking Visualization
    private NetworkVariable<bool> isBlinking = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Color originalColor;
    [SerializeField] private float blinkSpeed = 5f;
    // [SerializeField] private float normalSpeed = 5f;
    [Range(1f, 3f)]
    [SerializeField] private float blinkingSpeedMultiplier = 1.3f;

    private Animator animator;

    [Header("Armature Settings")]
    [SerializeField] private Transform _wheels;
    

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        animator = GetComponentInChildren<Animator>();
        
        if (renderer != null) {
            originalColor = renderer.material.color;
        }

        isBlinking.OnValueChanged += OnBlinkingStateChanged;
    }

    // This function gets called whenever the value of isBlinking changes
    private void OnBlinkingStateChanged(bool oldValue, bool newValue) {
        if (newValue) {
            if (IsServer && agent != null) {
                // agent.speed = blinkingSpeed;
                UpdateSpeedServerRpc();
                animator.SetBool("TransitionToExplode", true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    protected override void UpdateSpeedServerRpc(){
        float finalSpeed = _baseSpeed;  
        float finalAcceleration = _baseAcceleration;

        if (n_isSlowed.Value > 0){
            finalSpeed *= 1 - n_slowMultiplier.Value;
            finalAcceleration *= 1 - n_slowMultiplier.Value;
        }

        if (isBlinking.Value){
            finalSpeed *= blinkingSpeedMultiplier;
            finalAcceleration *= blinkingSpeedMultiplier;
        }

        agent.speed = finalSpeed;
        agent.acceleration = finalAcceleration;
    }


    protected override void OnDamaged(float damage, GameObject damageSource)
    {
        base.OnDamaged(damage, damageSource);
        
        if (!isBlinking.Value && renderer != null)
        {
            originalColor = renderer.material.color;
        }
    }

    #endregion

    public void TriggerExplosion() {
        if (!IsServer || _isExploding) return;

        isBlinking.Value = true;

        StartCoroutine(ExplodeAfterDelay(attackCooldown));
    }

    private IEnumerator ExplodeAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        Attack();
    }

    protected override void OnDie() {
        TriggerExplosion();

        StartCoroutine(DelayedBaseDie());
    }

    private IEnumerator DelayedBaseDie() {
        yield return new WaitForSeconds(3.0f);
        base.OnDie();
    }

    protected override IEnumerator DelayAttack() {
        isBlinking.Value = true;

        yield return new WaitForSeconds(attackCooldown);
        Attack();
    }

    protected override void Attack() {
        if (!IsServer || _isExploding) return;
        _isExploding = true;

        ParticleManager.instance.SpawnSelfThenAll("EnemyExplosion", transform.position, Quaternion.identity);
        AttackServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    protected override void AttackServerRpc(bool destroyAfterAttack = false)
    {
        if (!IsServer) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
            else if (hitCollider.CompareTag("Enemy"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage / 3, false, gameObject);
            }
        }

        if (destroyAfterAttack)
        {
            enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
        }
    }

    public override void OnNetworkDespawn()
    {
        isBlinking.OnValueChanged -= OnBlinkingStateChanged;

        StopAllCoroutines();

        base.OnNetworkDespawn();
    }
}
