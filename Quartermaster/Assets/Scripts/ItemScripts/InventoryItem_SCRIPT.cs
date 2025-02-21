using UnityEngine;

public abstract class InventoryItem {
    // define an int itemID that all subclasses should define as a constant
    public abstract int itemID { get; set; }
    public virtual bool isHoldable { get; set; } = false;

    public abstract int quantity { get; set; }

    public abstract float lastUsed { get; set; }

    // class static cooldown. defined by inherited class
    public abstract float cooldown { get; set; }

    public void AttemptUse(GameObject user, bool isHeld){
        if (isHeld && !isHoldable){
            return;
        }else{
            Use(user, isHeld);
        }
    }

    public abstract void Use(GameObject user, bool isHeld);

    public abstract bool IsConsumable();

    public abstract bool IsWeapon();

    public abstract int StackLimit();
}
