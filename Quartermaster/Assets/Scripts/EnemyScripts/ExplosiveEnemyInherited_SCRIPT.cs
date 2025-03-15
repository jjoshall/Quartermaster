using Unity.Netcode;
using System.Collections;
using UnityEngine;

public class ExplosiveMeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    #region Variables for GameManager
    protected override float GetAttackCooldown() => GameManager.instance.ExplosiveEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.ExplosiveEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.ExplosiveEnemy_AttackDamage;
    protected override float GetAttackRadius() => GameManager.instance.ExplosiveEnemy_AttackRadius;
    protected override bool GetUseGlobalTarget() => GameManager.instance.ExplosiveEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.ExplosiveEnemy_Health;
    #endregion

    private bool _isExploding = false;

    #region Explosion Beeping Changes
    // this bool means if explosive enemy is starting his explosion sequence
    private NetworkVariable<bool> isBlinking = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private float blinkSpeed = 5f;
    [Range(1f, 3f)]
    [SerializeField] private float blinkingSpeedMultiplier = 1.3f;  // Uses a range for 

    private Animator animator;
    private SoundEmitter[] soundEmitters;

    [Header("Armature Settings")]
    [SerializeField] private Transform _wheels;
    
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        animator = GetComponentInChildren<Animator>();
        soundEmitters = GetComponents<SoundEmitter>();

        isBlinking.OnValueChanged += OnBlinkingStateChanged;
    }

    // This function gets called whenever the value of isBlinking changes/explosion sequence starts
    private void OnBlinkingStateChanged(bool oldValue, bool newValue) {
        if (newValue) {
            if (IsServer && agent != null) {
                // agent.speed = blinkingSpeed;
                UpdateSpeedServerRpc();
                PlaySoundForEmitter("explode_build", transform.position);
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

    #endregion

    // This method is for when the explosive enemy is killed
    public void TriggerExplosion() {
        if (!IsServer || _isExploding) return;

        isBlinking.Value = true;

        // Change to timer?
        StartCoroutine(ExplodeAfterDelay(attackCooldown));
    }

    private IEnumerator ExplodeAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        Attack();
    }

    protected override void OnDie() {
        try {
            PlaySoundForEmitter("explode_die", transform.position);
        }
        catch (System.Exception e) {
            Debug.LogError("Error play sound for emitter: " + e.Message);
        }
        TriggerExplosion();

        // Change to timer?, this is so explosive enemy doesn't get destroyed from scene before sound finishes
        StartCoroutine(DelayedBaseDie());
    }

    private IEnumerator DelayedBaseDie() {
        yield return new WaitForSeconds(3.0f);
        base.OnDie();
    }

    //protected override void OnAttackStart() {
    //    isBlinking.Value = true;
    //    base.OnAttackStart();
    //}

    // This method is for when the explosive enemy is attacking, change to timer?
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

        // Explosion hurts players and enemies, but enemies only take 1/3 of the damage
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

    public override void OnNetworkDespawn() {
        isBlinking.OnValueChanged -= OnBlinkingStateChanged;

        StopAllCoroutines();

        base.OnNetworkDespawn();
    }

    public void PlaySoundForEmitter(string emitterId, Vector3 position) {
        foreach (SoundEmitter emitter in soundEmitters) {
            if (emitter.emitterID == emitterId) {
                emitter.PlayNetworkedSound(position);
                return;
            }
        }
    }
    
}
