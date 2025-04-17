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
            // DropSelectedItem();
            DropItem(_currentInventoryIndex);
        }

        // Clamp the inventory index to a valid range.
        _currentInventoryIndex = Mathf.Clamp(_InputHandler.inventoryIndex, 0, _maxInventorySize - 1);

        // If the selection has changed, update the UI and the networked current holdable.
        if (_currentInventoryIndex != _oldInventoryIndex) {
            if (_inventoryMono[_oldInventoryIndex] != null){
                Item old = _inventoryMono[_oldInventoryIndex].GetComponent<Item>();
                if (old == null){
                    Debug.LogError("Inventory: MyInput() - Old item is null.");
                } 
                old.SwapCancel(_playerObj); // resets any charging state.
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
        if  (_inventoryMono[_currentInventoryIndex] == null) return;

        Item itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<Item>();
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
        if (_inventoryMono[_currentInventoryIndex] == null) return;

        Item itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<Item>();
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
        if  (_inventoryMono[_currentInventoryIndex] == null) return;

        Item itemComponent = _inventoryMono[_currentInventoryIndex].GetComponent<Item>();
        if (itemComponent != null) {
            itemComponent.ButtonRelease(_playerObj);
        } else {
            // Do nothing.
        }

        QuantityCheck();
        UpdateAllInventoryUI();
    }

    void QuantityCheck(){
        if (_inventoryMono[_currentInventoryIndex].GetComponent<Item>().quantity <= 0) {
            Debug.Log ("Inventory: QuantityCheck() - Item quantity is 0. Despawning item");
            // despawn network item 
            NetworkObject n_item = _inventoryMono[_currentInventoryIndex].GetComponent<NetworkObject>();
            if (n_item != null) {
                n_item.Despawn(true); // despawn the item on all clients
            } else {
                Debug.LogError("Inventory: QuantityCheck() - Item is null.");
            }
            _inventoryMono[_currentInventoryIndex] = null;
            _currentHeldItems--;
        }
    }


    // -------------------------------------------------------------------------------------------------------------------------
    #region PickUpEvents
    #endregion

    void PickUpClosest() {
        if (!IsOwner) return;

        GameObject pickedUp = _itemAcquisitionRange.
                                    GetComponent<ItemAcquisitionRange>().
                                    GetClosestItem(); // prioritizes raycast over physical closest.

        // Try to stack the item in any existing item stacks
        if (pickedUp != null && 
            pickedUp.GetComponent<Item>() != null &&
            TryStackItem(pickedUp)) {

                // TryStack returns true if fully stacked into existing stacks.
                pickedUp.GetComponent<Item>().PickUp(_playerObj); // Call the item's onPickUp function
                Destroy(pickedUp);

                RemoveFromItemAcq(pickedUp);
                return;
        }

        // CHECK: INVENTORY FULL
        if (_currentHeldItems >= _maxInventorySize){
            return;
        }

        AddToInventory(pickedUp); 
        RemoveFromItemAcq(pickedUp);
    }

    private void RemoveFromItemAcq(GameObject itemPickedUp){
        // Remove the item from the ItemAcquisitionRange
        ItemAcquisitionRange itemAcquisitionRange = _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>();
        if (itemAcquisitionRange) {
            itemAcquisitionRange.RemoveItem(itemPickedUp);
        } else {
            Debug.LogError("ItemAcquisitionRange component not found on _itemAcquisitionRange.");
        }
    }

    private void AddToItemAcq(GameObject itemDropped){
        // Add the item to the ItemAcquisitionRange
        ItemAcquisitionRange itemAcquisitionRange = _itemAcquisitionRange.GetComponent<ItemAcquisitionRange>();
        if (itemAcquisitionRange != null) {
            itemAcquisitionRange.AddItem(itemDropped);
        } else {
            Debug.LogError("ItemAcquisitionRange component not found on _itemAcquisitionRange.");
        }
    }

    private void AddToInventory(GameObject pickedUp){
        var no = pickedUp.GetComponent<NetworkObject>();
        var pno = _playerObj.GetComponent<NetworkObject>();
        if (pickedUp == null) {
            Debug.LogError("Picked up item is null.");
            return;
        }
        if (no == null) {
            Debug.LogError("Picked up item does not have a NetworkObject component.");
            return;
        }
        if (pno == null) {
            Debug.LogError("Player object does not have a NetworkObject component.");
            return;
        }

        // Locally attach the item to the player on each client
        PropagateItemAttachmentServerRpc(no, pno);

        pickedUp.GetComponent<Item>().userRef = _playerObj; // local.

        if (pickedUp.GetComponent<Item>().IsClassSpec){
            // Drop all other class specs
            DropAllOtherClassSpecs(pickedUp.GetComponent<Item>().uniqueID);
        } else if (pickedUp.GetComponent<Item>().IsWeapon) {
            // Drop all other weapons
            Debug.Log ("Dropping all other weapons.");
            DropAllOtherWeapons();
        } 

        // CHECK: CURRENT SLOT EMPTY
        if (_inventoryMono[_currentInventoryIndex] == null) {
            // put in curr slot
            _inventoryMono[_currentInventoryIndex] = pickedUp;
            _inventoryMono[_currentInventoryIndex].GetComponent<Item>().PickUp(_playerObj);

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
        Item newMono = newItem.GetComponent<Item>();
        if (newMono == null) {
            Debug.LogError("Attempting to pick up Item without MonoItem component");
            return false;
        }

        // FOR ITEM IN INVENTORY
        for (int i = 0; i < _inventoryMono.Length; i++){

            // GET CURR
            GameObject curr = _inventoryMono[i];
            if (curr == null) continue;

            Item currMono = curr.GetComponent<Item>();

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


    bool AddToFirstEmptySlot(GameObject item) {
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] == null) {
                _inventoryMono[i] = item;
                item.GetComponent<Item>().PickUp(_playerObj);
                return true;
            }
        }
        return false;
    }

    
    // -------------------------------------------------------------------------------------------------------------------------
    #region DropEvents
    #endregion

    void DropItem(int slot) { // called directly to drop additional ClassSpecs / Weapons
        if (_inventoryMono[slot] == null) return;
        if (!IsOwnerValidIndexAndPauseMenuCheck()) return;
        
        NetworkObjectReference n_playerObj = _playerObj.GetComponent<NetworkObject>();
        Vector3 initVelocity = orientation.forward * GameManager.instance.DropItemVelocity;

        // detach the current held item
        PropagateItemDetachServerRpc(_inventoryMono[slot].GetComponent<NetworkObject>(), n_playerObj, initVelocity);
        

        AddToItemAcq(_inventoryMono[slot]); // add to item acquisition range

        _inventoryMono[slot].GetComponent<Item>().Drop(_playerObj); // Call the item's onDrop function

        _inventoryMono[slot] = null;
        _currentHeldItems--;

        UpdateAllInventoryUI();
        UpdateHeldItem();
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

        float cooldownRemaining = selectedItem.GetComponent<Item>().GetCooldownRemaining();
        float cooldownMax = selectedItem.GetComponent<Item>().GetMaxCooldown();

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
            if (_inventoryMono[i] == null || _inventoryMono[i].GetComponent<Item>() == null) {
                _uiManager.SetInventorySlotTexture(i, null);
                _uiManager.SetInventorySlotQuantity(i, 0, 0);
                continue;
            }
            Texture textureToSet = _inventoryMono[i].GetComponent<Item>().icon;
            int quantity = _inventoryMono[i].GetComponent<Item>().quantity;
            int stackLimit = _inventoryMono[i].GetComponent<Item>().StackLimit;
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

        item.GetComponent<Item>().IsPickedUp = true; // prevent items in inventory from being picked up
        item.GetComponent<Item>().attachedWeaponSlot = weaponSlot; // local. 
        item.GetComponent<Item>().userRef = _playerObj; // local.
        if (item.GetComponent<Outline>() != null) {
            item.GetComponent<Outline>().enabled = false;
        }

        // freeze rigidbody while held
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null){
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void PropagateItemDetachServerRpc(NetworkObjectReference item, NetworkObjectReference n_player, Vector3 initVelocity){
        if (!IsServer) return;
        var itemNO = item.TryGet(out NetworkObject itemObj) ? itemObj : null;
        var playerNO = n_player.TryGet(out NetworkObject playerObj) ? playerObj : null;
        if (itemNO == null || playerNO == null) {
            Debug.LogError("PropagateItemDetachServerRpc: item or player is null.");
            return;
        }
        // Attach the item to the weapon slot on the server
        DetachItemClientRpc(item, n_player);
        var itemNT = itemNO.GetComponent<NetworkTransform>();
        if (itemNT != null) {
            itemNT.enabled = true; // Enable the NetworkTransform component
            itemNT.Teleport(playerNO.transform.position, Quaternion.identity, itemNT.transform.localScale);
        } else {
            Debug.LogError("PropagateItemDetachServerRpc: itemNT is null.");
        }
        // Give it velocity
        Rigidbody rb = itemNO.GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = initVelocity;
        }
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
        item.GetComponent<Item>().IsPickedUp = false; // prevent items in inventory from being picked up
        item.GetComponent<Item>().attachedWeaponSlot = null; // local.
        item.GetComponent<Item>().userRef = null; // local.

        MeshRenderer[] renderers = item.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer r in renderers) {
            r.enabled = true;
        }

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
    public int GetSlotQuantity (int slot) {
        if (_inventoryMono[slot] == null) return 0;
        return _inventoryMono[slot].GetComponent<Item>().quantity;
    }

    public int GetItemQuantity (string uniqueID) {
        int total = 0;
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] != null && _inventoryMono[i].GetComponent<Item>().uniqueID == uniqueID) {
                total += _inventoryMono[i].GetComponent<Item>().quantity;
            }
        }
        return total;
    }
    private void DropAllOtherClassSpecs(string pickedSpec){
        for (int i = 0; i < _inventoryMono.Length; i++){
            if (_inventoryMono[i] == null){
                continue;
            }
            if (_inventoryMono[i].GetComponent<Item>().IsClassSpec && _inventoryMono[i].GetComponent<Item>().uniqueID != pickedSpec){
                DropItem(i);
            }
        }
    }
    private void DropAllOtherWeapons(){
        for (int i = 0; i < _inventoryMono.Length; i++){
            if (_inventoryMono[i] == null){
                continue;
            }
            if (_inventoryMono[i].GetComponent<Item>().IsWeapon){
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
        // if (_inventoryMono[_currentInventoryIndex] == null) {
        //     return false;
        // }
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
            if (_inventoryMono[i] != null && _inventoryMono[i].GetComponent<Item>().IsWeapon) {
                return i;
            }
        }
        return -1;
    }

    public int HasItem(string itemClass) {
        for (int i = 0; i < _inventoryMono.Length; i++) {
            if (_inventoryMono[i] != null && _inventoryMono[i].GetComponent<Item>().uniqueID == itemClass) {
                return i;
            }
        }
        return -1;
    }

    #endregion
}
