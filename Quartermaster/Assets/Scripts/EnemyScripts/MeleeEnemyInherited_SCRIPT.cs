using UnityEngine;
using System.Collections;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT
{
    private bool canAttack = true;
    public float attackCooldown = 1f;

    protected override void Attack()
    {
        if (!canAttack) return;
        canAttack = false;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }

        canAttack = true;
    }
}
