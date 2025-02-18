using UnityEngine;
using Unity.Netcode;

public class EnemyHealth : NetworkBehaviour
{

    public Health health;
    void Start(){
        if (!health) health = GetComponent<Health>();
        health.OnDamaged += OnDamaged;
        health.OnDie += OnDie;
    }
    void OnDamaged(float damage, GameObject source){
          Debug.Log("enemy took damage");
     }
     
    void OnDie(){
          EnemySpawner.instance.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }
}
