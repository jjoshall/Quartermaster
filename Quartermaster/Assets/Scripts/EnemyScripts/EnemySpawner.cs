using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
     [SerializeField] private GameObject enemyPrefab;
     [SerializeField] private int maxEnemyInstanceCount = 20;

     private void Start()
     {
          NetworkManager.Singleton.OnServerStarted += SpawnEnemiesStart;
     }

     public void SpawnEnemiesStart()
     {
          NetworkManager.Singleton.OnServerStarted -= SpawnEnemiesStart;
          NetworkObjectPool.Singleton.InitializePool();

          for (int i = 0; i < maxEnemyInstanceCount; i++)
          {
               SpawnEnemies();
          }

          StartCoroutine(SpawnOverTime());
     }

     public void SpawnEnemies()
     {
          NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity);

          obj.GetComponent<EnemyNavScript>().enemyPrefab = enemyPrefab;
          obj.Spawn(true);
     }

     private Vector3 GetRandomPositionOnMap()
     {
          float x = Random.Range(-50, 50);
          float z = Random.Range(-50, 50);
          return new Vector3(x, 0, z);
     }

     private IEnumerator SpawnOverTime()
     {
          while (NetworkManager.Singleton.ConnectedClients.Count > 0)
          {
               yield return new WaitForSeconds(3f);
               SpawnEnemies();
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
