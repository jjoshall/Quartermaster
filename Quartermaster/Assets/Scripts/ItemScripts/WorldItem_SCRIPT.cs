using UnityEngine;
using Unity.Netcode;

public class WorldItem : NetworkBehaviour {
    // WorldItem : MonoBehavior is for item objects in the game space.
    //             All game space item prefabs will have this script attached.
    //             All other item scripts correspond to inventory items. 


    // NETWORK VARIABLES
    public NetworkVariable<int> n_stackQuantity = new NetworkVariable<int>(1);
    public NetworkVariable<float> n_lastUsed = new NetworkVariable<float>(0);

    [Header("Set ItemID to corresponding index ItemManager's itemEntries")]
    public int itemID; // inherited from ItemManager's list.
                        // or for manually placed prefabs, set it in inspector
    public void InitializeItem(int id, int quantity, float timeLastUsed) {
        // Called by ItemManager instantiation. ID is index of item in ItemManager's prefab list. 
        itemID = id;
        // stackQuantity = quantity;
        n_stackQuantity.Value = quantity;
        // lastUsed = timeLastUsed;
        n_lastUsed.Value = timeLastUsed;
    }
    
    public int GetItemID() {
        return itemID;
    }

    public int GetStackQuantity() {
        return n_stackQuantity.Value;
    }

    public float GetLastUsed() {
        return n_lastUsed.Value;
    }

    void Update() {
        FloatyAnimation();
    }

    void FloatyAnimation() {
        // Make the item float up and down
    }

    public override void OnDestroy() {
    // Notify all nearby acquisition ranges
        foreach (var collider in Physics.OverlapSphere(transform.position, 2.0f)) {
            ItemAcquisitionRange range = collider.GetComponent<ItemAcquisitionRange>();
            if (range != null) {
                range.RemoveItem(gameObject);
            }
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        foreach (var collider in Physics.OverlapSphere(transform.position, 2.0f)) {
            var range = collider.GetComponent<ItemAcquisitionRange>();
            if (range != null) {
                range.AddItem(gameObject);
            }
        }
    }
}
