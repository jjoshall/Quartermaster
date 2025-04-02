using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Add this for NetworkTransform
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
    // public GameObject currentHoldable;

    // private InventoryItem[] _inventory;

    private GameObject[] _inventoryMono;
    private int _currentInventoryIndex = 0;
    private int _oldInventoryIndex = 0;
    private int _currentHeldItems = 0;
    private int _maxInventorySize = 4;

    // public NetworkVariable<int> currentHeldItemId = new NetworkVariable<int>(-1, 
    //     NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
            itemComponent.ButtonUse(_playerObj);
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
            itemComponent.ButtonHeld(_playerObj);
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
            itemComponent.ButtonRelease(_playerObj);
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

        PropagateItemAttachmentServerRpc(pickedUp.GetComponent<NetworkObject>(), _playerObj.GetComponent<NetworkObject>());
        pickedUp.GetComponent<MonoItem>().IsPickedUp = true; // prevent items in inventory from being picked up
        pickedUp.GetComponent<MonoItem>().attachedWeaponSlot = weaponSlot; // local. 
        pickedUp.GetComponent<MonoItem>().userRef = _playerObj; // local.

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
    void DropSelectedItem() { // trigger on hotkey
        if (!IsOwner) return;
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;

        // Drop the item in the current slot
        DropItem(_currentInventoryIndex);
    }

    void DropItem(int slot) { // called directly to drop additional ClassSpecs / Weapons
        if (!IsOwner) return;
        if (_inventoryMono[slot] == null) return;
        if (PauseMenuToggler.IsPaused) return;
        if (!ValidIndexCheck()) return;
        
        // int itemId = _inventory[_currentInventoryIndex].itemID;
        // float lastUsed = _inventory[_currentInventoryIndex].lastUsed;
        NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();
        Vector3 initVelocity = orientation.forward * GameManager.instance.DropItemVelocity;

        // detach the current held item
        PropagateItemDetachServerRpc(_inventoryMono[_currentInventoryIndex].GetComponent<NetworkObject>(), n_playerObj);
        
        // Give it velocity
        Rigidbody rb = _inventoryMono[_currentInventoryIndex].GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = initVelocity;
        }

        _inventoryMono[slot].GetComponent<MonoItem>().Drop(_playerObj); // Call the item's onDrop function

        _inventoryMono[slot] = null;
        _currentHeldItems--;

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

        if (_inventoryMono[_currentInventoryIndex] == null){
            if (animator) 
                animator.SetBool("WeaponEquipped", false);
            return;
        }
        if (animator)
            animator.SetBool("WeaponEquipped", true);

    }

    private void UpdateAllInventoryUI() {
        for (int i = 0; i < _maxInventorySize; i++) {
            if (_inventoryMono[i] == null || _inventoryMono[i].GetComponent<MonoItem>() == null) {
                _uiManager.SetInventorySlotTexture(i, null);
                _uiManager.SetInventorySlotQuantity(i, 0, 0);
                continue;
            }
            Texture textureToSet = _inventoryMono[i].GetComponent<MonoItem>().icon;
            int quantity = _inventoryMono[i].GetComponent<MonoItem>().quantity;
            int stackLimit = _inventoryMono[i].GetComponent<MonoItem>().StackLimit;
            _uiManager.SetInventorySlotTexture(i, textureToSet);
            _uiManager.SetInventorySlotQuantity(i, quantity, stackLimit);
        }
    }

    #region HoldingItem
    [ServerRpc(RequireOwnership = false)]
    private void PropagateItemAttachmentServerRpc(NetworkObjectReference item, NetworkObjectReference n_player){
        if (!IsServer) return;
        // Attach the item to the weapon slot on the server
        AttachItemClientRpc(item, n_player);
    }

    [ClientRpc]
    private void AttachItemClientRpc(NetworkObjectReference itemRef, NetworkObjectReference n_playerRef){
        // Get the item and weapon slot GameObjects
        NetworkObject n_item = itemRef.TryGet(out NetworkObject itemObj) ? itemObj : null;
        GameObject item = n_item != null ? n_item.gameObject : null;
        NetworkObject n_player = n_playerRef.TryGet(out NetworkObject weaponSlotObj) ? weaponSlotObj : null;
        GameObject player = n_player != null ? weaponSlotObj.gameObject : null;
        
        if (!player || !item){
            return;
        }

        GameObject weaponSlot = player.GetComponent<Inventory>().weaponSlot;

        if (!weaponSlot){
            return;
        }
        
        // toggle OFF networktransform
        item.GetComponent<NetworkTransform>().enabled = false;
        // item.transform.SetParent(weaponSlot.transform, worldPositionStays: false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // freeze rigidbody position / rotation
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null){
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void PropagateItemDetachServerRpc(NetworkObjectReference item, NetworkObjectReference n_player){
        if (!IsServer) return;
        // Attach the item to the weapon slot on the server
        DetachItemClientRpc(item, n_player);
    }
    [ClientRpc]
    private void DetachItemClientRpc(NetworkObjectReference itemRef, NetworkObjectReference n_playerRef){
        // Get the item and weapon slot GameObjects
        NetworkObject n_item = itemRef.TryGet(out NetworkObject itemObj) ? itemObj : null;
        GameObject item = n_item != null ? n_item.gameObject : null;
        NetworkObject n_player = n_playerRef.TryGet(out NetworkObject weaponSlotObj) ? weaponSlotObj : null;
        GameObject player = n_player != null ? weaponSlotObj.gameObject : null;

        if (!player || !item){
            return;
        }

        item.GetComponent<NetworkTransform>().enabled = true;
        item.GetComponent<MonoItem>().IsPickedUp = false; // prevent items in inventory from being picked up
        item.GetComponent<MonoItem>().attachedWeaponSlot = null; // local.
        item.GetComponent<MonoItem>().userRef = null; // local.


    }

    void FreezeRigidbody(GameObject obj){
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null){
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

    }
    void UnfreezeRigidbody(GameObject obj){
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null){
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }
    }
    #endregion


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
