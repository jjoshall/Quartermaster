using UnityEngine;

public class MedKit : InventoryItem
{
    // define itemID as 1
    // write setter to take a parameter
    public override int itemID { get; set; }
    public override void use()
    {
        Debug.Log("MedKit used");
        // destroy self, replace with empty slot.
    }
}
