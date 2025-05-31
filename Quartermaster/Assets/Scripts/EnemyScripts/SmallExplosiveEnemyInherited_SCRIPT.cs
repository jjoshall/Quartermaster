using Unity.Netcode;
using System.Collections;
using UnityEngine;
using Unity.Services.Analytics;

public class SmallExplosiveMeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    #region Variables for GameManager
    protected override float GetAttackCooldown() => GameManager.instance.SmallExplosiveEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.SmallExplosiveEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.SmallExplosiveEnemy_AttackDamage;
    protected override float GetAttackRadius() => GameManager.instance.SmallExplosiveEnemy_AttackRadius;
    protected override bool GetUseGlobalTarget() => GameManager.instance.SmallExplosiveEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.SmallExplosiveEnemy_Health;
    #endregion

    private bool _isExploding = false; // exploding means dying explosion. don't ask why. removing this causes game to freeze on enemy death.
    private bool _isAnimatingExplosion = false; // attacking means enemy-initiated, delayed explosion.
                                       // don't try to fix this until the end of the quarter.

    #region Explosion Beeping Changes
    // this bool means if explosive enemy is starting his explosion sequence
    private NetworkVariable<bool> isBlinking = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private float _explosionDelay;
    //[SerializeField] private float _blinkSpeed = 5f;
    [Range(1f, 3f)]
    [SerializeField] private float _blinkingSpeedMultiplier = 1.3f;

    [Header("Armature Settings")]
    [SerializeField] private Transform _wheels;
    
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        enemyType = EnemyType.Explosive;

        _isExploding = false;
        if (IsServer) {
            isBlinking.Value = false;
        }
        

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
            else if (!IsServer && agent != null) {
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

        if (isBlinking.Value){
            finalSpeed *= _blinkingSpeedMultiplier;
            finalAcceleration *= _blinkingSpeedMultiplier;
        }

        agent.speed = finalSpeed;
        agent.acceleration = finalAcceleration;
    }

    #endregion

    protected override void OnDie() {
        if (!IsServer) return;

        if (_isExploding) {
            Debug.Log("Explosive enemy already exploding, skipping OnDie explosion.");
            return;
        }

        _isExploding = true;
        isBlinking.Value = false; // stop blinking

        StopAllCoroutines();

        try {
            PlaySoundForEmitter("explode_die", transform.position);
        }
        catch (System.Exception e) {
            Debug.LogError("Error play sound for emitter: " + e.Message);            
        }

        // Immediate explosion � skip build-up
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);
        ParticleManager.instance.SpawnSelfThenAll("EnemyExplosion", transform.position, Quaternion.identity);

        foreach (var hitCollider in hitColliders) {
            if (hitCollider.CompareTag("Player")) {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
            else if (hitCollider.CompareTag("Enemy")) {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage / 5, false, gameObject);
            }
        }

        if (AnalyticsManager_SCRIPT.Instance != null && AnalyticsManager_SCRIPT.Instance.IsAnalyticsReady()) {
            AnalyticsService.Instance.RecordEvent("ExplosiveEnemyKilled");
        }

        enemySpawner.RemoveEnemyFromList(gameObject);
        enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }

    // Called by base class attack cooldown.
    protected override void Attack() {
        if (!IsServer || _isExploding || _isAnimatingExplosion) return;

        _isAnimatingExplosion = true;
        isBlinking.Value = true;

        LeanTween.value(gameObject, 0f, 1f, _explosionDelay)
            .setOnComplete(() => {
                isBlinking.Value = false;
                Explosion();
            });
    }    

    private void Explosion(bool destroyAfterAttack = true)
    {
        if (!IsServer) return;

        try {
            PlaySoundForEmitter("explode_die", transform.position);
        }
        catch (System.Exception e) {
            Debug.LogError("Error play sound for emitter: " + e.Message);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);
        ParticleManager.instance.SpawnSelfThenAll("EnemyExplosion", transform.position, Quaternion.identity);

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
            if (AnalyticsManager_SCRIPT.Instance != null && AnalyticsManager_SCRIPT.Instance.IsAnalyticsReady()) {
                AnalyticsService.Instance.RecordEvent("ExplosiveEnemyKilled");
            }

            enemySpawner.RemoveEnemyFromList(gameObject);
            enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
        }
    }


    public override void OnNetworkDespawn() {
        isBlinking.OnValueChanged -= OnBlinkingStateChanged;

        StopAllCoroutines();

        base.OnNetworkDespawn();
    }
}
