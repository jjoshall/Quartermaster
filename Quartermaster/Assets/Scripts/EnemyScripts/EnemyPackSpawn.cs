using UnityEngine;
using Unity.Netcode;

public class EnemyPackSpawn : NetworkBehaviour {
    [SerializeField] private KeyCode spawnKey = KeyCode.H;
    [SerializeField] private int packSize = 10;
    [SerializeField] private float enemySpread = 2f;

    private EnemySpawner enemySpawner;

    private void Start() {
        enemySpawner = EnemySpawner.instance;
    }

    private void Update() {
        if (!IsLocalPlayer) return;

        if (Input.GetKeyDown(spawnKey)) {
            if (enemySpawner != null) { 
                enemySpawner.SpawnEnemyPackAtRandomPointServerRpc(packSize, enemySpread);
            }
        }
    }
}
