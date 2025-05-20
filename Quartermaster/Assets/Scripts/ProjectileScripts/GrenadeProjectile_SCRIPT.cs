using UnityEngine;

public class GrenadeProjectile : IProjectile
{
    float _explosionRadius = 0f;
    float _explosionDamage = 0f;


    protected override void Start()
    {
        _expireTimer = 10f; // generic value to avoid immediate destruction.
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

    public override void InitializeData(float expireTimer, params object[] args)
    {
        base.InitializeData(expireTimer, args);
        if (args.Length < 2)
        {
            Debug.LogError("GrenadeProjectile.InitializeData() - not enough args");
        }
        else
        {
            _explosionDamage = (float)args[0];
            _explosionRadius = (float)args[1];
        }
    }

    void Explode(){
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
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

}
