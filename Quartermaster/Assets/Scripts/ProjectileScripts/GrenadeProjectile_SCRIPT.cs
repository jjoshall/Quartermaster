using UnityEngine;

public class GrenadeProjectile : IProjectile
{
    [SerializeField] private float _explosionRadius;
    [SerializeField] private float _explosionDamage;

    protected override void Expire()
    {
        Explode();
    }

    void Explode(){
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
