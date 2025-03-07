using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BulletTracerManager : NetworkBehaviour
{
    public static BulletTracerManager Instance { get; private set; }

    [SerializeField] private GameObject _bulletTrailPrefab;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Persist across scenes
        } else {
            Destroy(gameObject);
            return;
        }
        Debug.Log("BulletTracerManager awake, bullet trail Prefab: " + _bulletTrailPrefab);
        if (_bulletTrailPrefab == null) {
            _bulletTrailPrefab = Resources.Load<GameObject>("BulletTrailPrefab");
            Debug.Log("BulletTracerPrefab loaded dynamically: " + _bulletTrailPrefab);
            if(_bulletTrailPrefab == null) {
                Debug.LogError("Failed to load BulletTracerPrefab from Resources!");
            }
        }
    }

    public void RequestSpawnTracer(Vector3 startPoint, Vector3 endPoint) {
        if (IsServer) {
            SpawnTracer(startPoint, endPoint);
        } else {
            RequestSpawnTracerServerRpc(startPoint, endPoint);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnTracerServerRpc(Vector3 startPoint, Vector3 endPoint) {
        SpawnTracer(startPoint, endPoint);
    }

    private void SpawnTracer(Vector3 startPoint, Vector3 endPoint) {
        GameObject tracerInstance = Instantiate(_bulletTrailPrefab);
        tracerInstance.GetComponent<NetworkObject>().Spawn();
        tracerInstance.GetComponent<BulletTracer>().SetupTracerClientRpc(startPoint, endPoint);
    }
}
