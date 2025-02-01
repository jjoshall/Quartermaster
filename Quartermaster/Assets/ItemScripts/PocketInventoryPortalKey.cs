using UnityEngine;

public class PocketInventoryPortalKey : InventoryItem
{
    public override int itemID { get; set; }
    public override void use(GameObject user)
    {
        Debug.Log("PocketInventoryPortalKey used");
        // Reusable item. Do not destroy.
        // Store player current location.
        // Teleport player to pocket inventory location.
        
        
    }
}
