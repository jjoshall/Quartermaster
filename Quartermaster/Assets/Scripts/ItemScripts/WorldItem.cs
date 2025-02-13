using UnityEngine;
using Unity.Netcode;

public class WorldItem : NetworkBehaviour
{
    // WorldItem : MonoBehavior is for item objects in the game space.
    //             All game space item prefabs will have this script attached.
    //             All other item scripts correspond to inventory items. 

    // DEPRECATED
    // private int stackQuantity = 1;
    // private float lastUsed = 0;

    // NETWORK VARIABLES
    private NetworkVariable<int> n_stackQuantity = new NetworkVariable<int>(1);
    private NetworkVariable<float> n_lastUsed = new NetworkVariable<float>(0);

    [Header("Set ItemID to corresponding index ItemManager's itemEntries")]
    public int itemID; // inherited from ItemManager's list.
                        // or for manually placed prefabs, set it in inspector
    public void InitializeItem(int id, int quantity, float timeLastUsed){
        // Called by ItemManager instantiation. ID is index of item in ItemManager's prefab list. 
        itemID = id;
        // stackQuantity = quantity;
        n_stackQuantity.Value = quantity;
        // lastUsed = timeLastUsed;
        n_lastUsed.Value = timeLastUsed;
        Debug.Log ("GameObject Initialized: " + gameObject + " has itemID: " + itemID + " and quantity: " + n_stackQuantity.Value);
    }
    public int GetItemID(){
        return itemID;
    }

    public int GetStackQuantity(){
        return n_stackQuantity.Value;
    }

    public float GetLastUsed(){
        return n_lastUsed.Value;
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
