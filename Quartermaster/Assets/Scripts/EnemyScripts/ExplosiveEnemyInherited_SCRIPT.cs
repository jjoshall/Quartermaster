using Unity.Netcode;
using System.Collections;
using UnityEngine;

public class ExplosiveMeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    protected override float attackCooldown { get; } = 2f;
    protected override float attackRange { get; } = 3f;
    protected override int damage { get; } = 45;

    private bool _isExploding = false;

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
        if (!IsServer || _isExploding) return;

        _isExploding = true;
        Debug.Log("Exploding in 2 seconds...");

        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay() {
        yield return new WaitForSeconds(attackCooldown);

        AttackServerRpc();
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
                Debug.Log("EXPLODE");
            }
        }

        // Delete the enemy
        enemySpawner.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }
}
