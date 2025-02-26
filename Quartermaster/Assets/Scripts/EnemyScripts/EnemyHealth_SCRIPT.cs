using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Health) , typeof(Renderer))]
public class EnemyHealth : NetworkBehaviour
{

    public Health m_health;
    public Renderer m_renderer;
    void Start(){
        if (!m_renderer) m_renderer = GetComponent<Renderer>();
        if (!m_health) m_health = GetComponent<Health>();
        m_health.OnDamaged += OnDamaged;
        m_health.OnDie += OnDie;
    }

    void Update(){
        m_renderer.material.color = new Color(m_health.GetRatio(), 0f, 0f);
    }
    void OnDamaged(float damage, GameObject source){
          Debug.Log("enemy took damage");
     }
     
    void OnDie(){
          EnemySpawner.instance.destroyEnemyServerRpc(GetComponent<NetworkObject>());
    }
}
