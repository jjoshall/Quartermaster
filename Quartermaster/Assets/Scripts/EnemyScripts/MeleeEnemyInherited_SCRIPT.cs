using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    #region Variables for GameManager
    protected override float GetAttackCooldown() => GameManager.instance.MeleeEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.MeleeEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.MeleeEnemy_AttackDamage;
    protected override float GetAttackRadius() => GameManager.instance.MeleeEnemy_AttackRadius;
    protected override bool GetUseGlobalTarget() => GameManager.instance.MeleeEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.MeleeEnemy_Health;
    #endregion

    protected override void Attack() {
        if (IsServer) {
            animator.SetBool("IsAttacking", true);
            StartCoroutine(TriggerPunchSFX());
            AttackServerRpc();
        }

        // SWITCH TO TIMER LATER
        StartCoroutine(ResetAttackCooldown());
    }

    [ServerRpc(RequireOwnership = false)]   
    private void AttackServerRpc() {
        if (!IsServer) return;

        // OverlapSphere will find all colliders in the attackRadius around the enemy
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (var hitCollider in hitColliders) {
            if (hitCollider.CompareTag("Player")) {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }
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
    }
}
