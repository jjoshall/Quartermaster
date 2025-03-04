using UnityEngine;

public class KnifeProjectile : IProjectile
{    
    
    protected virtual void OnCollisionEnter(Collision collision){
        // if the grenade hits a player, it should explode.
        if (collision.gameObject.CompareTag("Enemy")){
            
            // WIP. DO DAMAGE HERE. 

            
            Expire();
        }
        else {
            _projectileCollided = true;
        }
    }


    protected override void Expire()
    {
        // Destroy self. Spawn knife worlditem.
    }
}
