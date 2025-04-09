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

    private UIManager _uiManager;


    [Header("Weapon Holdable Setup")]
    public GameObject weaponSlot;

    public NetworkVariable<NetworkObjectReference> n_currentHoldable = new NetworkVariable<NetworkObjectReference>(
                                                                                default, 
                                                                                NetworkVariableReadPermission.Everyone, 
                                                                                NetworkVariableWritePermission.Owner);
    public NetworkVariable<NetworkObjectReference> n_prevHoldable = new NetworkVariable<NetworkObjectReference>();

    private GameObject[] _inventoryMono;
    private int _currentInventoryIndex = 0;
    private int _oldInventoryIndex = 0; 
    private int _currentHeldItems = 0;
    private int _maxInventorySize = 4;

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

            // Subscribe to network variable changes so that updates propagate immediately.
            n_currentHoldable.OnValueChanged += (oldVal, newVal) => {
                UpdateHeldItem();
            };
        }
    }

    void Update() {
        if (IsOwner){
            MyInput();
            UpdateWeaponCooldownUI();
        }
    }

    void MyInput() {
        if (!IsOwner) return;

        // Handle drop input first.
        if (_InputHandler.isDropping) {
            DropSelectedItem();
        }

        // Clamp the inventory index to a valid range.
        _currentInventoryIndex = Mathf.Clamp(_InputHandler.inventoryIndex, 0, _maxInventorySize - 1);

        // If the selection has changed, update the UI and the networked current holdable.
        if (_currentInventoryIndex != _oldInventoryIndex) {
            UpdateAllInventoryUI();
            _oldInventoryIndex = _currentInventoryIndex;

            UpdateHoldableNetworkReference();
        }

        // Update the UI highlight for the current slot.
        _uiManager.HighlightSlot(_currentInventoryIndex);
    }

        // Helper method to update the network variable based on the current index.
    void UpdateHoldableNetworkReference(){
        GameObject selectedItem = _inventoryMono[_currentInventoryIndex];
        if (selectedItem != null) {
            NetworkObject netObj = selectedItem.GetComponent<NetworkObject>();
            n_currentHoldable.Value = netObj != null ? netObj : default;
        } else {
            n_currentHoldable.Value = default;
        }
    }

    // -------------------------------------------------------------------------------------------------------------------------
    #region PlayerItemUsageEvents
    #endregion

    // PlayerInputHandler uses same event and a Held bool to distinguish OnPress and OnHold.
    // This function is to split the two events.
    void PlayerInputHandlerUseEvent(bool isHeld) {
        if (isHeld){
            HeldItem();
        } else {
            UseItem();
        }
    }

    void UseItem(){
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;

        MonoItem itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>();
        if (itemComponent != null) {
            itemComponent.ButtonUse(_playerObj);
        } else {
            // Do nothing.
        }

        QuantityCheck(); 
        UpdateAllInventoryUI();
    }

    void HeldItem(){
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;

        MonoItem itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>();
        if (itemComponent != null) {
            itemComponent.ButtonHeld(_playerObj);
        } else {
            // Do nothing.
        }
        QuantityCheck();
        UpdateAllInventoryUI();
    }

    void ReleaseItem(bool b) {
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;

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


    // -------------------------------------------------------------------------------------------------------------------------
    #region PickUpEvents
    #endregion

    void PickUpClosest() {
        if (!IsOwner) return;

        GameObject pickedUp = ReturnClosestItem();  

        // Try to stack the item in any existing item stacks
        if (pickedUp != null && 
            pickedUp.GetComponent<MonoItem>() != null &&
            TryStackItem(pickedUp)) {

                // TryStack returns true if fully stacked into existing stacks.
                pickedUp.GetComponent<MonoItem>().PickUp(_playerObj); // Call the item's onPickUp function
                Destroy(pickedUp);
                return;
        }

        // CHECK: INVENTORY FULL
        if (_currentHeldItems >= _maxInventorySize){
            return;
        }

        AddToInventory(pickedUp); 
    }

    private void AddToInventory(GameObject pickedUp){

        // Locally attach the item to the player on each client
        PropagateItemAttachmentServerRpc(pickedUp.GetComponent<NetworkObject>(), _playerObj.GetComponent<NetworkObject>());

        pickedUp.GetComponent<MonoItem>().userRef = _playerObj; // local.

        // CHECK: CURRENT SLOT EMPTY
        if (_inventoryMono[_currentInventoryIndex] == null) {
            // put in curr slot
            _inventoryMono[_currentInventoryIndex] = pickedUp;
            _inventoryMono[_currentInventoryIndex].GetComponent<MonoItem>().PickUp(_playerObj);

            _currentHeldItems++;
            UpdateAllInventoryUI();
            UpdateHoldableNetworkReference();   // updates network var for current item
            UpdateHeldItem();                   // show currSlot, hides others
            return;
        } else {
            // ELSE: ADD TO FIRST EMPTY SLOT
            AddToFirstEmptySlot(pickedUp); 

            _currentHeldItems++;
            UpdateAllInventoryUI();
            UpdateHeldItem();
        }

    }


    // -------------------------------------------------------------------------------------------------------------------------
    #region PickupHelpers
    #endregion 

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

    public int GetItemQuantity (string uniqueID) {
        int total = 0;
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] != null && _inventoryMono[i].GetComponent<MonoItem>().uniqueID == uniqueID) {
                total += _inventoryMono[i].GetComponent<MonoItem>().quantity;
            }
        }
        return total;
    }

    bool AddToFirstEmptySlot(GameObject item) {
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] == null) {
                _inventoryMono[i] = item;
                item.GetComponent<MonoItem>().PickUp(_playerObj);
                return true;
            }
        }
        return false;
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

    
    // -------------------------------------------------------------------------------------------------------------------------
    #region DropEvents
    #endregion

    void DropSelectedItem() { // trigger on hotkey. calls DropItem on current slot.
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;

        DropItem(_currentInventoryIndex);
    }

    void DropItem(int slot) { // called directly to drop additional ClassSpecs / Weapons
        if (_inventoryMono[slot] == null) return;
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;
        
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

    // -------------------------------------------------------------------------------------------------------------------------
    #region UIUpdate
    #endregion

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
        for (int i =0; i < _inventoryMono.Length; i++){
            if (_inventoryMono[i] == null){
                continue;
            }
            if (i != _currentInventoryIndex){
                NetworkObject n_item = _inventoryMono[i].GetComponent<NetworkObject>();
                PropagateHoldableHideServerRpc(n_item);
            } else {
                NetworkObject n_item = _inventoryMono[i].GetComponent<NetworkObject>();
                PropagateHoldableShowServerRpc(n_item);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PropagateHoldableShowServerRpc(NetworkObjectReference item){
        if (!IsServer) return;
        // Attach the item to the weapon slot on the server
        PropagateHoldableShowClientRpc(item);
    }
    [ClientRpc]
    private void PropagateHoldableShowClientRpc(NetworkObjectReference itemRef){
        // Get the item and weapon slot GameObjects
        NetworkObject n_item = itemRef.TryGet(out NetworkObject itemObj) ? itemObj : null;
        GameObject item = n_item != null ? n_item.gameObject : null;
        if (item == null) return;

        // Show the item in the weapon slot
        foreach (Renderer r in item.GetComponentsInChildren<Renderer>()) {
            r.enabled = true;
        }

        if (animator){
            animator.SetBool("WeaponEquipped", true);
        } else {
            Debug.LogWarning("Animator is null. Cannot set WeaponEquipped parameter.");
        }
    }

    //PropagateHoldableHide
    [ServerRpc(RequireOwnership = false)]
    private void PropagateHoldableHideServerRpc(NetworkObjectReference item){
        if (!IsServer) return;
        // Attach the item to the weapon slot on the server
        PropagateHoldableHideClientRpc(item);
    }
    [ClientRpc]
    private void PropagateHoldableHideClientRpc(NetworkObjectReference itemRef){
        // Get the item and weapon slot GameObjects
        NetworkObject n_item = itemRef.TryGet(out NetworkObject itemObj) ? itemObj : null;
        GameObject item = n_item != null ? n_item.gameObject : null;
        if (item == null) return;

        // Hide the item in the weapon slot
        foreach (Renderer r in item.GetComponentsInChildren<Renderer>()) {
            r.enabled = false;
        }

        if (animator){
            animator.SetBool("WeaponEquipped", false);
        } else {
            Debug.LogWarning("Animator is null. Cannot set WeaponEquipped parameter.");
        }
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


    #region ItemAttachment
    [ServerRpc(RequireOwnership = false)]
    private void PropagateItemAttachmentServerRpc(NetworkObjectReference item, NetworkObjectReference n_player){
        if (!IsServer) return;
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
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        item.GetComponent<MonoItem>().IsPickedUp = true; // prevent items in inventory from being picked up
        item.GetComponent<MonoItem>().attachedWeaponSlot = weaponSlot; // local. 
        item.GetComponent<MonoItem>().userRef = _playerObj; // local.
        if (item.GetComponent<Outline>() != null) {
            item.GetComponent<Outline>().enabled = false;
        }

        // freeze rigidbody while held
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

        // unfreeze the rigidbody
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null){
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
        }

    }
    #endregion


    #region Helpers
    private void DropAllOtherClassSpecs(string pickedSpec){
        for (int i = 0; i < _inventoryMono.Length; i++){
            if (_inventoryMono[i] == null){
                continue;
            }
            if (_inventoryMono[i].GetComponent<MonoItem>().IsClassSpec && _inventoryMono[i].GetComponent<MonoItem>().uniqueID != pickedSpec){
                DropItem(i);
            }
        }
    }
    private void DropAllOtherWeapons(){
        for (int i = 0; i < _inventoryMono.Length; i++){
            if (_inventoryMono[i] == null){
                continue;
            }
            if (_inventoryMono[i].GetComponent<MonoItem>().IsWeapon){
                DropItem(i);
            }
        }
    }
    bool IsOwnerValidIndexAndPauseMenuCheck(){
        if (!IsOwner) return false;
        if (PauseMenuToggler.IsPaused) return false;
        if (!ValidIndexCheck()) return false;

        return true;
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
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] != null && _inventoryMono[i].GetComponent<MonoItem>().uniqueID == itemClass) {
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
