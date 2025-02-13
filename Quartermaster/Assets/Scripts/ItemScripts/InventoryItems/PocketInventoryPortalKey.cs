using UnityEngine;

public class PocketInventoryPortalKey : InventoryItem
{
    // Backing fields
    private int id = 0;
    private int pocketInventoryQuantity = 0;
    private float lastUsedTime = float.MinValue;
    private static float itemCooldown = 10f;

    // Abstract overrides
    public override float cooldown
    {
        get => itemCooldown;
        set => itemCooldown = value;
    }
    public override int itemID {
        get => id;
        set => id = value;
    }
    public override int quantity {
        get => pocketInventoryQuantity;
        set => pocketInventoryQuantity = value;
    }

    public override float lastUsed {
        get => lastUsedTime;
        set => lastUsedTime = value;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable()
    {
        return false;
    }

    public override int StackLimit()
    {
        return 1;
    }

    public override void Use(GameObject user)
    {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (lastUsed + cooldown > Time.time){
            Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }
        Debug.Log(itemStr + " (" + itemID + ") used");
    
        if (IsConsumable()){
            quantity--;
        }
        lastUsed = Time.time;

        ItemEffect(user);

    }

    private void ItemEffect(GameObject user){
        PocketInventory.instance.TeleportToPocketClientRpc(user);
    }

}
