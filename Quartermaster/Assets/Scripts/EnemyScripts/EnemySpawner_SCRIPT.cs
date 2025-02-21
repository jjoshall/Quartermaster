using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class EnemySpawner : NetworkBehaviour {
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _maxEnemyInstanceCount = 20;
    [SerializeField] private float _spawnCooldown = 2f;

    // This networked list will be used to keep track of all players in the game so enemies can keep track of who they follow
    public NetworkList<NetworkObjectReference> playerList;

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

        // Initialize the player list
        playerList = new NetworkList<NetworkObjectReference>();

        // Initialize the pool
        NetworkObjectPool.Singleton.OnNetworkSpawn();
        StartCoroutine(SpawnOverTime());

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        playerList.Clear();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                playerList.Add(netObj);
            }
        }
    }

    private void ClientConnected(ulong u) {
        UpdatePlayerList();
    }

    private async void ClientDisconnected(ulong u) {
        await Task.Yield();
        UpdatePlayerList();
    }

    private Vector3 GetRandomPositionOnMap() {
        float x = Random.Range(-50, 50);
        float z = Random.Range(-50, 50);
        return new Vector3(x, 0, z);
     }

    private IEnumerator SpawnOverTime() {
        while (true) {
            if (NetworkObjectPool.Singleton.GetCurrentPrefabCount(_enemyPrefab) < _maxEnemyInstanceCount) {
                /// BEFORE POOLING, THIS JUST INSTANTIATES
                //Transform enemyTransform = Instantiate(_enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity, transform);
                //enemyTransform.GetComponent<EnemyNavScript>().enemySpawner = this;
                //enemyTransform.GetComponent<NetworkObject>().Spawn(true);
                //enemyList.Add(enemyTransform);

                /// POOLING
                NetworkObject enemy = NetworkObjectPool.Singleton.GetNetworkObject(_enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity);
                enemy.GetComponent<EnemyNavScript>().enemySpawner = this;
                if (!enemy.IsSpawned) {
                    enemy.Spawn(true);
                }
                
                yield return new WaitForSeconds(_spawnCooldown);
            }

                yield return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void destroyEnemyServerRpc(NetworkObjectReference enemy) {
        if (!IsServer) { return; }

        if (enemy.TryGet(out NetworkObject networkObject))
        {
            NetworkObjectPool.Singleton.ReturnNetworkObject(networkObject, _enemyPrefab);
            networkObject.Despawn(false);
        }
    }
}
