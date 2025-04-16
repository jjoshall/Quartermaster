using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;
using Unity.BossRoom.Infrastructure;    // Add this for network object pooling

public class EnemySpawner : NetworkBehaviour {    
    [Header("Spawner Timer")]
    private float _lastSpawnTime = 0f;
    public float spawnCooldown = 2f;

    [Header("Spawner Settings")]
    [HideInInspector] public NetworkVariable<bool> isSpawning = new NetworkVariable<bool>(false);
    public List<EnemySpawnData> _enemySpawnData = new List<EnemySpawnData>();
    [SerializeField] private int _maxEnemyInstanceCount = 50;
    [HideInInspector] public float _totalWeight = 0f;

    // Pool initialization parameters
    [SerializeField] private int _initialPoolSize = 10;

    [SerializeField] private float globalAggroUpdateInterval = 10.0f;
    private float globalAggroUpdateTimer = 0.0f;
    public Vector3 globalAggroTarget = new Vector3(0, 0, 0);

    [SerializeField] private List<GameObject> _enemySpawnPoints;
    //[SerializeField] private List<GameObject> _enemyPackSpawnPoints;

    public List<Transform> enemyList = new List<Transform>();
    private List<GameObject> playerList; // players in game.
    public List<GameObject> activePlayerList; // players in active playable area.
    public List<GameObject> inactiveAreas; // pathable areas that are not playable. set in inspector.
                                           // players in these areas will be occluded from activePlayerList.
                                           // for pathing / enemyspawner / aidirector purposes.

    // static 
    public static EnemySpawner instance;

    // Reference NetworkObjectPool
    private NetworkObjectPool _objectPool;

    [System.Serializable]
    public class EnemySpawnData {
        public Transform enemyPrefab;
        public float spawnWeight = 1f;
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

        // Register enemy prefabs to the pool
        InitializeEnemyPool();

        CalculateTotalWeight();

        NetworkManager.Singleton.OnClientConnectedCallback += RefreshPlayerLists;
        NetworkManager.Singleton.OnClientDisconnectCallback += RefreshPlayerLists;
    }

    private void InitializeEnemyPool() {
        foreach (var enemyData in _enemySpawnData) {
            GameObject prefab = enemyData.enemyPrefab.gameObject;
            _objectPool.AddPrefab(prefab, _initialPoolSize);
        }
    }

    private void RefreshPlayerLists(ulong u) {
        playerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));

        activePlayerList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
        foreach (GameObject area in inactiveAreas){
            foreach (GameObject player in activePlayerList){
                Collider areaCollider = area.GetComponent<Collider>();
                if (areaCollider != null && areaCollider.bounds.Contains(player.transform.position)){
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

    private void SpawnOverTime() {
        if (!IsServer) return;

        if (Time.time - _lastSpawnTime < spawnCooldown) {
            return;
        }

        // less than max enemies, and more than 0 players in playable area.
        // Managed by InactiveAreaCollider s adding/removing from activePlayerList
        if (enemyList.Count < _maxEnemyInstanceCount && activePlayerList.Count > 0) {
            Transform enemyPrefab = GetWeightedRandomEnemyPrefab();

            if (enemyPrefab != null) {
                Vector3 spawnPosition = GetSpawnPoint();

                try {
                    // Get a pooled enemy object
                    NetworkObject networkObject = _objectPool.GetNetworkObject(
                        enemyPrefab.gameObject,
                        spawnPosition,
                        Quaternion.identity
                    );

                    if (networkObject != null) {
                        Transform enemyTransform = networkObject.transform;

                        // Ensure position is correct
                        enemyTransform.position = spawnPosition;

                        // Setup enemy properties
                        BaseEnemyClass_SCRIPT enemyScript = enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>();
                        if (enemyScript != null) {
                            enemyScript.enemySpawner = this;
                            enemyScript.enemyType = GetEnemyType(enemyPrefab);

                            UnityEngine.AI.NavMeshAgent agent = enemyTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();
                            if (agent != null) {
                                agent.Warp(spawnPosition);  // Warp agent to spawn position
                                agent.enabled = true; // Enable the agent
                            }

                            // Spawn network object
                            if (!networkObject.IsSpawned) {
                                networkObject.Spawn(true);
                            }

                            Health hpComponent = enemyTransform.GetComponent<Health>();
                            if (hpComponent != null) {
                                hpComponent.CurrentHealth.Value = hpComponent.MaxHealth;
                            }

                            enemyList.Add(enemyTransform);

                            // Set parent and update speed
                            enemyTransform.SetParent(this.gameObject.transform);
                            enemyScript.UpdateSpeedServerRpc();

                            _lastSpawnTime = Time.time; // Update last spawn time
                        }
                    }
                }
                catch (System.Exception e) {
                    Debug.LogError("Error spawning enemy: " + e.Message);
                }
            }
        }
    }

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

    private EnemyType GetEnemyType(Transform enemyPrefab) {
        if (enemyPrefab.GetComponent<MeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
        if (enemyPrefab.GetComponent<ExplosiveMeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
        if (enemyPrefab.GetComponent<RangedEnemyInherited_SCRIPT>() != null) return EnemyType.Ranged;
        return EnemyType.Melee;
    }

    private Transform GetWeightedRandomEnemyPrefab() {
        if (_enemySpawnData.Count == 0) return null;
        if (_totalWeight <= 0f) return null;

        float randomValue = Random.Range(0f, _totalWeight);
        float weightSum = 0f;

        foreach (var enemyData in _enemySpawnData) {
            weightSum += enemyData.spawnWeight;
            if (randomValue <= weightSum) {
                return enemyData.enemyPrefab;
            }
        }

        return _enemySpawnData[0].enemyPrefab;
    }

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
            enemyList.Remove(networkObject.transform);
            
            // Return to pool
            GameObject prefab = GetPrefabForEnemy(networkObject.gameObject);
            if (prefab != null) {
                _objectPool.ReturnNetworkObject(networkObject, prefab);
            }
            else {
                // Fallback to destroy if prefab not found (shouldn't happen)
                networkObject.Despawn();
            }
        }
    }

    private GameObject GetPrefabForEnemy(GameObject enemy) {
        // Find matching prefab for this enemy instance
        foreach (var enemyData in _enemySpawnData) {
            if (enemy.name.StartsWith(enemyData.enemyPrefab.name)) {
                return enemyData.enemyPrefab.gameObject;
            }
        }
        return null;
    }

    #endregion

    #region Helpers
    [ServerRpc]
    private void serverDebugMsgServerRpc(string msg) {
        if (!IsServer) { return; }
        Debug.Log(msg);
    }
    public void CalculateTotalWeight() {
        _totalWeight = 0f;

        foreach (var data in _enemySpawnData) {
            _totalWeight += data.spawnWeight;
        }

        if (_totalWeight <= 0f) {
            Debug.LogError("Total weight is less than or equal to 0.");
            serverDebugMsgServerRpc("Total weight is less than or equal to 0.");
        }
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
    #endregion
}
