using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    private bool _canAttack = true;
    protected override float attackCooldown { get; } = 2f;
    protected override float attackRange { get; } = 2f;
    protected override int damage { get; } = 20;

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
            AttackServerRpc();
        }

        StartCoroutine(ResetAttackCooldown());
    }

    private IEnumerator ResetAttackCooldown() {
        yield return new WaitForSeconds(attackCooldown);
        _canAttack = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttackServerRpc() {
        if (!IsServer) return;

        // Perform the overlap sphere cast
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);

        // Loop through all the colliders that were detected within the sphere
        foreach (var hitCollider in hitColliders) {
            if (hitCollider.CompareTag("Player")) {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }
    }
}
