using UnityEngine;

public abstract class InventoryItem
{
    // define an int itemID that all subclasses should define as a constant
    public abstract int itemID {get; set;}

    public abstract int quantity {get; set;}

    public abstract float lastUsed {get; set;}

    // class static cooldown. defined by inherited class
    public abstract float cooldown {get; set;}

    public abstract void Use(GameObject user);

    public abstract bool IsConsumable();

    public abstract int StackLimit();
}
