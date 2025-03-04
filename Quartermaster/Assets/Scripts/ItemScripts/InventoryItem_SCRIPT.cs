using UnityEngine;

public abstract class InventoryItem {
    // define an int itemID that all subclasses should define as a constant
    public abstract int itemID { get; set; }
    public virtual bool isHoldable { get; set; } = false;

    public abstract int quantity { get; set; }

    public abstract float lastUsed { get; set; }

    // class static cooldown. defined by inherited class
    public abstract float cooldown { get; set; }

    public virtual void InitializeFromGameManager(){
        // Do nothing by default.
    }

    // Current PlayerInputHandler fires event to which UseItem() is subscribed every update
    //                         with bool isHeld == true.
    //               Fires event for UseItem() with bool isHeld == false when initially pressed.
    // UseItem() checks attemptuse. If item isn't "holdable", it won't auto-fire.
    public void AttemptUse(GameObject user, bool isHeld){
        if (isHeld && !isHoldable){
            return;
        }else{
            Use(user, isHeld);
        }
    }

    public abstract void Use(GameObject user, bool isHeld);

    public virtual void Release(GameObject user){
        // Do nothing by default.
    }

    public abstract bool IsConsumable();

    public abstract bool IsWeapon();

    public abstract int StackLimit();
}
