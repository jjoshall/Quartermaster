using UnityEngine;

public class MedKit : InventoryItem {
    // Backing fields
    private int _id = 0;
    private int _medkitQuantity = 0;
    private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 0.1f;

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
        get => _medkitQuantity;
        set => _medkitQuantity = value;
    }

    public override float lastUsed {
        get => _lastUsedTime;
        set => _lastUsedTime = value;
    }

    public override void InitializeFromGameManager()
    {
        _itemCooldown = GameManager.instance.MedKit_Cooldown;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return true;
    }

    public override int StackLimit() {
        return 5;
    }

    public override bool IsWeapon() {
        return false;
    }

    // Item constants
    private const int HEAL_AMOUNT = 50;

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
        // user.GetComponent<PlayerHealth>().Heal(HEAL_AMOUNT);
        // What handles health now?
        // Generate a quaternion for the particle effect to have no rotation
        Health hp = user.GetComponent<Health>();
        if (hp == null) {
            Debug.LogError("MedKit: ItemEffect: No Health component found on user.");
            return;
        }
        hp.HealServerRpc(HEAL_AMOUNT);
        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));
    }

}
