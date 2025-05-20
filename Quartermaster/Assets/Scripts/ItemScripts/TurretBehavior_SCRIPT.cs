using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TurretBehavior_SCRIPT : NetworkBehaviour
{
    private List<GameObject> _inRange;

    // Subscribe to OnEnemyDespawn event to handle removing enemies from _inRange
    public EnemySpawner enemySpawner;


    public override void OnNetworkSpawn(){
        enemySpawner = EnemySpawner.instance;
        enemySpawner.OnEnemyDespawn += OnEnemyDespawn;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")){
            _inRange.Add(other.gameObject);
        }
    }

    void OnEnemyDespawn(NetworkObject enemyObject){

    }

    // set up event
}
