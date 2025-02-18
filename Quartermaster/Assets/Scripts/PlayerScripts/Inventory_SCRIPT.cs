using UnityEngine;
using Unity.Netcode;

public class Inventory : NetworkBehaviour {
    private const bool DEBUG_FLAG = true;
    private GameObject _playerObj;
    private GameObject _itemAcquisitionRange;

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

    private InventoryItem[] _inventory; // List of items in the player's inventory

    private int _currentInventoryIndex = 0; // The index of the currently selected item in the inventory
    private int _currentHeldItems = 0; // The number of items the player is currently holding
    private int _maxInventorySize = 4; // Maximum number of items the player can hold
    private GameObject _selectedItem; // The item that the player has selected to use

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        _playerObj = this.gameObject;
        _itemAcquisitionRange = _playerObj.GetComponentInChildren<ItemAcquisitionRange>().gameObject;

        // if (orientation == null)
        // {
        //     orientation = playerObj.transform;
        // }

        _inventory = new InventoryItem[_maxInventorySize];
        for (int i = 0; i < _maxInventorySize; i++) {
            // InventoryItem newSlot = new InventoryItem(null, 0, 0);
            _inventory[i] = null;
        }
    }

    // Update is called once per frame
    void Update() {
        MyInput();
    }


    void MyInput() {
        if (!IsOwner) return;
        

        if (Input.GetKeyDown(pickupKey)) {
            GameObject closestItem = _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().GetClosestItem();
            if (closestItem != null) {
                PickUpItem(closestItem);
                // DEBUG_PRINT_INVENTORY();
            }
        }

        if (Input.GetKeyDown(dropItemKey)) {
            DropSelectedItem();
            // DEBUG_PRINT_INVENTORY();
        }

        if (Input.GetKeyDown(useItemKey)) {
            UseItem();
            // DEBUG_PRINT_INVENTORY();
        }

        if (Input.GetKeyDown(selectItemOneKey)) {
            _currentInventoryIndex = 0;
            // DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemTwoKey)) {
            _currentInventoryIndex = 1;
            // DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemThreeKey)) {
            _currentInventoryIndex = 2;
            // DEBUG_SELECT_SLOT();
        }

        if (Input.GetKeyDown(selectItemFourKey)) {
            _currentInventoryIndex = 3;
            // DEBUG_SELECT_SLOT();
        }
    }

    void UseItem () {
        if (_inventory[_currentInventoryIndex] != null) {
            // Use the item effect.
            if (_playerObj == null) {
                Debug.Log("playerObj is null");
                _playerObj = transform.parent.gameObject;
            }

            _inventory[_currentInventoryIndex].Use(_playerObj);

            if (_inventory[_currentInventoryIndex].quantity <= 0) {
                _inventory[_currentInventoryIndex] = null;
                _currentHeldItems--;
            }
        }
    }

    void PickUpItem (GameObject pickedUp) {
        int itemID = pickedUp.GetComponent<WorldItem>().GetItemID();
        string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (stringID == "PocketInventoryPortalKey") {
            // if (this.gameObject == PocketInventory.instance.playerInsidePocket()){
            if (PocketInventory.instance.PlayerIsInPocket(_playerObj.GetComponent<NetworkObject>())) {
                PocketInventory.instance.clearDroppedKeyServerRpc();
            }
        }

        int stackQuantity = pickedUp.GetComponent<WorldItem>().GetStackQuantity();
        float lastUsed = pickedUp.GetComponent<WorldItem>().GetLastUsed();

        _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().RemoveItem(pickedUp); // remove the item from the list of items in range
        ItemManager.instance.DestroyWorldItemServerRpc(pickedUp.GetComponent<NetworkObject>());

        Debug.Log("Spawning a new inventoryItem for pickup: " + stringID);
        // spawnInventoryItem uses stringID for lookup. 
        InventoryItem newItem = ItemManager.instance.SpawnInventoryItem(stringID, stackQuantity, lastUsed);
        
        
        // If stackable. Stack.
        if (TryStackItem (newItem)) { Debug.Log ("Stacked the item"); return; } 
        if (newItem.quantity <= 0) { return; } // Might be redundant but leave it here in case of 0

        // Weapon check.
        if (newItem.IsWeapon()){
            Debug.Log ("newItem.Isweapon() is true");;
            int weaponSlot = HasWeapon();
            if (weaponSlot != -1){
                Debug.Log ("We have a weapon at slot " + weaponSlot + ". Dropping it.");
                DropItem(weaponSlot);
            }
        }

        // else add to first empty slot
        if (_currentHeldItems < _maxInventorySize) {
            AddToFirstEmptySlot(newItem); // convert worlditem to inventoryitem
        }
    }

    // Return true if fully merged into existing stacks.
    bool TryStackItem (InventoryItem newItem) {
        for (int i = 0; i < _inventory.Length; i++){
            if (_inventory[i] == null) {
                continue;
            }

            if (_inventory[i].itemID == newItem.itemID) {
                _inventory[i].quantity += newItem.quantity;
                if (_inventory[i].quantity > _inventory[i].StackLimit()) {
                    newItem.quantity = _inventory[i].quantity - _inventory[i].StackLimit(); // left over quantity
                    _inventory[i].quantity = _inventory[i].StackLimit(); // cap curr item to stackLimit

                } else {
                    // same item as curr index and total quantity under stack limit.
                    // aka, we stacked into an existing stack with non left over.
                    
                    return true;
                }
            }
        }

        return false;
    }

    void AddToFirstEmptySlot (InventoryItem item) {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] == null) {
                _inventory[i] = item;
                _currentHeldItems++;
                return;
            }
        }
    }


    void DropSelectedItem () { // Active drop. Gives it a velocity.
        // If null, no selected item.
        if (_inventory[_currentInventoryIndex] == null) {
            return;
        }

        int selectedItemId = _inventory[_currentInventoryIndex].itemID;
        string stringID = ItemManager.instance.itemEntries[selectedItemId].inventoryItemClass;
        int stackQuantity = _inventory[_currentInventoryIndex].quantity;
        float lastUsed = _inventory[_currentInventoryIndex].lastUsed;
        _inventory[_currentInventoryIndex] = null;

        Vector3 initVelocity = orientation.forward * 10;

        NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();

        ItemManager.instance.SpawnWorldItemServerRpc(
                                    selectedItemId, 
                                    stackQuantity, 
                                    lastUsed, 
                                    this.transform.position, 
                                    initVelocity,
                                    n_playerObj);

        _currentHeldItems--;
    }

    // Don't refactor this into DropSelectedItem. 
    // Order of events for velocity & spawn is important.
    void DropItem(int slot){

        // If null, no selected item.
        if (_inventory[slot] == null) {
            return;
        }

        int selectedItemId = _inventory[slot].itemID;
        string stringID = ItemManager.instance.itemEntries[slot].inventoryItemClass;
        int stackQuantity = _inventory[slot].quantity;
        float lastUsed = _inventory[slot].lastUsed;
        _inventory[slot] = null;

        NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();

        ItemManager.instance.SpawnWorldItemServerRpc(
                                    selectedItemId, 
                                    stackQuantity, 
                                    lastUsed, 
                                    this.transform.position, 
                                    new Vector3(0, 0, 0),
                                    n_playerObj);

        _currentHeldItems--;
    }

    // True if has weapon. False if does not have weapon. Default attack.
    public bool FireWeapon(){
        int weaponSlot = HasWeapon();
        if (weaponSlot == -1) {
            // Do default attack(?)
            Debug.Log("Attempting to FireWeapon(). Player has no weapon.");
            return false;
        }
        //_inventory[weaponSlot].fire(this.gameObject);
        return true;
    }

    public bool CanAutoFire(){
        int weaponSlot = HasWeapon();
        if (weaponSlot == -1){
            // Do default attack(?)
            Debug.Log("Attempting to CanAutoFire(). Player has no weapon.");
            return false;
        }
        //return _inventory[weaponSlot].CanAutoFire();
        return false;
    }
    
    // Helper function
    // Return -1 if no weapon. Otherwise return index of weapon.
    public int HasWeapon(){
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] != null && _inventory[i].IsWeapon()) {
                Debug.Log ("Hasweapon(). Found weapon at slot " + i);  
                return i;
            }
        }

        return -1;
    }


    void DEBUG_PRINT_INVENTORY() {
        string DEBUG_STRING = "DEBUG: Inventory: \n";
        for (int i = 0; i < _inventory.Length; i++) {
            InventoryItem item = _inventory[i];
            if (item != null) {
                string itemString = ItemManager.instance.itemEntries[item.itemID].inventoryItemClass;
                DEBUG_STRING += "Slot " + (i+1) + ": " + itemString + "(" + item.itemID + "): " + item.quantity + "x, ";
            }
        }

        Debug.Log(DEBUG_STRING);
    }

    void DEBUG_SELECT_SLOT() {
        string DEBUG_STRING = "DEBUG: Selected: \n";
        DEBUG_STRING += "Current Index: " + (_currentInventoryIndex+1) + ", ";
        DEBUG_STRING += "Item: ";
        if (_inventory[_currentInventoryIndex] != null) {
            int itemID = _inventory[_currentInventoryIndex].itemID;
            string itemString = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
            DEBUG_STRING += itemString + "(" + itemID + "): " + _inventory[_currentInventoryIndex].quantity + "x";
        } else {
            DEBUG_STRING += "none";
        }

        Debug.Log(DEBUG_STRING);
    }

}