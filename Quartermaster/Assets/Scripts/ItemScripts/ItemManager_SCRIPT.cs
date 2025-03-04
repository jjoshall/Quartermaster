using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ItemManager : NetworkBehaviour {

    #region Setup
    public static ItemManager instance; //singleton

    // itemClassMap
    private Dictionary<string, Func<InventoryItem>> _itemClassMap = new Dictionary<string, Func<InventoryItem>>();

    [Serializable]
    public struct itemStruct {
        public GameObject worldPrefab; // assign in inspector
        public string inventoryItemClass; // string key that should match a InventoryItem class name
    }

    [Serializable]
    public struct DropEntry {
        public string stringID; // index in itemEntries
        public int quantity; // stack count when dropped. automatically capped to stackLimit of item
        public float dropChance; // 0-1
    }
    #endregion 



    #region InspectorVars
    // Constant during runtime.
    [SerializeField, Range(0, 1), Tooltip("Base drop chance")]
    public float burstDrop_baseRate;
    [SerializeField, Range(0, 0.5f), Tooltip("Increment per kill without a drop")]
    public float burstDrop_dropRateIncrement;
    [SerializeField, Range(0, 10), Tooltip("Max Drop Count")]
    public int burstDrop_dropCount;
    [SerializeField, Range(0, 60.0f), Tooltip("Duration of dropped items")]
    private float droppedItemDuration;
    public float burstdrop_targetEnemiesPerItem;    

    // Used for lookup during worlditem & inventoryitem spawn.
    public List<itemStruct> itemEntries = new List<itemStruct>(); 
    // When burstdrop triggered, chooses entries from this list.
    public List<DropEntry> dropEntries = new List<DropEntry>();
    #endregion



    #region RuntimeVars
    // Killcount based drop rate modifier.
    private float _burstDrop_moddedRate = 0.0f;
    private List<DropEntry> _modifiedDropRates = new List<DropEntry>();
    private int _burstDrop_totalEnemiesKilled = 0;

    // Placeholder variables for potential heuristic input:
    private float _sinceLast_damageTaken = 0.0f; // == Damage taken by players.
    private float _sinceLast_healingValue = 0.0f; // == MedKitsSpawned * MedKitHealingValue
                        // Drama. healingValue - damageTaken < 0
    private float _sinceLast_totalSingleTargetHp = 0; // single target damage
    private float _sinceLast_totalAoeHp = 0; // melee + explosive enemy hp
                        // Drama up when: totalSpawnedEnemyHp > teamDps

    // calculate based on current player inventory.
    private float _playerAoeDps = 0.0f;
    private float _playerSingleTargetDps = 0.0f;


    #endregion




    #region Initialization

    void Awake() {
        if(instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }

        // Map each string to a function that creates an instance of the class
        foreach (itemStruct item in itemEntries) {
            Type itemType = Type.GetType(item.inventoryItemClass);
            if (itemType == null) {
                Debug.LogError("Item class not found: " + item.inventoryItemClass);
                continue;
            }

            _itemClassMap[item.inventoryItemClass] = () => (InventoryItem)Activator.CreateInstance(itemType);
        }

        _burstDrop_moddedRate = burstDrop_baseRate;
    }
    #endregion 
    



    #region Item Functions

    [ServerRpc(RequireOwnership = false)]
    public void SpawnWorldItemServerRpc(int id, 
                                        int quantity, 
                                        float lastUsed, 
                                        Vector3 spawnLoc, 
                                        Vector3 initialVelocity, 
                                        NetworkObjectReference n_playerObj) {
        if (!IsServer) { return; }

        if (!n_playerObj.TryGet(out NetworkObject playerObj)) {
            Debug.Log ("Could not get player object from reference.");
        }

        GameObject newWorldItem = Instantiate(itemEntries[id].worldPrefab);
        NetworkObject netObj = newWorldItem.GetComponent<NetworkObject>();

        if (netObj == null) {
            Debug.LogError("SpawnWorldItemServerRpc: The spawned object is missing a NetworkObject component!");
            Destroy(newWorldItem);  // Prevent stray objects in the scene
            return;
        }

        netObj.transform.position = spawnLoc;
        netObj.GetComponent<Rigidbody>().linearVelocity = initialVelocity;
        netObj.Spawn(true);
        newWorldItem.transform.SetParent(this.gameObject.transform);
        newWorldItem.GetComponent<WorldItem>().InitializeItem(id, quantity, lastUsed);

        // map id number to its stringID
        string stringID = itemEntries[id].inventoryItemClass;
        if (stringID == "PocketInventoryPortalKey") {
            if (PocketInventory.instance.PlayerIsInPocket(playerObj)) {
                PocketInventory.instance.n_droppedPortalKeyInPocket.Value = true;
                PocketInventory.instance.n_storedKeyObj = netObj;
                Debug.Log ("dropped portal key inside pocket");
            }
        }
        
    }

    // Duplicate of spawnWorldItem without pocketinventory check, for use in enemy drops.
    [ServerRpc(RequireOwnership = false)]
    private void EnemyDropServerRpc(int id, 
                                        int quantity, 
                                        float lastUsed, 
                                        Vector3 spawnLoc, 
                                        Vector3 initialVelocity) {
        if (!IsServer) { return; }

        GameObject newWorldItem = Instantiate(itemEntries[id].worldPrefab);
        StartCoroutine(DestroyItemAfterTime(newWorldItem, droppedItemDuration));
        NetworkObject netObj = newWorldItem.GetComponent<NetworkObject>();

        if (netObj == null) {
            Debug.LogError("SpawnWorldItemServerRpc: The spawned object is missing a NetworkObject component!");
            Destroy(newWorldItem);  // Prevent stray objects in the scene
            return;
        }

        netObj.transform.position = spawnLoc;
        netObj.GetComponent<Rigidbody>().linearVelocity = initialVelocity;
        netObj.Spawn(true);
        newWorldItem.transform.SetParent(this.gameObject.transform);
        newWorldItem.GetComponent<WorldItem>().InitializeItem(id, quantity, lastUsed);
        
    }

    // Used as a lookup, and returns an instance of InventoryItem
    public InventoryItem SpawnInventoryItem (string id, int stackQuantity, float timeLastUsed) {
        InventoryItem newInventoryItem = _itemClassMap[id]();
        newInventoryItem.itemID = itemEntries.FindIndex(item => item.inventoryItemClass == id);
        newInventoryItem.quantity = stackQuantity;
        newInventoryItem.lastUsed = timeLastUsed;
        return newInventoryItem;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyWorldItemServerRpc(NetworkObjectReference n_worldItem) {
        if (!IsServer) { return; }

        if (n_worldItem.TryGet(out NetworkObject worldItem)) {
            worldItem.Despawn();
            // Destroy(worldItem.gameObject);
        }
    }

    #endregion 

    #region ThresholdDrop
    // Rolls a multiplier. All drop entries are multiplied by this value.
    // If 
    public void ThresholdBurstDrop(Vector3 position){
        // roll for burst drop against modded rate.
        float dropChanceMultiplier = UnityEngine.Random.Range(0.0f, 1.0f);
        if (dropChanceMultiplier < _burstDrop_moddedRate){
            for (int i = 0; i < burstDrop_dropCount; i++){
                DropItems(position);
            }
            _burstDrop_moddedRate = burstDrop_baseRate;
        } else {
            _burstDrop_moddedRate += burstDrop_dropRateIncrement;
        }
    }

    private void DropItems(Vector3 position){
        float countMultiplier = _burstDrop_moddedRate / burstDrop_baseRate;
        foreach (DropEntry entry in dropEntries){
            // roll for each item
            float itemRoll = UnityEngine.Random.Range(0.0f, 1.0f);
            if (itemRoll * countMultiplier > 1 - entry.dropChance){
                Vector3 randomDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
                randomDirection.Normalize();

                string stringID = entry.stringID;
                int itemID = itemEntries.FindIndex(item => item.inventoryItemClass == stringID);
                EnemyDropServerRpc(itemID, entry.quantity, 0.0f, position, randomDirection);
            }
        }
    }

    private IEnumerator DestroyItemAfterTime(GameObject expiringDrop, float time){
        Debug.Log ("destroying itemDrop after time: " + time);
        yield return new WaitForSeconds(time);
        if (expiringDrop != null){
            Debug.Log ("destroying itemDrop after waiting time");
            NetworkObject netObj = expiringDrop.GetComponent<NetworkObject>();
            DestroyWorldItemServerRpc(netObj);
        }
    }

    #endregion 



    #region HeuristicInput
    // Call on enemy death.
    [ServerRpc(RequireOwnership = false)]
    public void IncrementEnemiesKilledServerRpc(){
        if (!IsServer) { return; }
        _burstDrop_totalEnemiesKilled++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncrementTotalDamageTakenServerRpc(float damage){
        if (!IsServer) { return; }
        _sinceLast_damageTaken += damage;
    }
    #endregion
}
