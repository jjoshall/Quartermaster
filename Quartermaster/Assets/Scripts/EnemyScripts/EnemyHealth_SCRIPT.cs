using UnityEngine;
using Unity.Netcode;

public class EnemyHealth : NetworkBehaviour
{

    public static int MAX_HEALTH = 10;
    public NetworkVariable<int> health = new NetworkVariable<int>(MAX_HEALTH);

    [ServerRpc]
    public void DamageEnemyServerRpc(int damageAmount) {
        health.Value -= damageAmount;
        if (health.Value <= 0) {
            NetworkObject netObject = this.gameObject.GetComponent<NetworkObject>();
            EnemySpawner.instance.destroyEnemyServerRpc(netObject);
        }
    }
}
