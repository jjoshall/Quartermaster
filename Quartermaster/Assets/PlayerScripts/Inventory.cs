using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    private GameObject playerObj;
    private GameObject itemAcquisitionRange;

    [Header("Inventory Keybinds")]
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropItemKey = KeyCode.Q;
    public KeyCode selectItemOneKey = KeyCode.Alpha1;
    public KeyCode selectItemTwoKey = KeyCode.Alpha2;
    public KeyCode selectItemThreeKey = KeyCode.Alpha3;
    public KeyCode selectItemFourKey = KeyCode.Alpha4;

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

        if (Input.GetKeyDown(selectItemOneKey)){
            currentInventoryIndex = 0;
        }

        if (Input.GetKeyDown(selectItemTwoKey)){
            currentInventoryIndex = 1;
        }

        if (Input.GetKeyDown(selectItemThreeKey)){
            currentInventoryIndex = 2;
        }

        if (Input.GetKeyDown(selectItemFourKey)){
            currentInventoryIndex = 3;
        }
    }

    void pickUpItem (GameObject pickedUp){
        if (currentHeldItems < maxInventorySize){
            addToFirstEmptySlot(pickedUp); // convert worlditem to inventoryitem
            itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().removeItem(pickedUp); // remove the item from the list of items in range
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

    void addItem (GameObject item, int index){
        int itemId = item.GetComponent<WorldItem>().itemID;                                // get ID from WorldItem
        string itemString = ItemManager.instance.itemEntries[itemId].inventoryItemClass;   // map ID to string

        InventoryItem lootedItem = ItemManager.instance.spawnInventoryItem(itemString);    // create InventoryItem object
        inventory[index] = lootedItem;
        currentHeldItems++;
    }

    void dropItem (){
        if (inventory[currentInventoryIndex] == null){
            return;
        }

        int selectedItemId = inventory[currentInventoryIndex].itemID;
        inventory[currentInventoryIndex] = null; // C# has a garbage collector(?)

        GameObject droppedItem = ItemManager.instance.spawnWorldItem(selectedItemId);

        // Drop the item in front of the player
        Vector3 dropPosition = playerObj.transform.position; //  + playerParent.transform.forward;
        droppedItem.transform.position = dropPosition;

        itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().addItem(droppedItem);
        currentHeldItems--;
    }

    void DEBUG_PRINT_INVENTORY(){
        string DEBUG_STRING = "DEBUG: Inventory: \n";
        foreach (InventoryItem item in inventory){
            if (item != null){
                DEBUG_STRING += item.itemID + ", ";
            }
        }
        Debug.Log(DEBUG_STRING);
    }

}
