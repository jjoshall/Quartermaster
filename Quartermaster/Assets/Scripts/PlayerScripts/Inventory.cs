using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class Inventory : NetworkBehaviour
{
    private const bool DEBUG_FLAG = true;
    private GameObject playerObj;
    private GameObject itemAcquisitionRange;
    [Header("Orientation for dropItem direction")]
    public Transform orientation;

    [Header("Inventory Keybinds")]
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropItemKey = KeyCode.Q;
    public KeyCode useItemKey = KeyCode.F;
    public KeyCode selectItemOneKey = KeyCode.Alpha1;
    public KeyCode selectItemTwoKey = KeyCode.Alpha2;
    public KeyCode selectItemThreeKey = KeyCode.Alpha3;
    public KeyCode selectItemFourKey = KeyCode.Alpha4;

    // public struct InventoryItem
    // {
    //     public InventoryItem item;
    //     public int quantity;
    //     public float last_used;

    //     public InventoryItem(InventoryItem item, int quantity, float last_used)
    //     {
    //         this.item = item;
    //         this.quantity = quantity;
    //         this.last_used = last_used;
    //     }
    // }

    private InventoryItem[] inventory; // List of items in the player's inventory

    private int currentInventoryIndex = 0; // The index of the currently selected item in the inventory
    private int currentHeldItems = 0; // The number of items the player is currently holding
    private int maxInventorySize = 4; // Maximum number of items the player can hold
    private GameObject selectedItem; // The item that the player has selected to use

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerObj = this.gameObject;
        itemAcquisitionRange = playerObj.GetComponentInChildren<ItemAcquisitionRange>().gameObject;

        // if (orientation == null)
        // {
        //     orientation = playerObj.transform;
        // }

        inventory = new InventoryItem[maxInventorySize];
        for (int i = 0; i < maxInventorySize; i++){
            // InventoryItem newSlot = new InventoryItem(null, 0, 0);
            inventory[i] = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        MyInput();
    }


    void MyInput(){
        if (!IsOwner) {
            Debug.Log("Ownerid: " + OwnerClientId);
            return;
        }

        if (Input.GetKeyDown(pickupKey)){
            GameObject closestItem = itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().GetClosestItem();
            if (closestItem != null){
                PickUpItem(closestItem);
                // DEBUG_PRINT_INVENTORY();
            }
        }

        if (Input.GetKeyDown(dropItemKey)){
            DropItem();
            string droppingPlayerNetworkId = playerObj.GetComponent<NetworkObject>().NetworkObjectId.ToString();
            Debug.Log("MyInput: drop item from player: " + droppingPlayerNetworkId);
            // DEBUG_PRINT_INVENTORY();
        }

        if (Input.GetKeyDown(useItemKey)){
            UseItem();
            // DEBUG_PRINT_INVENTORY();
        }

        if (Input.GetKeyDown(selectItemOneKey)){
            currentInventoryIndex = 0;
            // DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemTwoKey)){
            currentInventoryIndex = 1;
            // DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemThreeKey)){
            currentInventoryIndex = 2;
            // DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemFourKey)){
            currentInventoryIndex = 3;
            // DEBUG_SELECT_SLOT();
        }
    }

    void UseItem (){
        if (inventory[currentInventoryIndex] != null){

            // Use the item effect.
            if (playerObj == null){
                Debug.Log("playerObj is null");
                playerObj = transform.parent.gameObject;
            }

            inventory[currentInventoryIndex].Use(playerObj);

            if (inventory[currentInventoryIndex].quantity <= 0){
                inventory[currentInventoryIndex] = null;
                currentHeldItems--;
            }
        }
    }

    void PickUpItem (GameObject pickedUp){
        int itemID = pickedUp.GetComponent<WorldItem>().GetItemID();
        string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (stringID == "PocketInventoryPortalKey"){
            // if (this.gameObject == PocketInventory.instance.playerInsidePocket()){
            if (PocketInventory.instance.PlayerIsInPocket(playerObj.GetComponent<NetworkObject>())){
                PocketInventory.instance.n_droppedPortalKeyInPocket.Value = false; 
            }
        }

        int stackQuantity = pickedUp.GetComponent<WorldItem>().GetStackQuantity();
        float lastUsed = pickedUp.GetComponent<WorldItem>().GetLastUsed();

        itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().RemoveItem(pickedUp); // remove the item from the list of items in range
        ItemManager.instance.DestroyWorldItemServerRpc(pickedUp.GetComponent<NetworkObject>());

        // spawnInventoryItem uses stringID for lookup. 
        InventoryItem newItem = ItemManager.instance.SpawnInventoryItem(stringID, stackQuantity, lastUsed);

        if (TryStackItem (newItem))
        {
            return;
        } 

        if (newItem.quantity <= 0){
            return;
        }

        // else add to first empty slot
        if (currentHeldItems < maxInventorySize){
            AddToFirstEmptySlot(newItem); // convert worlditem to inventoryitem
        }
    }

    // Return true if fully merged into existing stacks.
    bool TryStackItem (InventoryItem newItem){
        for (int i = 0; i < inventory.Length; i++){
            if (inventory[i] == null){
                continue;
            }
            if (inventory[i].itemID == newItem.itemID){
                inventory[i].quantity += newItem.quantity;
                if (inventory[i].quantity > inventory[i].StackLimit()){
                    newItem.quantity = inventory[i].quantity - inventory[i].StackLimit(); // left over quantity
                    inventory[i].quantity = inventory[i].StackLimit(); // cap curr item to stackLimit
                } else {
                    // same item as curr index and total quantity under stack limit.
                    // aka, we stacked into an existing stack with non left over.
                    
                    return true;
                }
            }
        }
        return false;
    }

    void AddToFirstEmptySlot (InventoryItem item){
        for (int i = 0; i < inventory.Length; i++){
            if (inventory[i] == null){
                inventory[i] = item;
                currentHeldItems++;
                return;
            }
        }
    }


    void DropItem (){
        // check if this is the own
        string droppingPlayerNetworkId = playerObj.GetComponent<NetworkObject>().NetworkObjectId.ToString();
        Debug.Log("drop item from player: " + droppingPlayerNetworkId);
        
        // If null, no selected item.
        if (inventory[currentInventoryIndex] == null){
            return;
        }

        int selectedItemId = inventory[currentInventoryIndex].itemID;
        string stringID = ItemManager.instance.itemEntries[selectedItemId].inventoryItemClass;
        int stackQuantity = inventory[currentInventoryIndex].quantity;
        float lastUsed = inventory[currentInventoryIndex].lastUsed;
        inventory[currentInventoryIndex] = null;

        Vector3 initVelocity = orientation.forward * 10;

        NetworkObjectReference n_playerObj = playerObj.GetComponent<NetworkObject>();

        ItemManager.instance.SpawnWorldItemServerRpc(
                                    selectedItemId, 
                                    stackQuantity, 
                                    lastUsed, 
                                    this.transform.position, 
                                    initVelocity,
                                    n_playerObj);
        
        Debug.Log ("dropped stringID: " + stringID);

        currentHeldItems--;
    }

    void DEBUG_PRINT_INVENTORY(){
        string DEBUG_STRING = "DEBUG: Inventory: \n";
        for (int i = 0; i < inventory.Length; i++){
            InventoryItem item = inventory[i];
            if (item != null){
                string itemString = ItemManager.instance.itemEntries[item.itemID].inventoryItemClass;
                DEBUG_STRING += "Slot " + (i+1) + ": " + itemString + "(" + item.itemID + "): " + item.quantity + "x, ";
            }
        }
        Debug.Log(DEBUG_STRING);
    }

    void DEBUG_SELECT_SLOT(){
        string DEBUG_STRING = "DEBUG: Selected: \n";
        DEBUG_STRING += "Current Index: " + (currentInventoryIndex+1) + ", ";
        DEBUG_STRING += "Item: ";
        if (inventory[currentInventoryIndex] != null){
            int itemID = inventory[currentInventoryIndex].itemID;
            string itemString = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
            DEBUG_STRING += itemString + "(" + itemID + "): " + inventory[currentInventoryIndex].quantity + "x";
        } else {
            DEBUG_STRING += "none";
        }
        Debug.Log(DEBUG_STRING);
    }

}