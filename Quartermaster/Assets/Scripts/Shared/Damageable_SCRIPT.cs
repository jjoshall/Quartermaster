using Unity.Netcode;
using UnityEngine;

public class Damageable : MonoBehaviour {
    [Tooltip("Multiplier to apply to the received damage")]
    public float DamageMultiplier = 1f;

    [Range(0, 1)] [Tooltip("Multiplier to apply to self damage")]
    public float SensibilityToSelfdamage = 0.1f;

    public Health Health { get; private set; }

    void Awake() {
        // find the health component either at the same level, or higher in the hierarchy
        Health = GetComponent<Health>();
        if (!Health) {
            Health = GetComponentInParent<Health>();
        }
    }

    public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource) {
        if (Health) {
            var totalDamage = damage;

            // explosive damage does not crit
            if (!isExplosionDamage) {
                totalDamage *= DamageMultiplier;
            }

            // self inflicted damage is lowered
            if (Health.gameObject == damageSource) {
                totalDamage *= SensibilityToSelfdamage;
            }

            // apply the damages
            if (damageSource.TryGetComponent(out NetworkObject damageNetworkObject)) {
                if (damageNetworkObject.IsSpawned)
                {
                    NetworkObjectReference damageSourceRef = damageNetworkObject;
                    Health.TakeDamageServerRpc(totalDamage, damageSourceRef);
                }
            }
        }
    }
}