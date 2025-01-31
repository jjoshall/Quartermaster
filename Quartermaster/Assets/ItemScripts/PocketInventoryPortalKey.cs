using UnityEngine;

public class PocketInventoryPortalKey : InventoryItem
{
    public override int itemID { get; set; }
    public override void use()
    {
        Debug.Log("PocketInventoryPortalKey used");
        // Reusable item. Do not destroy.


    }
}
