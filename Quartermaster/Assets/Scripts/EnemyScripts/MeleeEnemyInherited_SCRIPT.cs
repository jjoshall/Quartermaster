using UnityEngine;
using System.Collections;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    private bool _canAttack = true;
    public float attackCooldown = 1f;

    protected override void Attack() {
        if (!_canAttack) return;
        _canAttack = false;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine() {
        yield return new WaitForSeconds(attackCooldown);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }

        _canAttack = true;
    }
}
