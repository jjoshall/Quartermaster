using UnityEngine;

public class DummyGrenade : IProjectile
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
        // ParticleManager.instance.
        Destroy(this.gameObject);
    }
}
