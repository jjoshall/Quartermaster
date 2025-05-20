using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class ChestSpawnPoint : NetworkBehaviour
{
    [Serializable]
    public struct dropTableStruct
    {
        public List<itemStruct> dropTable;
        public int weight;
    }


    [Header("Scene Settings (const)")]
    [Tooltip("Assign prefab for item chests"), SerializeField]                  private GameObject _chestPrefab;
    [Tooltip("Random if true, else cycles through list"), SerializeField]       private bool _GetRandom = false;                 // if true, get random; else cycle through. 
    [Tooltip("Will spawn a chest every time a client joins. Overrides below setting."), SerializeField]   private bool _SpawnChestOnNetworkSpawn = false;
    [Tooltip("Spawn chest once on server start"), SerializeField]               private bool _SpawnChestOnlyOnceOnServer = false; 

    [SerializeField] private List<dropTableStruct> _possibleDropTableList = new List<dropTableStruct>();

    #region Runtime
    #endregion
    private NetworkVariable<int> _currentDropTableIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        if (_SpawnChestOnNetworkSpawn)
            SpawnChestAtSelfServerRpc();
        else if (_SpawnChestOnlyOnceOnServer && IsServer)
            SpawnChestAtSelfServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnChestAtSelfServerRpc()
    {
        if (!IsServer)
        {
            Debug.LogError("SpawnChestAtSelf: This function should only be called on the server.");
            return;
        }
        // Spawn the chest prefab at the current position and rotation of this object
        GameObject chest = Instantiate(_chestPrefab, transform.position, transform.rotation);
        SpawnNetObject(chest);
        InitDropTable(chest);
    }

    private void InitDropTable(GameObject chest)
    {
        // Initialize the drop table on the chest
        ItemChest_SCRIPT itemChest = chest.GetComponent<ItemChest_SCRIPT>();
        if (itemChest != null)
        {
            List<itemStruct> dropTable = _GetRandom ? GrabRandomDropTable() : GrabNextDropTable();

            itemChest.InitDropTable(dropTable);
        }
        else
        {
            Debug.LogError("ItemChest_SCRIPT component not found on chest prefab.");
        }
    }

    private List<itemStruct> GrabRandomDropTable()
    {
        int totalWeight = 0;
        foreach (var dropTable in _possibleDropTableList)
        {
            totalWeight += dropTable.weight;
        }
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        // grab a random drop table using randomValue
        foreach (var dropTable in _possibleDropTableList)
        {
            if (randomValue < dropTable.weight)
            {
                return dropTable.dropTable;
            }
            randomValue -= dropTable.weight;
        }
        Debug.LogError("No drop table found for random value: " + randomValue);
        return null; // or return a default drop table
    }

    private List<itemStruct> GrabNextDropTable()
    {
        // grab the next drop table in the list
        int index = _currentDropTableIndex.Value;
        if (index >= _possibleDropTableList.Count)
        {
            index = 0;
        }
        IncrementDropTableIndexServerRpc();
        return _possibleDropTableList[index].dropTable;
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

    [ServerRpc(RequireOwnership = false)]
    private void IncrementDropTableIndexServerRpc()
    {
        if (!IsServer)
        {
            Debug.LogError("IncrementDropTableIndex: This function should only be called on the server.");
            return;
        }
        _currentDropTableIndex.Value = _currentDropTableIndex.Value + 1;
        if (_currentDropTableIndex.Value >= _possibleDropTableList.Count)
        {
            _currentDropTableIndex.Value = 0;
        }
    }
}
