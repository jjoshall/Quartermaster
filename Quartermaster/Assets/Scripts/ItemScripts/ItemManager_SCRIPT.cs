using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ItemManager : NetworkBehaviour
{
    // Singleton
    public static ItemManager instance;
    private Dictionary<string, Func<InventoryItem>> itemClassMap = new Dictionary<string, Func<InventoryItem>>();
    void Awake(){
        if(instance == null){
            instance = this;
        } else {
            Destroy(this);
        }

        // Map each string to a function that creates an instance of the class
        foreach (itemStruct item in itemEntries)
        {
            Type itemType = Type.GetType(item.inventoryItemClass);
            if (itemType == null)
            {
                Debug.LogError("Item class not found: " + item.inventoryItemClass);
                continue;
            }
            itemClassMap[item.inventoryItemClass] = () => (InventoryItem)Activator.CreateInstance(itemType);
        }
    }

    [Serializable]
    public struct itemStruct    // This is the struct for each item in the ItemManager's list
    {
        public GameObject worldPrefab; // assign in inspector
        public string inventoryItemClass; // Fully qualified class name (e.g., "HealthPotionItem")
    }


    public List<itemStruct> itemEntries = new List<itemStruct>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnWorldItemServerRpc(int id, 
                                        int quantity, 
                                        float lastUsed, 
                                        Vector3 spawnLoc, 
                                        Vector3 initialVelocity, 
                                        NetworkObjectReference n_playerObj)
    {
        if (!IsServer)
        {
            return;
        }

        if (!n_playerObj.TryGet(out NetworkObject playerObj))
        {
            Debug.Log ("Could not get player object from reference.");
        }

        GameObject newWorldItem = Instantiate(itemEntries[id].worldPrefab);
        NetworkObject netObj = newWorldItem.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("SpawnWorldItemServerRpc: The spawned object is missing a NetworkObject component!");
            Destroy(newWorldItem);  // Prevent stray objects in the scene
            return;
        }

        netObj.transform.position = spawnLoc;
        netObj.GetComponent<Rigidbody>().linearVelocity = initialVelocity;
        netObj.Spawn(true);
        newWorldItem.GetComponent<WorldItem>().InitializeItem(id, quantity, lastUsed);

        // map id number to its stringID
        string stringID = itemEntries[id].inventoryItemClass;
        if (stringID == "PocketInventoryPortalKey"){
            if (PocketInventory.instance.PlayerIsInPocket(playerObj)){
                PocketInventory.instance.n_droppedPortalKeyInPocket.Value = true;
                PocketInventory.instance.n_storedKeyObj = netObj;
                Debug.Log ("dropped portal key inside pocket");
            }
        }
        
    }


    public InventoryItem SpawnInventoryItem (string id, int stackQuantity, float timeLastUsed){
        InventoryItem newInventoryItem = itemClassMap[id]();
        newInventoryItem.itemID = itemEntries.FindIndex(item => item.inventoryItemClass == id);
        newInventoryItem.quantity = stackQuantity;
        newInventoryItem.lastUsed = timeLastUsed;
        return newInventoryItem;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyWorldItemServerRpc(NetworkObjectReference n_worldItem){
        if (!IsServer)
        {
            return;
        }

        if (n_worldItem.TryGet(out NetworkObject worldItem))
        {
            worldItem.Despawn();
            // Destroy(worldItem.gameObject);
        }
    }


}
