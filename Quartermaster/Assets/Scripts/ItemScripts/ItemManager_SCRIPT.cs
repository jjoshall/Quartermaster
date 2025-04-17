using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class ItemManager : NetworkBehaviour {

    #region Setup
    public static ItemManager instance; //singleton
    void Awake() {
        if(instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
    }

    [Serializable]
    public struct itemStruct {
        public GameObject itemPrefab;
        public int quantity;
    }

    [Serializable]
    public struct DropEntry {
        public List<itemStruct> itemDrops;
        public float dropChancePercent; // 0-100
    }
    #endregion 

    [Tooltip("Each entry can have a list of dropped items that are dropped all at once when rolled")] 
    public List<DropEntry> dropTable = new List<DropEntry>();

    #region Roll All
    // Individually rolls against all entries in the drop table. o(n)
    public void RollDropTable(Vector3 position){
        float countMultiplier = 1.0f; //_burstDrop_moddedRate / burstDrop_baseRate; // >1 multiplier.
        for (int i = 0; i < dropTable.Count; i++){
            // roll for each item
            float itemRoll = UnityEngine.Random.Range(0.0f, 1.0f); // 0-1.
            if (itemRoll * countMultiplier > ( 1 - (dropTable[i].dropChancePercent / 100) )   ){
                Vector3 randomDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
                randomDirection.Normalize();

                DropAnEntryInTableServerRpc(i, position, randomDirection);
            }
        }
    }


    #endregion 
    #region Drop function

    // Drops all the items in an entry.
    [ServerRpc(RequireOwnership = false)]
    private void DropAnEntryInTableServerRpc(int index,
                                        Vector3 spawnLoc, 
                                        Vector3 initialVelocity) {
        if (!IsServer) { return; }
        if (spawnLoc == null) {
            Debug.LogError("DropItemServerRpc: spawnLoc is null!");
            return;
        }

        if (initialVelocity == null) {
            Debug.LogError("DropItemServerRpc: initialVelocity is null!");
            return;
        }
        foreach (itemStruct item in dropTable[index].itemDrops) {
            if (item.itemPrefab == null) {
                Debug.LogError("DropItemServerRpc: itemPrefab is null!");
                continue;
            }
            if (item.itemPrefab.GetComponent<Item>() == null){
                Debug.LogError("DropItemServerRpc: itemPrefab is missing MonoItem component!");
                continue;
            }

            Debug.Log ("DropItemServerRpc: Spawning item " + item.itemPrefab.GetComponent<Item>().uniqueID + " at " + spawnLoc.ToString() + " with quantity " + item.quantity.ToString() + ".");
            GameObject newItem = Instantiate(item.itemPrefab, spawnLoc, Quaternion.identity);
            newItem.GetComponent<Item>().quantity = item.quantity; // set quantity to the item stack size.
            newItem.GetComponent<Item>().userRef = null; // set user ref to the enemy.
            newItem.GetComponent<Item>().IsPickedUp = false; // set IsPickedUp to false.
            
            NetworkObject n_newItem = newItem.GetComponent<NetworkObject>();
            if (n_newItem == null) {
                Debug.LogError("DropItemServerRpc: The spawned object is missing a NetworkObject component!");
                Destroy(newItem);  // Prevent stray objects in the scene
                return;
            }

            n_newItem.transform.position = spawnLoc;
            n_newItem.GetComponent<Rigidbody>().linearVelocity = initialVelocity;
            n_newItem.Spawn(true); // Spawn the object on the network
            newItem.transform.SetParent(this.gameObject.transform); // Set the parent to this object.
            newItem.GetComponent<Item>().OnSpawn();
        }
    }

    #endregion 

    #region Helper
    #endregion
    // Drop a specific item at a specific position. Written for creative mode / dev function. 
    // Drops slightly above ground to avoid slipping through the ground.
    public void DropSpecificItem(int index, Vector3 position){
        Vector3 randVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
        Vector3 positionYOffsetByOne = new Vector3(position.x, position.y + 1.0f, position.z);
        
        DropAnEntryInTableServerRpc (index, positionYOffsetByOne, randVelocity);
    }
}
