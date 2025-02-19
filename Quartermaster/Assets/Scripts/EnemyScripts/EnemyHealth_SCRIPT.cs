using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Health) , typeof(Renderer))]
public class EnemyHealth : NetworkBehaviour
{

    public Health health;
    public Renderer renderer;
    void Start(){
        if (!renderer) renderer = GetComponent<Renderer>();
        if (!health) health = GetComponent<Health>();
        health.OnDamaged += OnDamaged;
        health.OnDie += OnDie;
    }

    void Update(){
        renderer.material.color = new Color(health.GetRatio(), 0f, 0f);
    }
    void OnDamaged(float damage, GameObject source){
          Debug.Log("enemy took damage");
     }
     
    void OnDie(){
          EnemySpawner.instance.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }
}
