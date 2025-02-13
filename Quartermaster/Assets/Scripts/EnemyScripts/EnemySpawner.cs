using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class EnemySpawner : NetworkBehaviour
{
     [SerializeField] private Transform enemyPrefab;
     [SerializeField] private int maxEnemyInstanceCount = 20;
     [SerializeField] private float spawnCooldown = 2f;

     public List<Transform> enemyList = new List<Transform>();
     public List<GameObject> playerList;

     //private void Start()
     //{
     //     //NetworkManager.Singleton.OnServerStarted += SpawnEnemiesStart; 
     //}

     public override void OnNetworkSpawn()
     {
          if (!IsServer)
          {
               enabled = false;
               return;
          }

          StartCoroutine(SpawnOverTime());

          NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
          NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
     }

     private void ClientConnected(ulong u)
     {
          playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
     }

     private async void ClientDisconnected(ulong u)
     {
          await Task.Yield();
          playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
     }

     private Vector3 GetRandomPositionOnMap()
     {
          float x = Random.Range(-50, 50);
          float z = Random.Range(-50, 50);
          return new Vector3(x, 0, z);
     }

     private IEnumerator SpawnOverTime()
     {
          while (true)
          {
               if (enemyList.Count < maxEnemyInstanceCount)
               {
                    Transform enemyTransform = Instantiate(enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity, transform);
                    enemyTransform.GetComponent<EnemyNavScript>().enemySpawner = this;
                    enemyTransform.GetComponent<NetworkObject>().Spawn(true);
                    enemyList.Add(enemyTransform);

                    yield return new WaitForSeconds(spawnCooldown);
               }

               yield return null;
          }
     }

     //public void SpawnEnemiesStart()
     //{
     //     NetworkManager.Singleton.OnServerStarted -= SpawnEnemiesStart;
     //     //NetworkObjectPool.Singleton.InitializePool();

     //     for (int i = 0; i < maxEnemyInstanceCount; i++)
     //     {
     //          SpawnEnemies();
     //     }

     //     StartCoroutine(SpawnOverTime());
     //}

     //public void SpawnEnemies()
     //{
     //     //NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity);

     //     //obj.GetComponent<EnemyNavScript>().enemyPrefab = enemyPrefab;
     //     //obj.Spawn(true);


     //}
}
