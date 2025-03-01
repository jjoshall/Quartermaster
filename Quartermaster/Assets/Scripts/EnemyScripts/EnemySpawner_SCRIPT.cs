using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class EnemySpawner : NetworkBehaviour {
    [Header("Spawner Settings")]
    [SerializeField] private List<Transform> _enemyPrefabs;
    [SerializeField] private int _maxEnemyInstanceCount = 20;
    public bool isSpawning = true;
    [SerializeField] private float _spawnCooldown = 2f;

    [SerializeField] private List<GameObject> _enemySpawnPoints;

    public List<Transform> enemyList = new List<Transform>();
    public List<GameObject> playerList;

    // static 
    public static EnemySpawner instance;

    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(this);
        }
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

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

    private Vector3 GetSpawnPoint() {
        if (_enemySpawnPoints.Count == 0) {
            Debug.LogError("No spawn points found.");
            serverDebugMsgServerRpc("No spawn points found.");
            return Vector3.zero;
        }

        // Choose a random spawn point index
        GameObject spawnPoint = _enemySpawnPoints[Random.Range(0, _enemySpawnPoints.Count)];

        float spawnX = spawnPoint.transform.position.x;
        float spawnY = 5f;
        float spawnZ = spawnPoint.transform.position.z;
        return new Vector3(spawnX, spawnY, spawnZ);
    }

    [ServerRpc]
    private void serverDebugMsgServerRpc(string msg) {
        if (!IsServer) { return; }
        Debug.Log(msg);
    }

    private IEnumerator SpawnOverTime() {
        while (true) {
            if (enemyList.Count < _maxEnemyInstanceCount && isSpawning) {
                Transform enemyPrefab = GetRandomEnemyPrefab();
                Transform enemyTransform = Instantiate(enemyPrefab, GetSpawnPoint(), Quaternion.identity);
                enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>().enemySpawner = this;
                enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>().enemyType = GetEnemyType(enemyPrefab);
                enemyTransform.GetComponent<NetworkObject>().Spawn(true);
                enemyList.Add(enemyTransform);
                enemyTransform.SetParent(this.gameObject.transform);
                yield return new WaitForSeconds(_spawnCooldown);
            }

            yield return null;
        }
    }

    private EnemyType GetEnemyType(Transform enemyPrefab) {
        if (enemyPrefab.GetComponent<MeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
        if (enemyPrefab.GetComponent<ExplosiveMeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
        if (enemyPrefab.GetComponent<RangedEnemyInherited_SCRIPT>() != null) return EnemyType.Ranged;
        return EnemyType.Melee;
    }

    private Transform GetRandomEnemyPrefab() {
        if (_enemyPrefabs.Count == 0) return null;
        return _enemyPrefabs[Random.Range(0, _enemyPrefabs.Count)];
    }

    [ServerRpc(RequireOwnership = false)]
    public void destroyEnemyServerRpc(NetworkObjectReference enemy) {
        if (!IsServer) { return; }
        if (enemy.TryGet(out NetworkObject networkObject)) {
            enemyList.Remove(networkObject.transform);
            networkObject.Despawn();
        }
    }
}
