using UnityEngine;

public class KnifeProjectile : IProjectile
{
    private bool _inFlight = false;
    private bool _hasCollided = false; // Cool effect: give knife rebound a hitbox.
    
    public override void Move()
    {
        
    }
    public override void WallCollision(){
        // rebound.
        // launch angle.
        // decrease velocity.
    }
    public override void EnemyCollision(){
        // rebound.
        // launch angle? simple (treat enemies as a wall) or complex? (rebound off surface normal)
        // decrease velocity.

    }
    public override void PlayerCollision()
    {
        // Give the collided player the knife.
        // possibly fun interaction with throwing ur teammates knives so they can throw them.
    }


}
