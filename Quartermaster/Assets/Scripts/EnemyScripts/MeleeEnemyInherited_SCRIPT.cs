using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    private bool _canAttack = true;
    protected override float attackCooldown => 2f;
    protected override float attackRange => 10f;
    protected override int damage => 15;

    private Animator animator;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        animator = GetComponentInChildren<Animator>();
    }

    protected override void UpdateTarget() {
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

    protected override void Attack() {
        if (!_canAttack) return;
        _canAttack = false;

        if (IsServer) {
            Debug.Log("Melee enemy starting attack animation");
            animator.SetBool("IsAttacking", true);
            //StartCoroutine(DebugAttackState());
            AttackServerRpc(false);
        }

        StartCoroutine(ResetAttackCooldown());
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
}
