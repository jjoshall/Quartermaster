using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInputHandler))]
public class Inventory : NetworkBehaviour {
    private GameObject _playerObj;
    private GameObject _itemAcquisitionRange;

    [Header("Orientation for dropItem direction")]
    public Transform orientation;

    [Header("Inventory Keybinds")]
    private PlayerInputHandler _InputHandler;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropItemKey = KeyCode.Q;
    public KeyCode useItemKey = KeyCode.F;
    public KeyCode selectItemOneKey = KeyCode.Alpha1;
    public KeyCode selectItemTwoKey = KeyCode.Alpha2;
    public KeyCode selectItemThreeKey = KeyCode.Alpha3;
    public KeyCode selectItemFourKey = KeyCode.Alpha4;

    private UIManager _uiManager;

    [Header("Item Materials")]
    [SerializeField] public Texture medkitMaterial;
    [SerializeField] public Texture keyMaterial;
    [SerializeField] public Texture pistolMaterial;
    [SerializeField] public Texture emptyMaterial;
    [SerializeField] public Texture railgunMaterial;
    [SerializeField] public Texture flamethrowerMaterial;

    private InventoryItem[] _inventory;  // Inventory array
    private int _currentInventoryIndex = 0; // Currently selected slot (0-based)
    private int _oldInventoryIndex = 0;
    private int _currentHeldItems = 0;
    private int _maxInventorySize = 4;

    public override void OnNetworkSpawn(){
        if (!IsOwner) {
            this.enabled = false;
            return;
        }
        
        _uiManager = GameObject.Find("UI Manager").GetComponent<UIManager>();
        _playerObj = this.gameObject;
        _itemAcquisitionRange = _playerObj.GetComponentInChildren<ItemAcquisitionRange>().gameObject;
        if (!_InputHandler) _InputHandler = _playerObj.GetComponent<PlayerInputHandler>();

        _InputHandler.OnUse += UseItem;
        _InputHandler.OnInteract += PickUpClosest;

        // Initialize inventory array
        _inventory = new InventoryItem[_maxInventorySize];
        for (int i = 0; i < _maxInventorySize; i++) {
            _inventory[i] = null;
        }

        UpdateAllInventoryUI();
    }

    void Update() {
        MyInput();
    }

    void MyInput() {
        if (!IsOwner) return;

        if (_InputHandler.isDropping) {
            DropSelectedItem();
        }
        // Use the inventory index directly (assumed 0-based now)
        _currentInventoryIndex = Mathf.Clamp(_InputHandler.inventoryIndex, 0, _maxInventorySize - 1);
        Debug.Log("Mapped _currentInventoryIndex: " + _currentInventoryIndex);

        if (_currentInventoryIndex != _oldInventoryIndex) {
            UpdateAllInventoryUI();
            _oldInventoryIndex = _currentInventoryIndex;
        }
        Debug.Log("Raw inventoryIndex from input handler: " + _InputHandler.inventoryIndex);
        _uiManager.HighlightSlot(_currentInventoryIndex);
    }

    void UseItem(bool isHeld) {
        if (!IsOwner) return;
        if (_currentInventoryIndex < 0 || _currentInventoryIndex >= _inventory.Length) {
            Debug.LogError("Invalid inventory index: " + _currentInventoryIndex);
            return;
        }
        if (_inventory[_currentInventoryIndex] != null) {
            if (_playerObj == null) {
                _playerObj = transform.parent.gameObject;
            }
            _inventory[_currentInventoryIndex].AttemptUse(_playerObj, isHeld);
            if (_inventory[_currentInventoryIndex].quantity <= 0) {
                _inventory[_currentInventoryIndex] = null;
                _currentHeldItems--;
                UpdateAllInventoryUI();
            }
        }
    }

    void PickUpClosest(bool discardBool) {
        if (!IsOwner) return;
        GameObject closestItem = _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().GetClosestItem();
        if (closestItem != null) {
            PickUpItem(closestItem);
        }
    }

    void PickUpItem(GameObject pickedUp) {
        if (!IsOwner) return;

        int itemID = pickedUp.GetComponent<WorldItem>().GetItemID();
        string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (stringID == "PocketInventoryPortalKey") {
            if (PocketInventory.instance.PlayerIsInPocket(_playerObj.GetComponent<NetworkObject>())) {
                PocketInventory.instance.clearDroppedKeyServerRpc();
            }
        }

        int stackQuantity = pickedUp.GetComponent<WorldItem>().GetStackQuantity();
        float lastUsed = pickedUp.GetComponent<WorldItem>().GetLastUsed();

        _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().RemoveItem(pickedUp);
        ItemManager.instance.DestroyWorldItemServerRpc(pickedUp.GetComponent<NetworkObject>());

        Debug.Log("Spawning a new inventoryItem for pickup: " + stringID);
        InventoryItem newItem = ItemManager.instance.SpawnInventoryItem(stringID, stackQuantity, lastUsed);

        // If stackable, try to stack.
        if (TryStackItem(newItem)) {
            Debug.Log("Stacked the item");
            return;
        }
        if (newItem.quantity <= 0) return;

        // If it's a weapon, check if one already exists.
        if (newItem.IsWeapon()){
            int weaponSlot = HasWeapon();
            if (weaponSlot != -1) {
                Debug.Log("We have a weapon at slot " + weaponSlot + ". Dropping it.");
                DropItem(weaponSlot);
            }
        }

        // Add to the first empty slot.
        if (_currentHeldItems < _maxInventorySize) {
            if (_inventory[_currentInventoryIndex] == null) {
                _inventory[_currentInventoryIndex] = newItem;
                _currentHeldItems++;
                UpdateAllInventoryUI();
                return;
            }
            AddToFirstEmptySlot(newItem);
            UpdateAllInventoryUI();
        }
    }

    // Tries to stack newItem onto an existing stack. Returns true if stacked.
    bool TryStackItem(InventoryItem newItem) {
        for (int i = 0; i < _inventory.Length; i++){
            if (_inventory[i] == null) continue;
            if (_inventory[i].itemID == newItem.itemID) {
                _inventory[i].quantity += newItem.quantity;
                if (_inventory[i].quantity > _inventory[i].StackLimit()) {
                    newItem.quantity = _inventory[i].quantity - _inventory[i].StackLimit();
                    _inventory[i].quantity = _inventory[i].StackLimit();
                } else {
                    return true;
                }
            }
        }
        return false;
    }

    void AddToFirstEmptySlot(InventoryItem item) {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] == null) {
                _inventory[i] = item;
                _currentHeldItems++;
                Debug.Log($"Item {item.GetType().Name} added to slot {i}");
                return;
            }
        }
    }

    void DropSelectedItem() {
        if (_inventory[_currentInventoryIndex] == null) return;
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
        UpdateAllInventoryUI();
    }

    void DropItem(int slot) {
        if (_inventory[slot] == null) return;
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
            Vector3.zero,
            n_playerObj);

        _currentHeldItems--;
        UpdateAllInventoryUI();
    }

    public int HasWeapon() {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] != null && _inventory[i].IsWeapon()) {
                Debug.Log("HasWeapon(): Found weapon at slot " + i);
                return i;
            }
        }
        return -1;
    }

    public bool FireWeapon() {
        int weaponSlot = HasWeapon();
        if (weaponSlot == -1) {
            Debug.Log("Attempting to FireWeapon(). Player has no weapon.");
            return false;
        }
        IWeapon heldWeapon = _inventory[weaponSlot] as IWeapon;
        heldWeapon.fire(this.gameObject);
        return true;
    }

    public bool CanAutoFire() {
        int weaponSlot = HasWeapon();
        if (weaponSlot == -1) {
            Debug.Log("Attempting to CanAutoFire(). Player has no weapon.");
            return false;
        }
        IWeapon heldWeapon = _inventory[weaponSlot] as IWeapon;
        return heldWeapon.CanAutoFire();
    }

    // Updates all inventory UI slots by looping through the inventory array.
    private void UpdateAllInventoryUI() {
        for (int i = 0; i < _maxInventorySize; i++) {
            Texture textureToSet = emptyMaterial;
            InventoryItem item = _inventory[i];
            if (item != null) {
                switch (item.itemID) {
                    case 0:
                        textureToSet = keyMaterial;
                        break;
                    case 1:
                        textureToSet = medkitMaterial;
                        break;
                    case 2:
                        textureToSet = pistolMaterial;
                        break;
                    case 3:
                        textureToSet = railgunMaterial;
                        break;
                    case 4:
                        textureToSet = flamethrowerMaterial;
                        break;
                    default:
                        textureToSet = emptyMaterial;
                        break;
                }
            }
            _uiManager.SetInventorySlotTexture(i, textureToSet);
        }
    }
}