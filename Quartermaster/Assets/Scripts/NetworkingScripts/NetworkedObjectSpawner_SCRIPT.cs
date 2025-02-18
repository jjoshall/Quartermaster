using Unity.Netcode;
using UnityEngine;

public class NetworkedObjectSpawner : MonoBehaviour {

    [SerializeField] private Transform _spawnedObjectPrefab;
    private Transform _spawnedObjectTransform;

    void Start() {
        _spawnedObjectTransform = Instantiate(_spawnedObjectPrefab);
        _spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

}
