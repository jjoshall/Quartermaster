using Unity.Services.Lobbies.Models;
using UnityEngine;

public class HealSpec : InventoryItem
{
    // private int _id = 0;
    private int _quantity = 0;
    private float _cooldown = 0;
    // private float _lastUsedTime = 0;
    private int _stackLimit = 1;

    // Abstract overrides
    public override float cooldown {
        get => _cooldown;
        set => _cooldown = value;
    }
    public override int itemID {
        get => _id;
        set => _id = value;
    }

    public override int quantity {
        get => _quantity;
        set => _quantity = value;
    }

    public override void PickUp(GameObject user)
    {  
        Debug.Log ("picked up healSpec, quantity is now: " + _quantity);
        UpdateHealSpecCount (user);
    }

    public override void Drop(GameObject user)
    {
        Debug.Log ("dropped healSpec, quantity is now: " + _quantity);
        UpdateHealSpecCount (user);
    }

    private void UpdateHealSpecCount (GameObject user) {
        
        // get user status component
        PlayerStatus status = user.GetComponent<PlayerStatus>();
        Inventory i = user.GetComponent<Inventory>();
        int slot = i.HasItem("HealSpec");
        int count = 0;
        if (slot != -1) {
            count = i.GetSlotQuantity (slot);
        }
        status.UpdateHealSpecServerRpc(count);
        Debug.Log ("picked up healSpec, new healSpec = " + status.GetHealSpecLvl());  
    }

    public override void InitializeFromGameManager()
    {
        _stackLimit = GameManager.instance.HealSpec_StackLimit;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return false;
    }

    public override int StackLimit() {
        return _stackLimit;
    }

    public override bool IsWeapon() {
        return false;
    }

    public override bool IsClassSpec()
    {
        return true;
    }

    public override void Use(GameObject user, bool isHeld) {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;

        Debug.Log(itemStr + " (" + itemID + ") used");

        ItemEffect(user);

    }

    private void ItemEffect(GameObject user) {
    }

}
