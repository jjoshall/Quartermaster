using UnityEngine;

public class WorldItem : MonoBehaviour
{
    // WorldItem : MonoBehavior is for item objects in the game space.
    //             All game space item prefabs will have this script attached.
    //             All other item scripts correspond to inventory items. 

    private int stackQuantity = 1;

    private float lastUsed = 0;

    [Header("Set ItemID to corresponding index ItemManager's itemEntries")]
    public int itemID; // inherited from ItemManager's list.
                        // or for manually placed prefabs, set it in inspector
    public void initializeItem(int id, int quantity, float timeLastUsed){
        // Called by ItemManager instantiation. ID is index of item in ItemManager's prefab list. 
        itemID = id;
        stackQuantity = quantity;
        lastUsed = timeLastUsed;
        Debug.Log ("GameObject Initialized: " + gameObject + " has itemID: " + itemID + " and quantity: " + stackQuantity);
    }
    public int getItemID(){
        return itemID;
    }

    public int getStackQuantity(){
        return stackQuantity;
    }

    public float getLastUsed(){
        return lastUsed;
    }
    void Start()
    {

    }

    void Update()
    {
        floatyAnimation();
    }

    void floatyAnimation(){
        // Make the item float up and down
    }

}
