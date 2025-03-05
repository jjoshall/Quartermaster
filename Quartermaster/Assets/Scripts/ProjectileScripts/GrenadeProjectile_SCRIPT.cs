using UnityEngine;

public class GrenadeProjectile : IProjectile
{

    protected override void Expire()
    {
        Explode();
    }

    void Explode(){
        float _explosionRadius = GameManager.instance.Grenade_AoeRadius;
        float _explosionDamage = GameManager.instance.Grenade_Damage;

        LayerMask enemyMask = LayerMask.GetMask("Enemy");
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius, enemyMask);
        foreach (Collider nearbyObject in colliders){
            if (nearbyObject.CompareTag("Enemy")){
                Damageable d = nearbyObject.GetComponent<Damageable>();
                if (d != null){
                    d.InflictDamage(_explosionDamage, true, sourcePlayer);
                    ParticleManager.instance.SpawnSelfThenAll("Sample", nearbyObject.transform.position, Quaternion.identity);
                } else {
                    Debug.LogError("GrenadeProjectile.Explode() - enemy tag without damageable component: " + nearbyObject.name);
                }
            }
        }

        // ParticleManager.instance.
        Destroy(this.gameObject);
    }

}
