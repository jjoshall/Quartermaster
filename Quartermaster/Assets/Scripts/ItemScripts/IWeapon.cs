using UnityEngine;

public abstract class IWeapon : InventoryItem
{
    public override bool IsConsumable(){
        return false;
    }

    public override int StackLimit(){
        return 1;
    }

    public override int quantity {
        get => 1;
        set {}
    }

    public override bool IsWeapon(){
        return true;
    }

    public abstract void fire(GameObject user);
}
