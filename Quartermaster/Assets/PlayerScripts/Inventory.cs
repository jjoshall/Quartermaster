using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
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
        playerObj = transform.parent.gameObject;
        itemAcquisitionRange = playerObj.GetComponentInChildren<ItemAcquisitionRange>().gameObject;

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
        if (Input.GetKeyDown(pickupKey)){
            Debug.Log ("pickup key pressed");
            GameObject closestItem = itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().getClosestItem();
            if (closestItem != null){
                pickUpItem(closestItem);
                DEBUG_PRINT_INVENTORY();
            }
        }

        if (Input.GetKeyDown(dropItemKey)){
            dropItem();
            DEBUG_PRINT_INVENTORY();
        }

        if (Input.GetKeyDown(useItemKey)){
            if (inventory[currentInventoryIndex] == null){
                Debug.Log ("Use Item Key Pressed.");

                // Use the item effect.
                inventory[currentInventoryIndex].use(playerObj);

                // If item is consumable, decrement quantity. If quantity is 0, remove item from inventory.
                bool isConsumable = inventory[currentInventoryIndex].isConsumable();
                if (isConsumable){
                    inventory[currentInventoryIndex].quantity--;
                    if (inventory[currentInventoryIndex].quantity <= 0){
                        inventory[currentInventoryIndex] = null;
                        currentHeldItems--;
                    }
                }
            }
        }

        if (Input.GetKeyDown(selectItemOneKey)){
            currentInventoryIndex = 0;
            DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemTwoKey)){
            currentInventoryIndex = 1;
            DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemThreeKey)){
            currentInventoryIndex = 2;
            DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemFourKey)){
            currentInventoryIndex = 3;
            DEBUG_SELECT_SLOT();
        }
    }

    void pickUpItem (GameObject pickedUp){
        int itemID = pickedUp.GetComponent<WorldItem>().getItemID();
        string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        int stackQuantity = pickedUp.GetComponent<WorldItem>().getStackQuantity();
        float lastUsed = pickedUp.GetComponent<WorldItem>().getLastUsed();

        itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().removeItem(pickedUp); // remove the item from the list of items in range

        // spawnInventoryItem uses stringID for lookup. 
        InventoryItem newItem = ItemManager.instance.spawnInventoryItem(stringID, stackQuantity, lastUsed);

        for (int i = 0; i < inventory.Length; i++){
            if (inventory[i].itemID == newItem.itemID){
                inventory[i].quantity += newItem.quantity;
                if (inventory[i].quantity > inventory[i].stackLimit()){
                    newItem.quantity = inventory[i].quantity - inventory[i].stackLimit(); // left over quantity
                    inventory[i].quantity = inventory[i].stackLimit(); // cap curr item to stackLimit
                } else {
                    // same item as curr index and total quantity under stack limit.
                    // aka, we stacked into an existing stack with non left over.
                    
                    return;
                }
            }
        }

        if (newItem.quantity <= 0){
            return;
        }

        // else add to first empty slot
        if (currentHeldItems < maxInventorySize){
            addToFirstEmptySlot(pickedUp); // convert worlditem to inventoryitem
            Destroy(pickedUp); // destroy the original worlditem object
        }
    }

    void addToFirstEmptySlot (GameObject item){
        for (int i = 0; i < inventory.Length; i++){
            if (inventory[i] == null){
                addItem(item, i);
                break;
            }
        }
    }

    // void addItem (GameObject item, int index){
    //     int itemId = item.GetComponent<WorldItem>().getItemID();
    //     int stackQuantity = item.GetComponent<WorldItem>().getStackQuantity();
    //     float lastUsed = item.GetComponent<WorldItem>().getLastUsed();
    //     Debug.Log ("itemID: " + itemId);                                // get ID from WorldItem
    //     string itemString = ItemManager.instance.itemEntries[itemId].inventoryItemClass;   // map ID to string

    //     InventoryItem lootedItem = ItemManager.instance.spawnInventoryItem(itemString, stackQuantity, lastUsed);    // create InventoryItem object
    //     inventory[index] = lootedItem;
    //     currentHeldItems++;
    // }

    void dropItem (){

        // If null, no selected item.
        if (inventory[currentInventoryIndex] == null){
            return;
        }

        int selectedItemId = inventory[currentInventoryIndex].itemID;
        int stackQuantity = inventory[currentInventoryIndex].quantity;
        float lastUsed = inventory[currentInventoryIndex].last_used;
        inventory[currentInventoryIndex] = null;

        GameObject droppedItem = ItemManager.instance.spawnWorldItem(selectedItemId, stackQuantity, lastUsed);

        // give it a forward velocity to throw the item forward
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        rb.linearVelocity = orientation.forward * 10;

        // Drop the item in front of the player
        Vector3 dropPosition = playerObj.transform.position; //  + playerParent.transform.forward;
        droppedItem.transform.position = dropPosition;

        itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().addItem(droppedItem);
        currentHeldItems--;
    }

    // bool slotIsEmpty(int index){
    //     return inventory[index] == null;
    // }
    void DEBUG_PRINT_INVENTORY(){
        string DEBUG_STRING = "DEBUG: Inventory: \n";
        foreach (InventoryItem item in inventory){
            if (item != null){
                DEBUG_STRING += item.itemID + ", ";
            }
        }
        Debug.Log(DEBUG_STRING);
    }

    void DEBUG_SELECT_SLOT(){
        string DEBUG_STRING = "DEBUG: Selected: \n";
        DEBUG_STRING += "Current Index: " + currentInventoryIndex + "\n";
        DEBUG_STRING += "Item: ";
        if (inventory[currentInventoryIndex] != null){
            DEBUG_STRING += inventory[currentInventoryIndex].itemID;
        } else {
            DEBUG_STRING += "none";
        }
        Debug.Log(DEBUG_STRING);
    }

}
