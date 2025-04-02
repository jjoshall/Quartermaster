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
    // public KeyCode pickupKey = KeyCode.E;
    // public KeyCode dropItemKey = KeyCode.Q;
    // public KeyCode useItemKey = KeyCode.F;
    // public KeyCode selectItemOneKey = KeyCode.Alpha1;
    // public KeyCode selectItemTwoKey = KeyCode.Alpha2;
    // public KeyCode selectItemThreeKey = KeyCode.Alpha3;
    // public KeyCode selectItemFourKey = KeyCode.Alpha4;

    private UIManager _uiManager;


    [Header("Weapon Holdable Setup")]
    public GameObject weaponSlot;
    // public GameObject[] holdablePrefabs;
    public GameObject currentHoldable;

    // private InventoryItem[] _inventory;

    private GameObject[] _inventoryMono;
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
            _InputHandler.OnUse += PlayerInputHandlerUseEvent;
            _InputHandler.OnRelease += ReleaseItem;
            _InputHandler.OnInteract += PickUpClosest;
            _inventoryMono = new GameObject[_maxInventorySize];
            for (int i = 0; i < _maxInventorySize; i++) {
                _inventoryMono[i] = null;
            }
            UpdateAllInventoryUI();
            UpdateHeldItem();

            animator = _playerObj.GetComponentInChildren<Animator>();
        }
    }

    void Update() {
        if (IsOwner){
            MyInput();
            // if (_inventory[_currentInventoryIndex] != null)
            //     currentHeldItemId.Value = _inventory[_currentInventoryIndex].itemID;
            // else
            //     currentHeldItemId.Value = -1;
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

    // Abstracting away from isHeld bool. 
    // If isHeld is unnecessary in playerinputhandler, we can just call UseItem() and HeldItem() directly.
    void PlayerInputHandlerUseEvent(bool isHeld) {
        if (isHeld){
            HeldItem();
        } else {
            UseItem();
        }
    }

    #region UseItem
    #endregion
    void UseItem(){
        if (!IsOwner) return;
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;

        MonoItem itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>();
        if (itemComponent != null) {
            itemComponent.Use(_playerObj);
        } else {
            // Do nothing.
        }

        QuantityCheck();
        UpdateAllInventoryUI();
    }

    #region HeldItem
    #endregion
    void HeldItem(){
        if (!IsOwner) return;
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;

        MonoItem itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>();
        if (itemComponent != null) {
            itemComponent.Held(_playerObj);
        } else {
            // Do nothing.
        }
        // _inventory[_currentInventoryIndex].AttemptUse(_playerObj, isHeld);

        QuantityCheck();
        UpdateAllInventoryUI();
    }

    #region ReleaseItem
    #endregion
    void ReleaseItem(bool b) {
        if (!IsOwner) return;
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;

        // _inventory[_currentInventoryIndex].Release(_playerObj);
        MonoItem itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>();
        if (itemComponent != null) {
            itemComponent.Release(_playerObj);
        } else {
            // Do nothing.
        }

        QuantityCheck();
        UpdateAllInventoryUI();
    }

    void QuantityCheck(){
        if (_inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>().quantity <= 0) {
            _inventoryMono[_currentInventoryIndex] = null;
            _currentHeldItems--;
        }
    }

    #region PickUpEvents
    void PickUpClosest() {
        if (!IsOwner) return;

        GameObject pickedUp = ReturnClosestItem();

        // Try to stack the item in any existing item stacks
        if (pickedUp != null && 
            pickedUp.GetComponent<MonoItem>() != null &&
            TryStackItem(pickedUp)) {

                // TryStack returns true if fully stacked into existing stacks.
                CallPickUp(pickedUp.GetComponent<MonoItem>());
                Destroy(pickedUp);
                return;
        }

        // CHECK: INVENTORY FULL
        if (_currentHeldItems >= _maxInventorySize){
            return;
        }

        // update held item position locally.
        // every player has a local understanding of every other player's helditem, if exists, 
        // and the attached weaponSlot


        // CHECK: CURRENT SLOT EMPTY
        if (_inventoryMono[_currentInventoryIndex] == null) {
            // put in curr slot
            _inventoryMono[_currentInventoryIndex] = pickedUp;
            _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>().PickUp(_playerObj);

            _currentHeldItems++;
            return;
        } 

        // ELSE: ADD TO FIRST EMPTY SLOT
        AddToFirstEmptySlot(pickedUp);
    }

    GameObject ReturnClosestItem(){
        GameObject closestItem = _itemAcquisitionRange.
                                    GetComponent<ItemAcquisitionRange>().
                                    GetClosestItem(); // prioritizes raycast over physical closest.

        if (closestItem == null) {
            return null;
        }

        return closestItem;
    }
    #endregion

    #region PickupHelpers
    bool TryStackItem(GameObject newItem) {
        MonoItem newMono = newItem.GetComponent<MonoItem>();
        if (newMono == null) {
            Debug.LogError("Attempting to pick up Item without MonoItem component");
            return false;
        }

        // FOR ITEM IN INVENTORY
        for (int i = 0; i < _inventoryMono.Length; i++){

            // GET CURR
            GameObject curr = _inventoryMono[i];
            if (curr == null) continue;

            MonoItem currMono = curr.GetComponent<MonoItem>();

            // IF SAME ITEM, STACK NEW INTO CURR
            if (currMono.uniqueID == newMono.uniqueID) {

                int quantityBefore = currMono.quantity;
                currMono.quantity += newMono.quantity;

                // IF CURR STACK EXCEEDS STACKLIMIT, PUT EXCESS IN NEW
                if (currMono.quantity > currMono.StackLimit) {
                    newMono.quantity = currMono.quantity - currMono.StackLimit;
                    currMono.quantity = currMono.StackLimit;
                } else {
                    return true;
                }
            }
        }
        return false;
    }

    public int GetSlotQuantity (int slot) {
        if (_inventoryMono[slot] == null) return 0;
        return _inventoryMono[slot].GetComponent<MonoItem>().quantity;
    }

    void CallPickUp(MonoItem newItem) {
        newItem.PickUp(_playerObj);
    }

    bool AddToFirstEmptySlot(GameObject item) {
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] == null) {
                _inventoryMono[i] = item;
                _currentHeldItems++;
                item.GetComponent<MonoItem>().PickUp(_playerObj);
                return true;
            }
        }
        return false;
    }
    #endregion

    #region DropEvents
    void DropSelectedItem() {
        if (_inventoryMono[_currentInventoryIndex] == null) return;
        if (PauseMenuToggler.IsPaused) return;
        
        // int itemId = _inventory[_currentInventoryIndex].itemID;
        // float lastUsed = _inventory[_currentInventoryIndex].lastUsed;
        // NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();
        // Vector3 initVelocity = orientation.forward * GameManager.instance.DropItemVelocity;

        // if (_inventory[_currentInventoryIndex].quantity > 1) {
        //     _inventory[_currentInventoryIndex].quantity -= 1;
        //     ItemManager.instance.SpawnWorldItemServerRpc(
        //         itemId,
        //         1,
        //         lastUsed,
        //         this.transform.position,
        //         initVelocity,
        //         n_playerObj
        //     );
        // } else {
        //     int stackQuantity = _inventory[_currentInventoryIndex].quantity; 
        //     _inventory[_currentInventoryIndex].Drop(_playerObj);
        //     _inventory[_currentInventoryIndex] = null;
        //     ItemManager.instance.SpawnWorldItemServerRpc(
        //         itemId,
        //         stackQuantity,
        //         lastUsed,
        //         this.transform.position,
        //         initVelocity,
        //         n_playerObj
        //     );
        //     _currentHeldItems--;
        // }
        UpdateAllInventoryUI();
    }


    void DropItem(int slot) {
        // if (_inventory[slot] == null) return;
        // if (PauseMenuToggler.IsPaused) return;
        
        // int itemId = _inventory[slot].itemID;
        // float lastUsed = _inventory[slot].lastUsed;
        // NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();

        // if (_inventory[slot].quantity > 1) {
        //     _inventory[slot].quantity -= 1;
        //     ItemManager.instance.SpawnWorldItemServerRpc(
        //         itemId,
        //         1,
        //         lastUsed,
        //         this.transform.position,
        //         Vector3.zero,
        //         n_playerObj
        //     );
        // } else {
        //     int stackQuantity = _inventory[slot].quantity;
        //     _inventory[slot].Drop(_playerObj);
        //     _inventory[slot] = null;
        //     ItemManager.instance.SpawnWorldItemServerRpc(
        //         itemId,
        //         stackQuantity,
        //         lastUsed,
        //         this.transform.position,
        //         Vector3.zero,
        //         n_playerObj
        //     );
        //     _currentHeldItems--;
        // }
        UpdateAllInventoryUI();
    }

    #endregion

    public bool FireWeapon() {
        int weaponSlotIndex = HasWeapon();
        if (weaponSlotIndex == -1) {
            return false;
        }
        // IWeapon heldWeapon = _inventory[weaponSlotIndex] as IWeapon;
        // heldWeapon.fire(this.gameObject);
        return true;
    }

    // public bool CanAutoFire() {
    //     int weaponSlotIndex = HasWeapon();
    //     if (weaponSlotIndex == -1) {
    //         return false;
    //     }
    //     // IWeapon heldWeapon = _inventory[weaponSlotIndex] as IWeapon;
    //     // return heldWeapon.CanAutoFire();
    // }

    void UpdateWeaponCooldownUI() {
        if (!IsOwner || _uiManager == null) return;

        GameObject selectedItem = _inventoryMono[_currentInventoryIndex];

        if (selectedItem == null) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
            return;
        }

        float cooldownRemaining = selectedItem.GetComponent<MonoItem>().GetCooldownRemaining();
        float cooldownMax = selectedItem.GetComponent<MonoItem>().GetMaxCooldown();

        if (cooldownMax > 0) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(true);
            float cooldownRatio = Mathf.Clamp01(1 - (cooldownRemaining / cooldownMax));
            _uiManager.weaponCooldownRadial.fillAmount = Mathf.Lerp(0f, 0.25f, cooldownRatio);
        } else {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
        }
    }

    void UpdateHeldItem() {
        // int heldItemId = currentHeldItemId.Value;
        // if (heldItemId == -1) {
        //     if (currentHoldable != null) {
        //         Destroy(currentHoldable);
        //         currentHoldable = null;
        //         animator.SetBool("WeaponEquipped", false);
        //     }
        //     return;
        // }
        // if (currentHoldable != null) {
        //     HoldableIdentifer identifier = currentHoldable.GetComponent<HoldableIdentifer>();
        //     if (identifier != null && identifier.itemID == heldItemId) {
        //         return;
        //     } else {
        //         Destroy(currentHoldable);
        //         currentHoldable = null;
        //     }
        // }
        // if (heldItemId < holdablePrefabs.Length && holdablePrefabs[heldItemId] != null) {
        //     currentHoldable = Instantiate(holdablePrefabs[heldItemId], weaponSlot.transform);
        //     currentHoldable.transform.localPosition = Vector3.zero;
        //     currentHoldable.transform.localRotation = Quaternion.identity;
        //     HoldableIdentifer identifier = currentHoldable.GetComponent<HoldableIdentifer>();
        //     if (identifier != null) {
        //         identifier.itemID = heldItemId;
        //     }
        // }

        if (animator != null) {
            animator.SetBool("WeaponEquipped", true);
        }

    }

    private void UpdateAllInventoryUI() {
        for (int i = 0; i < _maxInventorySize; i++) {
            Texture textureToSet = _inventoryMono[i].GetComponent<MonoItem>().icon;;
            int quantity = _inventoryMono[i].GetComponent<MonoItem>().quantity;
            int stackLimit = _inventoryMono[i].GetComponent<MonoItem>().StackLimit;
            _uiManager.SetInventorySlotTexture(i, textureToSet);
            _uiManager.SetInventorySlotQuantity(i, quantity, stackLimit);
        }
    }


    #region Helpers
    private void DropAllOtherClassSpecs(string pickedSpec){
        // for (int i = 0; i < _inventoryMono.Length; i++){
        //     if (_inventoryMono[i] == null){
        //         continue;
        //     }
        //     if (_inventoryMono[i].GetComponent<MonoItem>().IsClassSpec()){
        //         string itemStr = InventoryItemToString(_inventory[i]);
        //         if (itemStr != pickedSpec){
        //             DropItem(i);
        //         }
        //     }
        // }
    }
    bool ValidIndexCheck(){
        if (_currentInventoryIndex < 0 || _currentInventoryIndex >= _inventoryMono.Length) {
            return false;
        }
        if (_inventoryMono[_currentInventoryIndex] == null) {
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
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] != null && _inventoryMono[i].GetComponent<MonoItem>().IsWeapon) {
                return i;
            }
        }
        return -1;
    }

    public int HasItem(string itemClass) {
        // for (int i = 0; i < _inventoryMono.Length; i++) {
        //     if (_inventoryMono[i] != null && InventoryItemToString(_inventoryMono[i].GetComponent<MonoItem>()) == itemClass) {
        //         return i;
        //     }
        // }
        return -1;
    }

    public string InventoryItemToString(InventoryItem item){
        if (item == null) return "null";
        return ItemManager.instance.itemEntries[item.itemID].inventoryItemClass;
    }
    #endregion
}
