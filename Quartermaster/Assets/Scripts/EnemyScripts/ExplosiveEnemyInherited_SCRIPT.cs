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

    [SerializeField] private float _explosionDelay;
    [SerializeField] private float _blinkSpeed = 5f;
    [Range(1f, 3f)]
    [SerializeField] private float _blinkingSpeedMultiplier = 1.3f;  // Uses a range for 

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
    public override void UpdateSpeedServerRpc(){
        float finalSpeed = _baseSpeed;  
        float finalAcceleration = _baseAcceleration;

        if (n_isSlowed.Value > 0){
            finalSpeed *= 1 - n_slowMultiplier.Value;
            finalAcceleration *= 1 - n_slowMultiplier.Value;
        }

        finalSpeed *= AISpeedMultiplier;
        finalAcceleration *= AISpeedMultiplier;

        if (isBlinking.Value){
            finalSpeed *= _blinkingSpeedMultiplier;
            finalAcceleration *= _blinkingSpeedMultiplier;
        }

        agent.speed = finalSpeed;
        agent.acceleration = finalAcceleration;
    }

    #endregion

    protected override void OnDie() {
        try {
            PlaySoundForEmitter("explode_die", transform.position);
        }
        catch (System.Exception e) {
            Debug.LogError("Error play sound for emitter: " + e.Message);
        }
        Attack(); // exploding enemy instantly explodes on death.

        // Change to timer?, this is so explosive enemy doesn't get destroyed from scene before sound finishes
        StartCoroutine(DelayedBaseDie());
    }

    private IEnumerator DelayedBaseDie() {
        yield return new WaitForSeconds(3.0f);
        base.OnDie();
    }
    // Called by base class attack cooldown.
    protected override void Attack() {
        if (!IsServer || _isExploding) return;
        Debug.Log("Explosive Attack");
        _isExploding = true;
        isBlinking.Value = true;

        StartCoroutine (DelayedExplosion(_explosionDelay));
    }    

    // delay
    protected virtual IEnumerator DelayedExplosion(float delay) {
        yield return new WaitForSeconds(delay);
        AttackServerRpc(true);

    }

    // the actual attack
    [ServerRpc(RequireOwnership = false)]
    private void AttackServerRpc(bool destroyAfterAttack = false)
    {
        if (!IsServer) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);
        ParticleManager.instance.SpawnSelfThenAll("EnemyExplosion", transform.position, Quaternion.identity);

        float dmgAiScaled = damage * AIDmgMultiplier;
        Debug.Log ("dmgAiScaled is: " + dmgAiScaled);

        // Explosion hurts players and enemies, but enemies only take 1/3 of the damage
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(dmgAiScaled, false, gameObject);
            }
            else if (hitCollider.CompareTag("Enemy"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(dmgAiScaled / 3, false, gameObject);
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
