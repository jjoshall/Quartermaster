using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;


// Abstract IProjectile class.
// Projectiles should be client truth network objects. 
// Hit detection done on client for cleaner feedback (PVE game)
// Client creates projectile, server validates, server sends to all clients.
public abstract class IProjectile : NetworkBehaviour
{
    [SerializeField] // store rigidbody?
    private Rigidbody rb;

    // Don't use start.
    public void Start(){
    }


    public override void OnNetworkSpawn()
    {
        
    }

    // Update is called once per frame
    public void Update(){
        Move();
    } // call some kind of movement function

    public void OnCollisionEnter(Collision collision){
        if (collision.gameObject.tag == "Wall"){
            WallCollision();
        }
        else if (collision.gameObject.tag == "Enemy"){
            EnemyCollision();
        }
    }

    public abstract void Move();
    public abstract void WallCollision();
    public abstract void EnemyCollision();
    public abstract void PlayerCollision();

    [ServerRpc]
    public void DestroyProjectileServerRpc(){
        // call server rpc to destroy self

    }
}
