using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ItemCarryTriggerVolume : NetworkBehaviour
{
    private NetworkVariable<int> itemsToDeliver; // Runtime managed network int.
    public int itemsNeeded; // Inspector setting.

    void Start(){
        itemsToDeliver = new NetworkVariable<int>(itemsNeeded);
    }

    public int getItemsToDeliver(){
        return itemsToDeliver.Value;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) { return; }
        if (other.gameObject.CompareTag("Item"))
        {
            int itemID = other.gameObject.GetComponent<WorldItem>().GetItemID();
            string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;

            if (stringID == "DeliverableQuestItem"){
                if (itemsToDeliver.Value > 0){
                    itemsToDeliver.Value--;
                    StartCoroutine (DelayedDespawn(other.gameObject, 1.0f));  
                }   
            }
        }
    }
    private IEnumerator DelayedDespawn(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkObjectReference objRef = new NetworkObjectReference(obj.GetComponent<NetworkObject>());
        ItemManager.instance.DestroyWorldItemServerRpc(objRef);
        // ParticleManager.instance.SpawnSelfThenAll(1, obj.transform.position, Quaternion.Euler(0, 0, 0));
    }

}
