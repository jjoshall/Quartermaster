using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class EnemySpawner : NetworkBehaviour {
    [Header("Spawner Settings")]
    //[SerializeField] private Transform _enemyPrefab;
    [SerializeField] private List<Transform> _enemyPrefabs;
    [SerializeField] private int _maxEnemyInstanceCount = 20;
    public bool isSpawning = true;
    [SerializeField] private float _spawnCooldown = 2f;

    public List<Transform> enemyList = new List<Transform>();
    public List<GameObject> playerList;

    // static 
    public static EnemySpawner instance;

    void Awake() {
        if (instance == null) {
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
            if (enemyList.Count < _maxEnemyInstanceCount && isSpawning) {
                Transform enemyPrefab = GetRandomEnemyPrefab();
                Transform enemyTransform = Instantiate(enemyPrefab, GetRandomPositionOnMap(), Quaternion.identity, transform);
                enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>().enemySpawner = this;
                enemyTransform.GetComponent<BaseEnemyClass_SCRIPT>().enemyType = GetEnemyType(enemyPrefab);
                enemyTransform.GetComponent<NetworkObject>().Spawn(true);
                enemyList.Add(enemyTransform);

                yield return new WaitForSeconds(_spawnCooldown);
            }

            yield return null;
        }
    }

    private EnemyType GetEnemyType(Transform enemyPrefab)
    {
        if (enemyPrefab.GetComponent<MeleeEnemyInherited_SCRIPT>() != null) return EnemyType.Melee;
        //if (enemyPrefab.GetComponent<RangedEnemyInherited_SCRIPT>() != null) return EnemyType.Ranged;
        return EnemyType.Melee;
    }

    private Transform GetRandomEnemyPrefab()
    {
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
