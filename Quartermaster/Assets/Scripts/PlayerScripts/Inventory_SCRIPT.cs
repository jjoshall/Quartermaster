using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

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
    [SerializeField] public Texture grenadeMaterial;
    [SerializeField] public Texture slowTrapMaterial;
    [SerializeField] public Texture deliverableMaterial;
    [SerializeField] public Texture healSpecMaterial;
    [SerializeField] public Texture dmgSpecMaterial;

    [Header("Weapon Holdable Setup")]
    public GameObject weaponSlot;
    public GameObject[] holdablePrefabs;
    private GameObject currentHoldable;

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
        _InputHandler.OnRelease += ReleaseItem;
        _InputHandler.OnInteract += PickUpClosest;

        // Initialize inventory array
        _inventory = new InventoryItem[_maxInventorySize];
        for (int i = 0; i < _maxInventorySize; i++) {
            _inventory[i] = null;
        }

        UpdateAllInventoryUI();
        UpdateHeldItem(); // Ensure the weapon display is updated on spawn.
    }

    void Update() {
        MyInput();
        UpdateWeaponCooldownUI();
        UpdateHeldItem(); // Continuously update based on current selection.
    }

    void MyInput() {
        if (!IsOwner) return;

        if (_InputHandler.isDropping) {
            DropSelectedItem();
        }
        // Map the raw input index to a valid inventory index.
        _currentInventoryIndex = Mathf.Clamp(_InputHandler.inventoryIndex, 0, _maxInventorySize - 1);

        if (_currentInventoryIndex != _oldInventoryIndex) {
            UpdateAllInventoryUI();
            _oldInventoryIndex = _currentInventoryIndex;
        }
        _uiManager.HighlightSlot(_currentInventoryIndex);
    }

    #region UseEvents
    void UseItem(bool isHeld) {
        if (!IsOwner) return;
        if (!ValidIndexCheck()) return;
        _inventory[_currentInventoryIndex].AttemptUse(_playerObj, isHeld);
        if (_inventory[_currentInventoryIndex].quantity <= 0) {
            _inventory[_currentInventoryIndex] = null;
            _currentHeldItems--;
            UpdateAllInventoryUI();
        }
    }

    void ReleaseItem(bool b){
        if (!IsOwner) return;
        if (!ValidIndexCheck()) return;
        _inventory[_currentInventoryIndex].Release(_playerObj);
        if (_inventory[_currentInventoryIndex].quantity <= 0) {
            _inventory[_currentInventoryIndex] = null;
            _currentHeldItems--;
            UpdateAllInventoryUI();
        }
    }
    #endregion

    #region PickUpEvents
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

        Debug.Log("Spawning a new inventoryItem for pickup: " + stringID);
        InventoryItem newItem = ItemManager.instance.SpawnInventoryItem(_playerObj, stringID, stackQuantity, lastUsed);

        // If stackable, try to stack.
        if (TryStackItem(newItem)) {
            Debug.Log("Stacked the item");
            _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().RemoveItem(pickedUp);
            ItemManager.instance.DestroyWorldItemServerRpc(pickedUp.GetComponent<NetworkObject>());
            return;
        }
        if (newItem.quantity <= 0) {
            Debug.Log("Item quantity is 0 or less. Deallocating it");
            newItem = null;
            return;
        }

        // If it's a weapon, check if one already exists.
        if (newItem.IsWeapon()){
            int weaponSlotIndex = HasWeapon();
            if (weaponSlotIndex != -1) {
                Debug.Log("We have a weapon at slot " + weaponSlotIndex + ". Dropping it.");
                DropItem(weaponSlotIndex);
            }
        }

        if (newItem.IsClassSpec()){
            Debug.Log ("Class Spec picked");
            DropAllOtherClassSpecs(InventoryItemToString(newItem));
        }

        // Add to the first empty slot.
        if (_currentHeldItems < _maxInventorySize) {
            if (_inventory[_currentInventoryIndex] == null) {
                _inventory[_currentInventoryIndex] = newItem;
                _currentHeldItems++;
                CallPickUp(newItem);
            } else {
                bool success = AddToFirstEmptySlot(newItem);
                if (!success){
                    Debug.Log("No empty slots available.");
                    // destroy newItem
                    newItem = null;
                }
            }
            UpdateAllInventoryUI();
            _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().RemoveItem(pickedUp);
            ItemManager.instance.DestroyWorldItemServerRpc(pickedUp.GetComponent<NetworkObject>());

        }
    }
    #endregion

    #region PickupHelpers
    // Tries to stack newItem onto an existing stack. Returns true if successful.
    bool TryStackItem(InventoryItem newItem) {
        for (int i = 0; i < _inventory.Length; i++){
            if (_inventory[i] == null) continue;
            if (_inventory[i].itemID == newItem.itemID) {
                int quantityBefore = _inventory[i].quantity;
                _inventory[i].quantity += newItem.quantity;
                if (_inventory[i].quantity > _inventory[i].StackLimit()) {
                    newItem.quantity = _inventory[i].quantity - _inventory[i].StackLimit();
                    _inventory[i].quantity = _inventory[i].StackLimit();
                    CallPickUp(newItem);
                } else {
                    CallPickUp(newItem);
                    return true;
                }
            }
        }
        return false;
    }

    public int GetSlotQuantity (int slot) {
        if (_inventory[slot] == null) return 0;
        return _inventory[slot].quantity;
    }

    void CallPickUp(InventoryItem newItem) {
        newItem.PickUp(_playerObj);
    }

    bool AddToFirstEmptySlot(InventoryItem item) {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] == null) {
                _inventory[i] = item;
                _currentHeldItems++;
                CallPickUp(item);
                Debug.Log($"Item {item.GetType().Name} added to slot {i}");
                return true;
            }
        }
        return false;
    }
    #endregion

    #region DropEvents
    void DropSelectedItem() {
        if (_inventory[_currentInventoryIndex] == null) return;
        int selectedItemId = _inventory[_currentInventoryIndex].itemID;
        string stringID = ItemManager.instance.itemEntries[selectedItemId].inventoryItemClass;
        int stackQuantity = _inventory[_currentInventoryIndex].quantity;
        float lastUsed = _inventory[_currentInventoryIndex].lastUsed;

        _inventory[_currentInventoryIndex].quantity = 0; // set quantity to 0 so drop logic counts as 0.
        _inventory[_currentInventoryIndex].Drop(_playerObj); // call any logic that needs to happen on drop
        
        _inventory[_currentInventoryIndex] = null;
        Vector3 initVelocity = orientation.forward * GameManager.instance.DropItemVelocity;
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
        string stringID = ItemManager.instance.itemEntries[selectedItemId].inventoryItemClass;
        int stackQuantity = _inventory[slot].quantity;
        float lastUsed = _inventory[slot].lastUsed;

        _inventory[slot].quantity = 0; // set quantity to 0 so drop logic counts as 0.
        _inventory[slot].Drop(_playerObj); // call any logic that needs to happen on drop

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
    #endregion

    // When is this used?
    public bool FireWeapon() {
        int weaponSlotIndex = HasWeapon();
        if (weaponSlotIndex == -1) {
            Debug.Log("Attempting to FireWeapon(). Player has no weapon.");
            return false;
        }
        IWeapon heldWeapon = _inventory[weaponSlotIndex] as IWeapon;
        heldWeapon.fire(this.gameObject);
        return true;
    }

    public bool CanAutoFire() {
        int weaponSlotIndex = HasWeapon();
        if (weaponSlotIndex == -1) {
            Debug.Log("Attempting to CanAutoFire(). Player has no weapon.");
            return false;
        }
        IWeapon heldWeapon = _inventory[weaponSlotIndex] as IWeapon;
        return heldWeapon.CanAutoFire();
    }

    void UpdateWeaponCooldownUI() {
        if (!IsOwner || _uiManager == null) return;

        InventoryItem selectedItem = _inventory[_currentInventoryIndex];

        // deprecated: !selectedItem.IsWeapon()
        if (selectedItem == null) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
            return;
        }

        InventoryItem heldItem = selectedItem;
        if (heldItem != null) {
            float cooldownRemaining = heldItem.GetCooldownRemaining();
            float cooldownMax = heldItem.GetMaxCooldown();

            if (cooldownMax > 0) {
                _uiManager.weaponCooldownRadial.gameObject.SetActive(true);
                float cooldownRatio = Mathf.Clamp01(1 - (cooldownRemaining / cooldownMax));
                _uiManager.weaponCooldownRadial.fillAmount = Mathf.Lerp(0f, 0.25f, cooldownRatio);
            } else {
                _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
            }
        }
    }

    void UpdateHeldItem() {
        InventoryItem selectedItem = _inventory[_currentInventoryIndex];

        if (selectedItem != null) {
            // If a holdable is already spawned, check if it matches the current item.
            if (currentHoldable != null) {
                WeaponIdentifier identifier = currentHoldable.GetComponent<WeaponIdentifier>();
                if (identifier != null && identifier.itemID == selectedItem.itemID) {
                    return; // Correct item already displayed.
                } else {
                    Destroy(currentHoldable);
                    currentHoldable = null;
                }
            }
            
            // Instantiate the corresponding prefab if it exists.
            if (selectedItem.itemID < holdablePrefabs.Length && holdablePrefabs[selectedItem.itemID] != null) {
                currentHoldable = Instantiate(holdablePrefabs[selectedItem.itemID], weaponSlot.transform);
                currentHoldable.transform.localPosition = Vector3.zero;
                currentHoldable.transform.localRotation = Quaternion.identity;
                
                // Optionally assign the itemID to the spawned holdable.
                WeaponIdentifier identifier = currentHoldable.GetComponent<WeaponIdentifier>();
                if (identifier != null) {
                    identifier.itemID = selectedItem.itemID;
                }
            }
        } else {
            // No item selected: remove any spawned holdable.
            if (currentHoldable != null) {
                Destroy(currentHoldable);
                currentHoldable = null;
            }
        }
    }

    // Updates all inventory UI slots.
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
                    case 5:
                        textureToSet = deliverableMaterial;
                        break;
                    case 6:
                        textureToSet = grenadeMaterial;
                        break;  
                    case 7:
                        textureToSet = slowTrapMaterial;
                        break;
                    case 9:
                        textureToSet = healSpecMaterial;
                        break;
                    case 10:
                        textureToSet = dmgSpecMaterial;
                        break;
                    default:
                        textureToSet = emptyMaterial;
                        break;
                }
            }
            _uiManager.SetInventorySlotTexture(i, textureToSet);
        }
    }

    #region Helpers
    private void DropAllOtherClassSpecs(string pickedSpec){
        for (int i = 0; i < _inventory.Length; i++){
            if (_inventory[i] == null){
                continue;
            }
            if (_inventory[i].IsClassSpec()){
                string itemStr = InventoryItemToString(_inventory[i]);
                if (itemStr != pickedSpec){
                    DropItem(i);
                }
                Debug.Log ("Dropped " + itemStr);
            }
        }

    }
    bool ValidIndexCheck(){
        if (_currentInventoryIndex < 0 || _currentInventoryIndex >= _inventory.Length) {
            // Debug.LogError("Invalid inventory index: " + _currentInventoryIndex);
            return false;
        }
        if (_inventory[_currentInventoryIndex] == null) {
            // Debug.Log("No item in inventory slot " + _currentInventoryIndex);
            return false;
        }
        if (_playerObj == null) {
            _playerObj = transform.parent.gameObject;
        }
        if (_playerObj == null) {
            // Debug.LogError("No player object found.");
            return false;
        }
        return true;
    }
    public int HasWeapon() {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] != null && _inventory[i].IsWeapon()) {
                // Debug.Log("HasWeapon(): Found weapon at slot " + i);
                return i;
            }
        }
        return -1;
    }

    public int HasItem(string itemClass) {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] != null && InventoryItemToString(_inventory[i]) == itemClass) {
                return i;
            }
        }
        return -1;
    }

    public string InventoryItemToString(InventoryItem item){
        if (item == null) return "null";
        return ItemManager.instance.itemEntries[item.itemID].inventoryItemClass;
    }
    #endregion
}
