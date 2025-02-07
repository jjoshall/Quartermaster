using UnityEngine;

public class PocketInventoryPortalKey : InventoryItem
{
    public override int itemID { get; set; }
    public override int quantity {get; set;}
    public override float last_used {get; set;}
    public override void use(GameObject user)
    {
        Debug.Log("PocketInventoryPortalKey used");
        // Reusable item. Do not destroy.
        // Store player current location.
        // Teleport player to pocket inventory location.
        PocketInventory.instance.teleportToPocket(user);
    }

    public override bool isConsumable()
    {
        return false;
    }

    public override int stackLimit()
    {
        return 1;
    }
}
