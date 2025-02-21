using UnityEngine;

public class GrenadeProjectile : IProjectile
{
    public override void Move()
    {

    }

    public override void WallCollision(){
        DestroyProjectileServerRpc();
    }
    public override void EnemyCollision(){
        DestroyProjectileServerRpc();
    }

    public override void PlayerCollision()
    {
        // friendly fire? or no?
    }

    public void Explode(){
        // spawn explosion
        // deal damage to enemies in radius
        // destroy self
    }
}
