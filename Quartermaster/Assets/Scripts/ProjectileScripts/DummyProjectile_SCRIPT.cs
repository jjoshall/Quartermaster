using UnityEngine;

public class DummyProjectile : IProjectile
{
    protected override void Expire(){
        Destroy(gameObject);
    }
}
