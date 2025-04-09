using UnityEngine;

public class DmgSpec : InventoryItem
{

    // private int _id = 0;
    private int _quantity = 0;
    private float _cooldown = 0;
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
        UpdateDmgSpecCount (user);
    }

    public override void Drop(GameObject user)
    {
        UpdateDmgSpecCount (user);
    }
    private void UpdateDmgSpecCount (GameObject user) {
        
        // get user status component
        PlayerStatus status = user.GetComponent<PlayerStatus>();
        Inventory i = user.GetComponent<Inventory>();
        int slot = i.HasItem("DmgSpec");
        int quantity = 0;
        if (slot != -1) {
            quantity = i.GetSlotQuantity (slot);
        }
        // status.UpdateDmgSpecServerRpc(quantity);
        Debug.Log ("picked up healSpec, new healSpec = " + status.GetDmgSpecLvl());  
    }


    public override void InitializeFromGameManager()
    {
        _stackLimit = GameManager.instance.DmgSpec_StackLimit;
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
        // user.GetComponent<PlayerHealth>().Heal(HEAL_AMOUNT);
        // What handles health now?
        // Generate a quaternion for the particle effect to have no rotation
        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));
    }

}
