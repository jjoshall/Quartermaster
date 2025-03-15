using UnityEngine;
using System.Collections;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    private bool _canAttack = true;

    #region Variables for GameManager
    protected override float GetAttackCooldown() => GameManager.instance.MeleeEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.MeleeEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.MeleeEnemy_AttackDamage;
    protected override float GetAttackRadius() => GameManager.instance.MeleeEnemy_AttackRadius;
    protected override bool GetUseGlobalTarget() => GameManager.instance.MeleeEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.MeleeEnemy_Health;
    #endregion

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

    private IEnumerator ResetAttackCooldown() {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Melee enemy starting idle animation");
        animator.SetBool("IsAttacking", false);
        Debug.Log("Animator state after resetting: " + animator.GetCurrentAnimatorStateInfo(0).fullPathHash);
        yield return new WaitForSeconds(attackCooldown - 0.5f);
        _canAttack = true;
    }

    private IEnumerator DebugAttackState() {
        yield return null;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log("Current animator state: " + stateInfo.fullPathHash);
        if (stateInfo.IsName("Attack")) {
            Debug.Log("Attack animation is playing");
        } else {
            Debug.Log("Attack animation is NOT playing");
        }
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
