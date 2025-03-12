using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ItemCarryObjective : IObjective
{
    private NetworkVariable<int> n_itemsToDeliver = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        n_itemsToDeliver.Value = Random.Range(1, 5);
    }

    public override bool IsComplete()
    {
        if (n_itemsToDeliver.Value > 0)
        {
            return false;
        }
        GameManager.instance.AddScoreServerRpc(200);
        Debug.Log("Total score " + GameManager.instance.totalScore.Value);
        return true;
    }

    // OnTriggerEnter check for item DeliverableQuestItem
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item") && 
            other.gameObject.name == "DeliverableQuestItem")
        {
            int itemID = other.gameObject.GetComponent<WorldItem>().GetItemID();
            string stringID = ItemManager.instance.itemEntries[itemID].inventoryItemClass;

            if (stringID == "DeliverableQuestItem"){
                if (n_itemsToDeliver.Value > 0){
                    StartCoroutine (_DelayedDespawn(other.gameObject, 1.0f));  
                }   
            }
        }

        // other. GetComponentInChildren<PlayerDissolveAnimator>().AnimateDissolveServerRpc();
    }

    #region = DespawnItem
    private IEnumerator _DelayedDespawn(GameObject obj, float delay)
    {
        obj.GetComponentInChildren<PlayerDissolveAnimator>().AnimateDissolveServerRpc();
        yield return new WaitForSeconds(delay);
        if (obj == null) { yield break; }
        n_itemsToDeliver.Value--;
        NetworkObjectReference objRef = new NetworkObjectReference(obj.GetComponent<NetworkObject>());
        ItemManager.instance.DestroyWorldItemServerRpc(objRef);
        // ParticleManager.instance.SpawnSelfThenAll(1, obj.transform.position, Quaternion.Euler(0, 0, 0));
        if (n_itemsToDeliver.Value <= 0){
            ClearObjective();
        }
    }
    #endregion
}
