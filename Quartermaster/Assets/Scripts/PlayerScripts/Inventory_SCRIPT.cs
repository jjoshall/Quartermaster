using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Add this for NetworkTransform
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInputHandler))]
public class Inventory : NetworkBehaviour {
    private UIManager _uiManager;
    private Animator animator;
    private GameObject _playerObj;
    private ItemAcquisitionRange _itemAcquisitionRange;


    [Header("Orientation for dropItem direction")]
    public Transform orientation;

    [Header("Inventory Keybinds")]
    private PlayerInputHandler _InputHandler;

    [Header("Weapon Holdable Setup")]
    public GameObject weaponSlot;

    [Header("Runtime Variables")]
    // Reference to currently selected item for this inventory.
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

    [SerializeField]
    private float _maxInventoryWeight = 100f; // Default max. Can be changed at run-time. 
    private float _currentInventoryWeight = 0.0f;

    public override void OnNetworkSpawn(){
        _playerObj = this.gameObject;
        _itemAcquisitionRange = _playerObj.GetComponentInChildren<ItemAcquisitionRange>();
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
            // DropSelectedItem();
            DropItem(_currentInventoryIndex);
        }

        // Clamp the inventory index to a valid range.
        _currentInventoryIndex = Mathf.Clamp(_InputHandler.inventoryIndex, 0, _maxInventorySize - 1);

        // If the selection has changed, update the UI and the networked current holdable.
        if (_currentInventoryIndex != _oldInventoryIndex) {
            Item oldItem = GetItemAt(_oldInventoryIndex);
            if (oldItem){
                oldItem.OnSwapOut(_playerObj); // resets any charging state.
            }
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
        if (selectedItem) {
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

        if (_inventoryMono[_currentInventoryIndex]?.TryGetComponent<Item>(out var item) != true)
            return;
        item.OnButtonUse(_playerObj);

        QuantityCheck(); 
        UpdateAllInventoryUI();
    }

    void HeldItem(){
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;

        if (_inventoryMono[_currentInventoryIndex]?.TryGetComponent<Item>(out var item) != true)
            return;
        item.OnButtonHeld(_playerObj);

        QuantityCheck();
        UpdateAllInventoryUI();
    }

    void ReleaseItem(bool b) {
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;

        if (_inventoryMono[_currentInventoryIndex]?.TryGetComponent<Item>(out var item) != true)
            return;
        item.OnButtonRelease(_playerObj);

        QuantityCheck();
        UpdateAllInventoryUI();
    }

    void QuantityCheck(){
        if (_inventoryMono[_currentInventoryIndex].GetComponent<Item>().quantity <= 0) {
            Debug.Log ("Inventory: QuantityCheck() - Item quantity is 0. Despawning item");
            // despawn network item 
            NetworkObject n_item = _inventoryMono[_currentInventoryIndex].GetComponent<NetworkObject>();
            if (n_item != null) {
                DespawnItemServerRpc(n_item);
            } else {
                Debug.LogError("Inventory: QuantityCheck() - Item is null.");
            }
            _inventoryMono[_currentInventoryIndex] = null;
            _currentHeldItems--;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemServerRpc(NetworkObjectReference item){
        if (!IsServer) return;

        // Despawn the item on all clients
        if (item.TryGet(out var itemNO)) {
            itemNO.Despawn(true);
        } else {
            Debug.LogError("DespawnItemServerRpc: item is null.");
        }
    }


    // -------------------------------------------------------------------------------------------------------------------------
    #region PickUpEvent
    #endregion

    // Entry point for item pickup logic.
    void PickUpClosest() {
        if (!IsOwner) return;

        GameObject pickedUp = _itemAcquisitionRange.GetClosestItem(); // prioritizes raycast over physical closest.

        if (!pickedUp) {
            Debug.Log("No item to pick up.");
            return;
        }

        if (!pickedUp.GetComponent<Item>()) {
            Debug.Log("PickedUp obj: " + pickedUp + " does not have an Item component.");
            return;
        }

        if (CanCarry(pickedUp.GetComponent<Item>()) == false) {
            Debug.Log("Inventory: PickUpClosest() - Cannot carry item. Weight exceeds max weight.");
            return;
        }

        // Try to stack the item in any existing item stacks. Returns true if fully stacked into existing stacks.
        if (TryStackItem(pickedUp)) {
            pickedUp.GetComponent<Item>().OnPickUp(_playerObj); // Call the item's onPickUp function
            RemoveFromItemAcqLocal(pickedUp);
            var netObj = pickedUp.GetComponent<NetworkObject>();
            if (netObj != null) {
                DespawnItemServerRpc(netObj);
            } else {
                Debug.LogError("Picked up item does not have a NetworkObject component.");
            }
            // Destroy(pickedUp);
            UpdateInventoryWeight();
            UpdateAllInventoryUI();
            return;
        } else if (_currentHeldItems < _maxInventorySize) {
            RemoveFromItemAcqLocal(pickedUp);
            AddToInventory(pickedUp); 
            UpdateInventoryWeight();
            UpdateAllInventoryUI();
            return;
        }
    }


    // -------------------------------------------------------------------------------------------------------------------------
    #region PickupHelpers
    #endregion 
    private void AddToInventory(GameObject pickedUp) {
        GetPickupVars(pickedUp, out var item, out var itemNO, out var playerNO);
        if (item == null || itemNO == null || playerNO == null) return;

        // local userRef set first to avoid null reference errors.
        item.userRef = _playerObj;

        // Visually/physically attach the item on all clients
        PropagateItemAttachmentServerRpc(itemNO, playerNO, true);

        HandleItemExclusivity(item);
        

        if (TryPlaceInCurrentSlot(pickedUp, item) || AddToFirstEmptySlot(pickedUp)) { // short circuits on first success. 
                                                                                      // sets inventory[i] to pickedUp and calls OnPickup.
            _currentHeldItems++;
            UpdateHeldItem();
            UpdateHeldItemNetworkReference();
        }
    }
    private void GetPickupVars(GameObject pickedUp, out Item item, out NetworkObject itemNO, out NetworkObject playerNO) {
        item = null;
        itemNO = null;
        playerNO = _playerObj?.GetComponent<NetworkObject>();

        if (playerNO == null) {
            Debug.LogError("Player object does not have a NetworkObject component.");
            return;
        }

        if (pickedUp == null) {
            Debug.LogError("Picked up item is null.");
            return;
        }

        itemNO = pickedUp.GetComponent<NetworkObject>();
        if (itemNO == null) {
            Debug.LogError("Picked up item does not have a NetworkObject component.");
            return;
        }

        item = pickedUp.GetComponent<Item>();
        if (item == null) {
            Debug.LogError("Picked up item does not have an Item component.");
            return;
        }
    }

    private void HandleItemExclusivity(Item item) {
        if (item.IsClassSpec) {
            DropAllOtherClassSpecs(item.uniqueID);
        } else if (item.IsWeapon) {
            Debug.Log("Dropping all other weapons.");
            DropAllOtherWeapons();
        }
    }

    private bool TryPlaceInCurrentSlot(GameObject pickedUp, Item item) {
        if (_inventoryMono[_currentInventoryIndex] != null) return false;

        _inventoryMono[_currentInventoryIndex] = pickedUp;
        item.OnPickUp(_playerObj);
        return true;
    }

    private void UpdateHeldItemNetworkReference() {
        UpdateHeldItem();                   // Hide/show items based on current slot
        UpdateHoldableNetworkReference();   // Sync network variable for current item
    }

    private void RemoveFromItemAcqLocal(GameObject itemPickedUp){
        // Remove the item from the ItemAcquisitionRange
        if (_itemAcquisitionRange) {
            _itemAcquisitionRange.RemoveItem(itemPickedUp);
        } else {
            Debug.LogError("ItemAcquisitionRange component not found on _itemAcquisitionRange.");
        }

        var netObj = itemPickedUp.GetComponent<NetworkObject>();

        RemoveFromAllPlayersItemAcqServerRpc(netObj);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveFromAllPlayersItemAcqServerRpc(NetworkObjectReference item){
        if (!IsServer) return;

        RemoveFromPlayersItemAcqClientRpc(item);
    }

    [ClientRpc]
    private void RemoveFromPlayersItemAcqClientRpc(NetworkObjectReference item){
        if (!item.TryGet(out var itemNO)) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players) {
            ItemAcquisitionRange itemAcqRange = player.GetComponentInChildren<ItemAcquisitionRange>();
            if (itemAcqRange != null) {
                itemAcqRange.RemoveItem(itemNO.gameObject);
            } else {
                Debug.LogError("ItemAcquisitionRange component not found on player.");
            }
        }
    }
    
    // Try to stack the item in any existing item stacks in inventory.
    // Returns true if the quantity of the pickedup item was fully stacked into existing stacks (no remainder).
    // Returns false if the item was not fully stacked (remainder exists).
    bool TryStackItem(GameObject newItem) {
        Item newMono = newItem.GetComponent<Item>();
        if (newMono == null) {
            Debug.LogError("Attempting to pick up Item without MonoItem component");
            return false;
        }

        // FOR ITEM IN INVENTORY
        for (int i = 0; i < _inventoryMono.Length; i++){

            Item currMono = GetItemAt(i);
            if (currMono == null) continue;

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

    bool AddToFirstEmptySlot(GameObject itemGO) {
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] == null) {
                _inventoryMono[i] = itemGO;
                itemGO.GetComponent<Item>().OnPickUp(_playerObj);
                return true;
            }
        }
        return false;
    }

    
    // -------------------------------------------------------------------------------------------------------------------------
    #region DropEvent
    #endregion

    void DropItem(int thisSlot) { // called directly to drop additional ClassSpecs / Weapons
        if (!IsOwner) return;
        if (PauseMenuToggler.IsPaused) return;

        Item thisItem = GetItemAt(thisSlot);
        var itemGO = _inventoryMono[thisSlot];
        if (!thisItem || !itemGO || !_playerObj) return;

        var itemNO = itemGO.GetComponent<NetworkObject>();
        var playerNO = _playerObj.GetComponent<NetworkObject>();
        if (!itemNO || !playerNO) return;

        // detach the current held item
        PropagateItemAttachmentServerRpc(itemNO, playerNO, false);
        AddToItemAcq(itemGO); // add to item acquisition range
        thisItem.OnDrop(_playerObj); // Call the item's onDrop function
        PropagateHoldableShowServerRpc(itemNO, true); // show the item that is being dropped.

        _inventoryMono[thisSlot] = null;
        _currentHeldItems--;
        UpdateAllInventoryUI();
        UpdateHeldItem();
    }
    private void AddToItemAcq(GameObject itemDropped){
        // Add the item to the ItemAcquisitionRange
        if (_itemAcquisitionRange != null) {
            _itemAcquisitionRange.AddItem(itemDropped);
        } else {
            Debug.LogError("ItemAcquisitionRange component not found on _itemAcquisitionRange.");
        }
    }

    // -------------------------------------------------------------------------------------------------------------------------
    #region UIUpdate
    #endregion

    // Update the radial dial display for current item's cooldown.
    void UpdateWeaponCooldownUI() {
        if (!IsOwner || _uiManager == null) return;

        Item item = GetItemAt(_currentInventoryIndex);
        if (item == null) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
            return;
        }

        float cooldownRemaining = item.GetCooldownRemaining();
        float cooldownMax = item.GetMaxCooldown();

        PlayerController playerController = _playerObj.GetComponent<PlayerController>();
        float stimAspdMultiplier = 1.0f;
        if (playerController != null) {
            stimAspdMultiplier = playerController.stimAspdMultiplier;
        }

        cooldownMax /= stimAspdMultiplier;

        if (cooldownMax > 0) {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(true);
            float cooldownRatio = Mathf.Clamp01(1 - (cooldownRemaining / cooldownMax));
            _uiManager.weaponCooldownRadial.fillAmount = Mathf.Lerp(0f, 0.25f, cooldownRatio);
        } else {
            _uiManager.weaponCooldownRadial.gameObject.SetActive(false);
        }
    }

    // Update the visibility of player currently held item for all clients.
    void UpdateHeldItem() {
        for (int i = 0; i < _inventoryMono.Length; i++){
            Item item = GetItemAt(i);
            if (item == null){
                continue;
            }
            NetworkObject netObj = item.GetComponent<NetworkObject>();
            if (i != _currentInventoryIndex){
                PropagateHoldableShowServerRpc(netObj, false);
            } else {
                PropagateHoldableShowServerRpc(netObj, true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PropagateHoldableShowServerRpc(NetworkObjectReference item, bool holdableVisibility){
        if (!IsServer) return;
        // Attach the item to the weapon slot on the server
        var GO = item.TryGet(out NetworkObject n_item) ? n_item.gameObject : null;
        if (GO == null) return;
        var itemComponent = GO.GetComponent<Item>();
        if (itemComponent == null) return;
        itemComponent.n_isCurrentlySelected.Value = holdableVisibility;
        PropagateHoldableShowClientRpc(item, holdableVisibility);
    }
    [ClientRpc]
    private void PropagateHoldableShowClientRpc(NetworkObjectReference itemRef, bool holdableVisibility){
        // Get the item and weapon slot GameObjects
        NetworkObject n_item = itemRef.TryGet(out NetworkObject itemObj) ? itemObj : null;
        GameObject item = n_item != null ? n_item.gameObject : null;
        if (item == null) return;

        // Show the item in the weapon slot
        foreach (Renderer r in item.GetComponentsInChildren<Renderer>()) {
            r.enabled = holdableVisibility;
        }

        if (animator){
            animator.SetBool("WeaponEquipped", holdableVisibility);
        } else {
            Debug.LogWarning("Animator is null. Cannot set WeaponEquipped parameter.");
        }
    }

    private void UpdateAllInventoryUI() {
        for (int i = 0; i < _maxInventorySize; i++) {
            Item item = GetItemAt(i);
            if (!item) {
                _uiManager.SetInventorySlotTexture(i, null);
                _uiManager.SetInventorySlotQuantity(i, 0, 0);
                continue;
            }
            Texture textureToSet = item.icon;
            int quantity = item.quantity;
            int stackLimit = item.StackLimit;
            _uiManager.SetInventorySlotTexture(i, textureToSet);
            _uiManager.SetInventorySlotQuantity(i, quantity, stackLimit);
        }
    }


    #region ItemAttachment

    // Attach = true when item is picked up. Detach = false when item is dropped (teleports networktransform to avoid interpolation)
    // Bool is passed to attachitemclientrpc
    [ServerRpc(RequireOwnership = false)]
    private void PropagateItemAttachmentServerRpc(NetworkObjectReference item, NetworkObjectReference n_player, bool attach){
        if (!IsServer) return;

        Vector3 velocity = orientation.forward * GameManager.instance.DropItemVelocity;
        AttachItemClientRpc(item, n_player, attach, velocity);

        if (attach) return; // finish here if not detaching.

        // Teleporting the item to the player's position when detaching (dropping item)
        if (!item.TryGet(out var itemNO) || !n_player.TryGet(out var playerNO)) {
            Debug.LogError("PropagateItemDetachServerRpc: item or player is null.");
            return;
        }
        var itemNT = itemNO.GetComponent<NetworkTransform>();
        if (itemNT != null) {
            itemNT.enabled = true; // Enable the NetworkTransform component
            itemNT.Teleport(playerNO.transform.position, Quaternion.identity, itemNT.transform.localScale);
        } else {
            Debug.LogError("PropagateItemDetachServerRpc: itemNT is null.");
        }
    }

    [ClientRpc]
    private void AttachItemClientRpc(NetworkObjectReference itemRef, NetworkObjectReference n_playerRef, bool attach, Vector3 velocity){
        if (!itemRef.TryGet(out var itemNO) || !n_playerRef.TryGet(out var playerNO)) return;

        var itemGO = itemNO.gameObject;
        var playerGO = playerNO.gameObject;

        var item = itemGO.GetComponent<Item>();
        var itemNT = itemGO.GetComponent<NetworkTransform>();
        var rb = itemGO.GetComponent<Rigidbody>();
        var outline = itemGO.GetComponent<Outline>();
        var slot = playerGO.GetComponent<Inventory>()?.weaponSlot;

        if (item == null || itemNT == null || slot == null) return;

        item.IsPickedUp = attach;
        item.attachedWeaponSlot = attach ? slot : null;
        item.userRef = attach ? _playerObj : null; 

        itemNT.enabled = !attach;
        itemGO.transform.localPosition = Vector3.zero;
        itemGO.transform.localRotation = Quaternion.identity;

        if (outline != null) outline.enabled = !attach;

        if (rb != null) {
            rb.isKinematic = attach;
            rb.useGravity = !attach;
            rb.constraints = attach ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
            if (!attach) rb.linearVelocity = velocity;
        }
    }
    #endregion


    #region InventoryWeightHelpers
    #endregion
    private void UpdateInventoryWeight(){
        _currentInventoryWeight = GetCurrentInventoryWeight();
    }
    public bool CanCarry (Item pickup){
        if (pickup == null) return false;
        UpdateInventoryWeight();
        if (_currentInventoryWeight + pickup.weight * pickup.quantity > _maxInventoryWeight) {
            Debug.Log("Inventory: CanCarry() - Cannot carry item. Weight exceeds max weight.");
            return false;
        }
        return true;
    }
    public float GetCurrentInventoryWeight(){
        float totalWeight = 0.0f;
        for (int i = 0; i < _inventoryMono.Length; i++) {
            Item item = GetItemAt(i);
            if (item != null) {
                totalWeight += item.weight * item.quantity;
            }
        }
        return totalWeight;
    }
    #region Helpers
    public int GetSlotQuantity (int slot) {
        Item item = GetItemAt(slot);
        if (!item) return 0;
        return item.quantity;
    }

    public int GetItemQuantity (string uniqueID) {
        int total = 0;
        for (int i = 0; i < _inventoryMono.Length; i++) {
            Item item = GetItemAt(i);
            if (!item) continue;
            if (item.uniqueID == uniqueID) {
                total += item.quantity;
            }
        }
        return total;
    }
    private void DropAllOtherClassSpecs(string pickedSpec){
        for (int i = 0; i < _inventoryMono.Length; i++){
            Item item = GetItemAt(i);
            if (!item){
                continue;
            }
            if (item.IsClassSpec && item.uniqueID != pickedSpec){
                DropItem(i);
            }
        }
    }
    private void DropAllOtherWeapons(){
        for (int i = 0; i < _inventoryMono.Length; i++){
            Item item = GetItemAt(i);
            if (!item){
                continue;
            }
            if (item.IsWeapon){
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
            Item item = GetItemAt(i);
            if (item && item.IsWeapon) {
                return i;
            }
        }
        return -1;
    }

    public int HasItem(string itemClass) {
        for (int i = 0; i < _inventoryMono.Length; i++) {
            Item item = GetItemAt(i);
            if (item && item.uniqueID == itemClass) {
                return i;
            }
        }
        return -1;
    }

    private Item GetItemAt (int index) {
        if (index < 0 || index >= _inventoryMono.Length) {
            Debug.LogError("Index out of bounds: " + index);
            return null;
        }
        if (_inventoryMono[index] == null) {
            // Debug.Log("Item at index " + index + " is null.");
            return null;
        }
        return _inventoryMono[index]?.GetComponent<Item>();
    }

    #endregion
}
