using UnityEngine;

public abstract class IWeapon : InventoryItem
{
    public override bool IsConsumable(){
        return false;
    }

    public override int StackLimit(){
        return 1;
    }

    public abstract bool CanAutoFire();

    public abstract void fire(GameObject user);
}
