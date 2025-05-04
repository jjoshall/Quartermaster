using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;
using Unity.BossRoom.Infrastructure;
using System.Linq;    // Add this for network object pooling
using TMPro;        // Add this for TextMeshPro

public class EnemySpawner : NetworkBehaviour {
    [Header("Spawner Timer")]
    private float _lastSpawnTime = 0f;
    public float spawnCooldown = 3f;

    [Header("Spawner Settings")]
    [HideInInspector] public NetworkVariable<bool> isSpawning = new NetworkVariable<bool>(false);
    [SerializeField] private int _maxEnemyInstanceCount = 50;
    [SerializeField] private List<WeightedPrefab> weightedPrefabs = new List<WeightedPrefab>();     // weights for enemy spawning (higher = more likely to spawn)

    [SerializeField] private float globalAggroUpdateInterval = 10.0f;
    private float globalAggroUpdateTimer = 0.0f;
    public Vector3 globalAggroTarget = new Vector3(0, 0, 0);

    [SerializeField] private List<GameObject> _enemySpawnPoints;
    //[SerializeField] private List<GameObject> _enemyPackSpawnPoints;

    public List<GameObject> enemyList = new List<GameObject>();
    private List<GameObject> playerList; // players in game.
    public List<GameObject> activePlayerList; // players in active playable area.
    public List<GameObject> inactiveAreas; // pathable areas that are not playable. set in inspector.
                                           // players in these areas will be occluded from activePlayerList.
                                           // for pathing / enemyspawner / aidirector purposes.

    // static 
    public static EnemySpawner instance;

    // Reference NetworkObjectPool
    private NetworkObjectPool _objectPool;

    // Weights struct for enemy prefabs
    [System.Serializable]
    public struct WeightedPrefab {
        public GameObject Prefab;
        public float Weight;
    }

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

        // Get ref to NetworkObjectPool
        _objectPool = NetworkObjectPool.Singleton;
        if (_objectPool == null) {
            Debug.LogError("NetworkObjectPool not found.");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += RefreshPlayerLists;
        NetworkManager.Singleton.OnClientDisconnectCallback += RefreshPlayerLists;
    }

    private void RefreshPlayerLists(ulong u) {
        playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));

        activePlayerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
        foreach (GameObject area in inactiveAreas) {
            foreach (GameObject player in activePlayerList) {
                Collider areaCollider = area.GetComponent<Collider>();
                if (areaCollider != null && areaCollider.bounds.Contains(player.transform.position)) {
                    activePlayerList.Remove(player);
                }
            }
        }
    }

    private void Update() {
        if (IsServer) {
            UpdateGlobalAggroTargetTimer();
        }

        SpawnOverTime();
    }

    #region Spawning Enemies
    private void SpawnOverTime() {
        if (!IsServer) return;
        if (activePlayerList.Count == 0) return; // No players in active area.
        if (enemyList.Count >= _maxEnemyInstanceCount) return; // Max enemies reached.

        // less than max enemies, and more than 0 players in playable area.
        // Managed by InactiveAreaCollider s adding/removing from activePlayerList
        if (Time.time >= _lastSpawnTime + spawnCooldown) {
            //Debug.Log("Spawning enemy");
            SpawnOneEnemy();
            _lastSpawnTime = Time.time;
        }
    }

    private void SpawnOneEnemy() {
        GameObject enemyPrefab = GetWeightedRandomInactivePrefab(); // Get a random enemy prefab from the pool
        if (enemyPrefab == null) return; // No available enemies in pool.

        Vector3 spawnPoint = GetSpawnPoint();

        if (enemyPrefab != null) {
            // Just get any object from the object pool
            NetworkObject networkObject = _objectPool.GetNetworkObject(
                enemyPrefab,
                spawnPoint,
                Quaternion.identity
            );

            // Get the enemy instance
            GameObject enemyInstance = networkObject.gameObject;

            if (networkObject.IsSpawned) {
                Debug.LogWarning($"Tried to spawn {networkObject.name} but it is already spawned.");
                return;
            }

            // Spawn on network
            networkObject.Spawn(true);

            // Make sure enemy uses enemy spawner instance
            BaseEnemyClass_SCRIPT enemyScript = enemyInstance.GetComponent<BaseEnemyClass_SCRIPT>();
            enemyScript.enemySpawner = this;
            enemyScript.originalPrefab = enemyPrefab;

            // Add enemy instance to the enemy list
            enemyList.Add(enemyInstance);

            // Set the speed of the enemy
            enemyInstance.GetComponent<BaseEnemyClass_SCRIPT>().UpdateSpeedServerRpc();
        }
    }

    #endregion

    #region Pooling example (floating damage numbers)
    /// <summary>
    /// How to pool!!
    /// </summary>
    public async void SpawnDamageNumberFromPool(GameObject floatingTextPrefab, Vector3 spawnPosition, float damage) {
        if (!IsServer) return;

        // Just get any object from the object pool
        NetworkObject networkObject = _objectPool.GetNetworkObject(
            floatingTextPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (networkObject.IsSpawned) {
            Debug.LogWarning($"Tried to spawn {networkObject.name} but it is already spawned.");
            return;
        }

        // Spawn on network
        networkObject.Spawn(true);

        // Get the floating text instance and set its text
        FloatingText_SCRIPT floatingScript = networkObject.GetComponent<FloatingText_SCRIPT>();
        floatingScript.SetTextClientRpc(damage);

        // Have the object spawn for 2.5 seconds
        await Task.Delay(2500);

        // Then despawn from network first
        networkObject.Despawn();

        // Return to pool
        _objectPool.ReturnNetworkObject(networkObject, floatingTextPrefab);
    }

    #endregion

    #region Pack Spawn
    /// <summary>
    /// If we add pack spawning, place spawn points in the scene and uncomment this method and use it
    /// Make sure to change SpawnEnemyPackAtRandomPoint and its server RPC to use this method
    /// </summary>
    //private Vector3 GetRandomPackSpawnPoint() {
    //    if (_enemyPackSpawnPoints == null || _enemyPackSpawnPoints.Count == 0) {
    //        Debug.LogError("No enemy pack spawn points found.");
    //        return Vector3.zero;
    //    }

    //    // Choose a random spawn point from the list
    //    GameObject spawnPoint = _enemyPackSpawnPoints[Random.Range(0, _enemyPackSpawnPoints.Count)];
    //    return spawnPoint.transform.position;
    //}

    //public void SpawnEnemyPackAtRandomPoint(int count, Vector3 position, float spread = 2f) {
    //    if (!IsServer) return;

    //    //Vector3 spawnPosition = GetRandomPackSpawnPoint();
    //    SpawnEnemyPack(count, position, spread);
    //}

    //// ServerRpc for clients to request a pack spawn at a random point
    //[ServerRpc(RequireOwnership = false)]
    //public void SpawnEnemyPackAtRandomPointServerRpc(int count, Vector3 position, float spread = 2f) {
    //    if (!IsServer) return;
    //    SpawnEnemyPackAtRandomPoint(count, position, spread);
    //}
    //public void SpawnEnemyPack(int count, Vector3 position, float spread = 2f) {
    //    if (!IsServer) return;

    //    for (int i = 0; i < count; i++) {
    //        // Add slight randomization to position so enemies don't stack
    //        Vector3 randomOffset = new Vector3(
    //            Random.Range(-spread, spread),
    //            0f,
    //            Random.Range(-spread, spread)
    //        );

    //        Vector3 spawnPosition = position + randomOffset;
    //        spawnPosition.y = 5f; // Same height as your other spawns

    //        Transform enemyPrefab = GetWeightedRandomEnemyPrefab();
    //        if (enemyPrefab != null)
    //        {
    //            Transform enemyTransform = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    //            enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>().enemySpawner = this;
    //            enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>().enemyType = GetEnemyType(enemyPrefab);
    //            enemyTransform.GetComponent<NetworkObject>().Spawn(true);
    //            enemyList.Add(enemyTransform);
    //            enemyTransform.SetParent(this.gameObject.transform);
    //        }
    //    }
    //}

    //[ServerRpc(RequireOwnership = false)]
    //public void SpawnEnemyPackServerRpc(int count, Vector3 position, float spread = 2f) {
    //    if (!IsServer) return;
    //    SpawnEnemyPack(count, position, spread);
    //}

    #endregion

    //private EnemyType GetEnemyType(GameObject enemyPrefab) {
    //    if (enemyPrefab.GetComponent<MeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
    //    if (enemyPrefab.GetComponent<ExplosiveMeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
    //    if (enemyPrefab.GetComponent<RangedEnemyInherited_SCRIPT>() != null) return EnemyType.Ranged;
    //    return EnemyType.Melee;
    //}


    #region GlobalAggro
    public Vector3 GetGlobalAggroTarget() {
        return globalAggroTarget;
    }

    private void UpdateGlobalAggroTargetTimer() {
        globalAggroUpdateTimer += Time.deltaTime;
        if (globalAggroUpdateTimer >= globalAggroUpdateInterval) {
            globalAggroUpdateTimer = 0.0f;

            for (int i = 0; i < playerList.Count; i++) {
                if (playerList[i] == null) {
                    playerList.RemoveAt(i);
                    i--;
                }
            }

            if (playerList.Count > 0) {
                int randomPlayer = Random.Range(0, playerList.Count);
                globalAggroTarget = playerList[randomPlayer].transform.position;
            }
        }
    }
    #endregion 

    #region DestroyEnemy
    [ServerRpc(RequireOwnership = false)]
    public void destroyEnemyServerRpc(NetworkObjectReference enemy) {
        if (!IsServer) { return; }

        if (enemy.TryGet(out NetworkObject networkObject)) {
            // Get original prefab reference
            BaseEnemyClass_SCRIPT enemyScript = networkObject.GetComponent<BaseEnemyClass_SCRIPT>();
            GameObject originalPrefab = enemyScript.originalPrefab;

            // Despawn from network first
            networkObject.Despawn();

            // Return to pool
            _objectPool.ReturnNetworkObject(networkObject, originalPrefab);
        }
    }

    #endregion

    #region Helpers
    [ServerRpc]
    private void serverDebugMsgServerRpc(string msg) {
        if (!IsServer) { return; }
        Debug.Log(msg);
    }

    private GameObject GetWeightedRandomInactivePrefab() {
        // Filter out prefabs that have no inactive instances in the pool
        List<WeightedPrefab> validPrefabs = new List<WeightedPrefab>();
        float _totalWeight = 0f;

        foreach (var wp in weightedPrefabs) {
            if (_objectPool.HasInactiveInstance(wp.Prefab)) {
                validPrefabs.Add(wp);
                _totalWeight += wp.Weight;
            }
        }

        if (validPrefabs.Count == 0) {
            Debug.LogError("No valid prefabs found.");
            return null;
        }

        float randomValue = Random.Range(0f, _totalWeight);
        float cumulativeWeight = 0f;

        foreach (var wp in validPrefabs) {
            cumulativeWeight += wp.Weight;
            if (randomValue <= cumulativeWeight) {
                return wp.Prefab;
            }
        }

        return validPrefabs[validPrefabs.Count - 1].Prefab; // Fallback to the last prefab
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

    public void RemoveEnemyFromList(GameObject enemy) {
        enemyList.Remove(enemy);
        //Debug.Log("Enemy list: " + enemyList.Count);
    }

    #endregion
}
