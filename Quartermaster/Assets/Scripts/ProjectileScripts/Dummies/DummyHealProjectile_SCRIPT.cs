using UnityEngine;

public class DummyHealProjectile : IProjectile
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

    protected override void Update()
    {
        if (_projectileCollided)
        {
            _expireTimer -= Time.deltaTime;
            if (_expireTimer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        Debug.Log("healprojectile collided with: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("PlayerHealCollider")){
            GameObject player = collision.gameObject.transform.parent.gameObject;
            if (player == sourcePlayer && _projectileCollided == false){
                return;
            }
            Destroy(gameObject);
        } else {
            _projectileCollided = true;
            // spherecast for playerhealcollider
        }
    }

}
