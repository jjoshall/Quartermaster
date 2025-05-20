using UnityEngine;

public class DummyGrenade : IProjectile
{
    protected override void Start()
    {
        if (_expireTimer <= 0f){
            Debug.LogError("_expireTimer is not set.");
            _expireTimer = 10f; // generic value to avoid immediate destruction.
        }
    }
    public override void InitializeData(float expireTimer, params object[] args)
    {
        base.InitializeData(expireTimer, args);
    }
    protected override void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Explode();
        }
        else
        {
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
