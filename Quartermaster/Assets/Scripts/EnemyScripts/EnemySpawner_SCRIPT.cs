using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class EnemySpawner : NetworkBehaviour {
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _maxEnemyInstanceCount = 20;
    [SerializeField] private float _spawnCooldown = 2f;

    public List<Transform> enemyList = new List<Transform>();
    public List<GameObject> playerList;

    //private void Start() {
    //    NetworkManager.Singleton.OnServerStarted += SpawnEnemiesStart;
    //}

    // static
    public static EnemySpawner instance;

    void Awake() {
        if(instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
    }
    
    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        // Initialize the pool
        NetworkObjectPool.Singleton.OnNetworkSpawn();
        StartCoroutine(SpawnOverTime());

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    private void ClientConnected(ulong u) {
        playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
    }

    private async void ClientDisconnected(ulong u) {
        await Task.Yield();
        playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
    }

    private Vector3 GetRandomPositionOnMap() {
        float x = Random.Range(-50, 50);
        float z = Random.Range(-50, 50);
        return new Vector3(x, 0, z);
     }

    private IEnumerator SpawnOverTime() {
        while (true) {
            if (enemyList.Count < _maxEnemyInstanceCount) {
                /// BEFORE POOLING, THIS JUST INSTANTIATES
                //Transform enemyTransform = Instantiate(_enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity, transform);
                //enemyTransform.GetComponent<EnemyNavScript>().enemySpawner = this;
                //enemyTransform.GetComponent<NetworkObject>().Spawn(true);
                //enemyList.Add(enemyTransform);

                /// POOLING
                NetworkObject enemy = NetworkObjectPool.Singleton.GetNetworkObject(_enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity);
                enemy.GetComponent<EnemyNavScript>().enemySpawner = this;
                enemy.Spawn(true);
                enemyList.Add(enemy.transform);

                yield return new WaitForSeconds(_spawnCooldown);
            }

                yield return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void destroyEnemyServerRpc(NetworkObjectReference enemy) {
        if (IsServer)
        {
            NetworkObjectPool.Singleton.ReturnNetworkObject(enemy, _enemyPrefab);
        }
    }
}
