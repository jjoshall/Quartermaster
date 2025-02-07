using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
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

        // Iterates over item entry list and initializes a function that returns the appropriate InventoryItem object
        //     when given the InventoryItem string name (e.g., "MedKit") as a key in the itemClassMap.
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
    public struct itemStruct
    {
        public GameObject worldPrefab; // Assign in Inspector
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

    public GameObject spawnWorldItem(int id, int quantity, float lastUsed){
        GameObject newWorldItem = Instantiate(itemEntries[id].worldPrefab);
        newWorldItem.GetComponent<WorldItem>().initializeItem(id, quantity, lastUsed);
        return newWorldItem;
    }

    public InventoryItem spawnInventoryItem (string id, int stackQuantity, float timeLastUsed){
        InventoryItem newInventoryItem = itemClassMap[id]();
        newInventoryItem.quantity = stackQuantity;
        newInventoryItem.last_used = timeLastUsed;
        return newInventoryItem;
    }

    // public int getItemID(GameObject item){
    //     for (int i = 0; i < itemEntries.Count; i++){
    //         // if item name is same as itemEntries[i].worldPrefab name
    //         if (item.name == itemEntries[i].worldPrefab.name){
    //             return i;
    //         }
    //     }
    //     Debug.Log ("Could not find item ID for item: " + item + "\nReturned 0. Could be a problem.");
    //     return 0;
    // }
}
