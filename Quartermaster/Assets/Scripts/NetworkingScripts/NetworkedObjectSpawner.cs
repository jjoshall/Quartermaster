using Unity.Netcode;
using UnityEngine;

public class NetworkedObjectSpawner : MonoBehaviour {

    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform spawnedObjectTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

}
