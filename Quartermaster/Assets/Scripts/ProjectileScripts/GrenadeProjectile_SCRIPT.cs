using UnityEngine;

public class GrenadeProjectile : IProjectile
{
    protected override void Start()
    {
        _expireTimer = GameManager.instance.Grenade_ExpireTimer;
    }
    protected override void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy")){
            Explode();
        } else {
            _projectileCollided = true;
        }
    }

    protected override void Update()
    {
        if (_projectileCollided){
            _expireTimer -= Time.deltaTime;
            if (_expireTimer <= 0){
                Explode();
            }
        }   
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
                    // d.InflictDamage(_explosionDamage, true, sourcePlayer);
                    DoDamage(d, _explosionDamage, true, sourcePlayer);
                    ParticleManager.instance.SpawnSelfThenAll("Sample", nearbyObject.transform.position, Quaternion.identity);
                } else {
                    Debug.LogError("GrenadeProjectile.Explode() - enemy tag without damageable component: " + nearbyObject.name);
                }
            }
        }

        // ParticleManager.instance.
        ParticleManager.instance.SpawnSelfThenAll("GrenadeExplosion", transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }

    
    private void DoDamage (Damageable d, float dmg, bool isExplosiveDmgType, GameObject user){
        float damage = dmg;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonusPerSpec = GameManager.instance.DmgSpec_MultiplierPer;
            int dmgSpecLvl = s.GetDmgSpecLvl();
            damage = damage * (1 + bonusPerSpec * dmgSpecLvl);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

}
