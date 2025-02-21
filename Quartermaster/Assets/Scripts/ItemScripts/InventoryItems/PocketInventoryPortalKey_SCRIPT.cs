using UnityEngine;

public class PocketInventoryPortalKey : InventoryItem {
    [Header("Backing Fields")]
    private int _id = 0;
    private int _pocketInventoryQuantity = 0;
    private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 10f;

    [Header("Abstract Overrides")]
    public override float cooldown {
        get => _itemCooldown;
        set => _itemCooldown = value;
    }

    public override int itemID {
        get => _id;
        set => _id = value;
    }

    public override int quantity {
        get => _pocketInventoryQuantity;
        set => _pocketInventoryQuantity = value;
    }

    public override float lastUsed {
        get => _lastUsedTime;
        set => _lastUsedTime = value;
    }

    public override bool IsWeapon(){
        return false;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return false;
    }

    public override int StackLimit() {
        return 1;
    }

    public override void Use(GameObject user , bool isHeld) {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (lastUsed + cooldown > Time.time) {
            Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }

        Debug.Log(itemStr + " (" + itemID + ") used");
    
        if (IsConsumable()) {
            quantity--;
        }

        lastUsed = Time.time;

        ItemEffect(user);

    }

    private void ItemEffect(GameObject user) {
        PocketInventory.instance.TeleportToPocketServerRpc(user);
    }

}
