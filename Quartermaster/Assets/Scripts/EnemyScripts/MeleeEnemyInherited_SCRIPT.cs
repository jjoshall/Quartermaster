using UnityEngine;
using System.Collections;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    #region Variables for GameManager
    protected override float GetAttackCooldown() => GameManager.instance.MeleeEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.MeleeEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.MeleeEnemy_AttackDamage;
    protected override float GetAttackRadius() => GameManager.instance.MeleeEnemy_AttackRadius;
    protected override bool GetUseGlobalTarget() => GameManager.instance.MeleeEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.MeleeEnemy_Health;
    #endregion

    private bool _canAttack = true;     // Prevents enemies from attacking too quickly

    private SoundEmitter[] soundEmitters;
    private Animator animator;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        animator = GetComponentInChildren<Animator>();
        soundEmitters = GetComponents<SoundEmitter>();
    }

    protected override void Attack() {
        if (!_canAttack) return;
        _canAttack = false;

        if (IsServer) {
            animator.SetBool("IsAttacking", true);
            StartCoroutine(TriggerPunchSFX());
            AttackServerRpc(false);
        }

        // SWITCH TO TIMER LATER
        StartCoroutine(ResetAttackCooldown());
    }

    protected override void OnDamaged(float damage, GameObject damageSource)
    {
        base.OnDamaged(damage, damageSource);
        PlaySoundForEmitter("melee_damaged", transform.position);
    }

    private IEnumerator TriggerPunchSFX() {
        PlaySoundForEmitter("melee_punch", transform.position);
        yield return new WaitForSeconds(0.2f);
        PlaySoundForEmitter("melee_punch", transform.position);
    }

    // Will change to timer later, this just makes enemies not attack over and over
    private IEnumerator ResetAttackCooldown() {
        yield return new WaitForSeconds(0.5f);
        animator.SetBool("IsAttacking", false);
        yield return new WaitForSeconds(attackCooldown - 0.5f);
        _canAttack = true;
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
