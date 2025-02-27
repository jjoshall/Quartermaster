using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ItemCarryTriggerVolume : NetworkBehaviour
{
    [Header("Vars")]
    private NetworkVariable<int> n_itemsToDeliver; // Runtime managed network int.
    public int itemsNeeded; // Inspector setting.

    void Start(){
        n_itemsToDeliver = new NetworkVariable<int>(itemsNeeded);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) { return; }
        if (other.gameObject.CompareTag("Item"))
        {
            int itemID = other.gameObject.GetComponent<WorldItem>().GetItemID();
            string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;

            if (stringID == "DeliverableQuestItem"){
                if (n_itemsToDeliver.Value > 0){
                    n_itemsToDeliver.Value--;
                    StartCoroutine (_DelayedDespawn(other.gameObject, 1.0f));  
                }   
            }
        }
    }

    #region = DespawnItem
    private IEnumerator _DelayedDespawn(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkObjectReference objRef = new NetworkObjectReference(obj.GetComponent<NetworkObject>());
        ItemManager.instance.DestroyWorldItemServerRpc(objRef);
        // ParticleManager.instance.SpawnSelfThenAll(1, obj.transform.position, Quaternion.Euler(0, 0, 0));
    }
    #endregion

    
    #region = Helpers
    public int GetItemsToDeliver(){
        return n_itemsToDeliver.Value;
    }
    #endregion

}
