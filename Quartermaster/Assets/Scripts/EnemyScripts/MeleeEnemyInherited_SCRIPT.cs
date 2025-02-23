using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    private bool _canAttack = true;
    public float attackCooldown = 1f;

    protected override void Attack() {
        if (!_canAttack) return;
        _canAttack = false;

        //StartCoroutine(AttackRoutine());
        InvokeRepeating(nameof(AttackServerRpc), 0f, attackCooldown);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttackServerRpc() {
        if (!IsServer) return;

        // Perform the overlap sphere cast
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);

        // Loop through all the colliders that were detected within the sphere
        foreach (var hitCollider in hitColliders) {
            // Check if the collider has the "Player" tag
            if (hitCollider.CompareTag("Player"))
            {
                DamagePlayerClientRpc(hitColliders[0].gameObject.GetComponent<NetworkObject>());
            }
        }

        _canAttack = true;
    }

    [ClientRpc]
    private void DamagePlayerClientRpc(NetworkObjectReference playerRef) {
        if (playerRef.TryGet(out NetworkObject playerObject)) {
            playerObject.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
        }
    }
}
