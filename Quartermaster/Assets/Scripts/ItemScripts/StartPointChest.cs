using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class StartPointChest : NetworkBehaviour
{
    [SerializeField] private List<itemStruct> _dropTable = new List<itemStruct>();
    [SerializeField] private GameObject _chestPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        SpawnChestAtSelfServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnChestAtSelfServerRpc()
    {
        if (!IsServer)
        {
            Debug.LogError("SpawnChestAtSelf: This function should only be called on the server.");
            return;
        }
        // Spawn the chest prefab at the current position and rotation of this object
        GameObject chest = Instantiate(_chestPrefab, transform.position, transform.rotation);
        InitDropTable(chest);
        SpawnNetObject(chest);
    }

    private void InitDropTable(GameObject chest)
    {
        // Initialize the drop table on the chest
        ItemChest_SCRIPT itemChest = chest.GetComponent<ItemChest_SCRIPT>();
        if (itemChest != null)
        {
            itemChest.InitDropTable(_dropTable);
        }
        else
        {
            Debug.LogError("ItemChest_SCRIPT component not found on chest prefab.");
        }
    }

    private void SpawnNetObject(GameObject obj)
    {
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError("NetworkObject component not found on object.");
        }
    }
}
