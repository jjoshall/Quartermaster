using UnityEngine;

public abstract class InventoryItem
{
    // define an int itemID that all subclasses should define as a constant
    public abstract int itemID {get; set;}

    public abstract void use();
}
