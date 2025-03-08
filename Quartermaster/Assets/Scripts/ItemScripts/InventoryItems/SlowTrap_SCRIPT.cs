using UnityEngine;

public class SlowTrap : InventoryItem {
    // Backing fields
    // private int _id = 0;
    private int _slowtrapQuantity = 0;
    private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 10f;
    
    private float _slowPercentage = 0.0f; // enemy movespeed *= 1 - slowPercentage
                                          // spawned slowtrap prefab should inherit this value

    // Abstract overrides
    public override float cooldown {
        get => _itemCooldown;
        set => _itemCooldown = value;
    }
    public override int itemID {
        get => _id;
        set => _id = value;
    }

    public override int quantity {
        get => _slowtrapQuantity;
        set => _slowtrapQuantity = value;
    }


    public override void InitializeFromGameManager()
    {
        _itemCooldown = GameManager.instance.SlowTrap_Cooldown;
        _slowPercentage = GameManager.instance.SlowTrap_SlowByPct;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return true;
    }

    public override int StackLimit() {
        return GameManager.instance.SlowTrap_StackLimit;
    }

    public override bool IsWeapon() {
        return false;
    }


    public override void Use(GameObject user, bool isHeld) {
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
        
    }

}
