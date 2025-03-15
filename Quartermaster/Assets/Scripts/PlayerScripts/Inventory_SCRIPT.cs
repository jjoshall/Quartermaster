using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerInputHandler))]
public class Inventory : NetworkBehaviour {
    private GameObject _playerObj;
    private GameObject _itemAcquisitionRange;

    private Animator animator;

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

    private InventoryItem[] _inventory;
    private int _currentInventoryIndex = 0;
    private int _oldInventoryIndex = 0;
    private int _currentHeldItems = 0;
    private int _maxInventorySize = 4;

    public NetworkVariable<int> currentHeldItemId = new NetworkVariable<int>(-1, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn(){
        _playerObj = this.gameObject;
        _itemAcquisitionRange = _playerObj.GetComponentInChildren<ItemAcquisitionRange>().gameObject;
        _uiManager = GameObject.Find("UI Manager").GetComponent<UIManager>();
        if (IsOwner){
            if (!_InputHandler) _InputHandler = _playerObj.GetComponent<PlayerInputHandler>();
            _InputHandler.OnUse += UseItem;
            _InputHandler.OnRelease += ReleaseItem;
            _InputHandler.OnInteract += PickUpClosest;
            _inventory = new InventoryItem[_maxInventorySize];
            for (int i = 0; i < _maxInventorySize; i++) {
                _inventory[i] = null;
            }
            UpdateAllInventoryUI();
            UpdateHeldItem();

            animator = _playerObj.GetComponentInChildren<Animator>();
        }
    }

    void Update() {
        if (IsOwner){
            MyInput();
            if (_inventory[_currentInventoryIndex] != null)
                currentHeldItemId.Value = _inventory[_currentInventoryIndex].itemID;
            else
                currentHeldItemId.Value = -1;
            UpdateWeaponCooldownUI();
        }
        UpdateHeldItem();
    }

    void MyInput() {
        if (!IsOwner) return;

        if (_InputHandler.isDropping) {
            DropSelectedItem();
        }
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
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;

        _inventory[_currentInventoryIndex].AttemptUse(_playerObj, isHeld);

        if (_inventory[_currentInventoryIndex].quantity <= 0) {
            _inventory[_currentInventoryIndex] = null;
            _currentHeldItems--;
        }
        UpdateAllInventoryUI();
    }

    void ReleaseItem(bool b) {
        if (!IsOwner) return;
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;

        _inventory[_currentInventoryIndex].Release(_playerObj);

        if (_inventory[_currentInventoryIndex].quantity <= 0) {
            _inventory[_currentInventoryIndex] = null;
            _currentHeldItems--;
        }
        UpdateAllInventoryUI();
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

        InventoryItem newItem = ItemManager.instance.SpawnInventoryItem(_playerObj, stringID, stackQuantity, lastUsed);

        if (TryStackItem(newItem)) {
            UpdateAllInventoryUI();
            _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>().RemoveItem(pickedUp);
            ItemManager.instance.DestroyWorldItemServerRpc(pickedUp.GetComponent<NetworkObject>());
            return;
        }
        if (newItem.quantity <= 0) {
            newItem = null;
            return;
        }

        if (newItem.IsWeapon()){
            int weaponSlotIndex = HasWeapon();
            if (weaponSlotIndex != -1) {
                DropItem(weaponSlotIndex);
            }
        }

        if (newItem.IsClassSpec()){
            DropAllOtherClassSpecs(InventoryItemToString(newItem));
        }

        if (_currentHeldItems < _maxInventorySize) {
            if (_inventory[_currentInventoryIndex] == null) {
                _inventory[_currentInventoryIndex] = newItem;
                _currentHeldItems++;
                CallPickUp(newItem);
            } else {
                bool success = AddToFirstEmptySlot(newItem);
                if (!success){
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
                return true;
            }
        }
        return false;
    }
    #endregion

    #region DropEvents
    void DropSelectedItem() {
        if (_inventory[_currentInventoryIndex] == null) return;
        if (PauseMenuToggler.IsPaused) return;
        
        int itemId = _inventory[_currentInventoryIndex].itemID;
        float lastUsed = _inventory[_currentInventoryIndex].lastUsed;
        NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();
        Vector3 initVelocity = orientation.forward * GameManager.instance.DropItemVelocity;

        if (_inventory[_currentInventoryIndex].quantity > 1) {
            _inventory[_currentInventoryIndex].quantity -= 1;
            ItemManager.instance.SpawnWorldItemServerRpc(
                itemId,
                1,
                lastUsed,
                this.transform.position,
                initVelocity,
                n_playerObj
            );
        } else {
            int stackQuantity = _inventory[_currentInventoryIndex].quantity; 
            _inventory[_currentInventoryIndex].Drop(_playerObj);
            _inventory[_currentInventoryIndex] = null;
            ItemManager.instance.SpawnWorldItemServerRpc(
                itemId,
                stackQuantity,
                lastUsed,
                this.transform.position,
                initVelocity,
                n_playerObj
            );
            _currentHeldItems--;
        }
        UpdateAllInventoryUI();
    }


    void DropItem(int slot) {
        if (_inventory[slot] == null) return;
        if (PauseMenuToggler.IsPaused) return;
        
        int itemId = _inventory[slot].itemID;
        float lastUsed = _inventory[slot].lastUsed;
        NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();

        if (_inventory[slot].quantity > 1) {
            _inventory[slot].quantity -= 1;
            ItemManager.instance.SpawnWorldItemServerRpc(
                itemId,
                1,
                lastUsed,
                this.transform.position,
                Vector3.zero,
                n_playerObj
            );
        } else {
            int stackQuantity = _inventory[slot].quantity;
            _inventory[slot].Drop(_playerObj);
            _inventory[slot] = null;
            ItemManager.instance.SpawnWorldItemServerRpc(
                itemId,
                stackQuantity,
                lastUsed,
                this.transform.position,
                Vector3.zero,
                n_playerObj
            );
            _currentHeldItems--;
        }
        UpdateAllInventoryUI();
    }

    #endregion

    public bool FireWeapon() {
        int weaponSlotIndex = HasWeapon();
        if (weaponSlotIndex == -1) {
            return false;
        }
        IWeapon heldWeapon = _inventory[weaponSlotIndex] as IWeapon;
        heldWeapon.fire(this.gameObject);
        return true;
    }

    public bool CanAutoFire() {
        int weaponSlotIndex = HasWeapon();
        if (weaponSlotIndex == -1) {
            return false;
        }
        IWeapon heldWeapon = _inventory[weaponSlotIndex] as IWeapon;
        return heldWeapon.CanAutoFire();
    }

    void UpdateWeaponCooldownUI() {
        if (!IsOwner || _uiManager == null) return;

        InventoryItem selectedItem = _inventory[_currentInventoryIndex];

        if (selectedItem == null) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
            return;
        }

        float cooldownRemaining = selectedItem.GetCooldownRemaining();
        float cooldownMax = selectedItem.GetMaxCooldown();

        if (cooldownMax > 0) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(true);
            float cooldownRatio = Mathf.Clamp01(1 - (cooldownRemaining / cooldownMax));
            _uiManager.weaponCooldownRadial.fillAmount = Mathf.Lerp(0f, 0.25f, cooldownRatio);
        } else {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
        }
    }

    void UpdateHeldItem() {
        int heldItemId = currentHeldItemId.Value;
        if (heldItemId == -1) {
            if (currentHoldable != null) {
                Destroy(currentHoldable);
                currentHoldable = null;
                animator.SetBool("WeaponEquipped", false);
            }
            return;
        }
        if (currentHoldable != null) {
            HoldableIdentifer identifier = currentHoldable.GetComponent<HoldableIdentifer>();
            if (identifier != null && identifier.itemID == heldItemId) {
                return;
            } else {
                Destroy(currentHoldable);
                currentHoldable = null;
            }
        }
        if (heldItemId < holdablePrefabs.Length && holdablePrefabs[heldItemId] != null) {
            currentHoldable = Instantiate(holdablePrefabs[heldItemId], weaponSlot.transform);
            currentHoldable.transform.localPosition = Vector3.zero;
            currentHoldable.transform.localRotation = Quaternion.identity;
            HoldableIdentifer identifier = currentHoldable.GetComponent<HoldableIdentifer>();
            if (identifier != null) {
                identifier.itemID = heldItemId;
            }
        }

        if (animator != null) {
            animator.SetBool("WeaponEquipped", true);
        }

    }

    private void UpdateAllInventoryUI() {
        for (int i = 0; i < _maxInventorySize; i++) {
            Texture textureToSet = emptyMaterial;
            int quantity = 0;
            int stackLimit = 1; // default for non-stackable items
            InventoryItem item = _inventory[i];
            if (item != null) {
                quantity = item.quantity;
                stackLimit = item.StackLimit();
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
            _uiManager.SetInventorySlotQuantity(i, quantity, stackLimit);
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
            }
        }
    }
    bool ValidIndexCheck(){
        if (_currentInventoryIndex < 0 || _currentInventoryIndex >= _inventory.Length) {
            return false;
        }
        if (_inventory[_currentInventoryIndex] == null) {
            return false;
        }
        if (_playerObj == null) {
            _playerObj = transform.parent.gameObject;
        }
        if (_playerObj == null) {
            return false;
        }
        return true;
    }
    public int HasWeapon() {
        for (int i = 0; i < _inventory.Length; i++) {
            if (_inventory[i] != null && _inventory[i].IsWeapon()) {
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
