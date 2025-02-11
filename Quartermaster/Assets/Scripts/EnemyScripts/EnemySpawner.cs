using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
     [SerializeField] private GameObject enemyPrefab;
     [SerializeField] private int maxEnemyInstanceCount = 10;

     private void Awake()
     {
          // initial pool
     }

     public void SpawnEnemies()
     {
          if (!IsServer)
          {
               return;
          }

          for (int i = 0; i < maxEnemyInstanceCount; i++)
          {
               GameObject enemy = Instantiate(enemyPrefab, new Vector3(Random.Range(-10, 10), 3, Random.Range(-10, 10)), Quaternion.identity);
               enemy.GetComponent<NetworkObject>().Spawn(true);

               // pool instantiation
          }
     }

     private void Update()
     {
          if (Input.GetKeyDown(KeyCode.M))
          {
               SpawnEnemies();
          }
     }
}
