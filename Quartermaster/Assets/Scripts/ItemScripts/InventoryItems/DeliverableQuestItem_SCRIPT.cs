using UnityEngine;

public class DeliverableQuestItem : InventoryItem
{
    private int _id = 0;
    private int _quantity = 0;
    private float _cooldown = 0;
    private float _lastUsedTime = 0;
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

    public override float lastUsed {
        get => _lastUsedTime;
        set => _lastUsedTime = value;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return false;
    }

    public override int StackLimit() {
        return 1;
    }

    public override bool IsWeapon() {
        return false;
    }

    public override void Use(GameObject user, bool isHeld) {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;

        Debug.Log(itemStr + " (" + itemID + ") used");

        ItemEffect(user);

    }

    private void ItemEffect(GameObject user) {
        // user.GetComponent<PlayerHealth>().Heal(HEAL_AMOUNT);
        // What handles health now?
        // Generate a quaternion for the particle effect to have no rotation
        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));
    }

}
