using UnityEngine;

public class MedKit : InventoryItem
{
    // define itemID as 1
    // write setter to take a parameter
    public override int itemID { get; set; }
    // quantity get set
    public override int quantity {get; set;}

    public override float last_used {get; set;}

    // return value should indicate if it is a consumable (true)
    public override void use(GameObject user)
    {
        Debug.Log("MedKit used");
        // destroy self, replace with empty slot.
        user.GetComponent<PlayerHealth>().Heal(20);
    }

    public override bool isConsumable()
    {
        return true;
    }

    public override int stackLimit(){
        return 5;
    }
}
