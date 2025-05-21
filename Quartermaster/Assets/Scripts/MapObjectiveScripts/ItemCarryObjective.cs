using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ItemCarryObjective : IObjective
{
    [SerializeField] private int _rngMinDeliverableRequired = 1;
    [SerializeField] private int _rngMaxDeliverableRequired = 1;

    private NetworkVariable<int> n_itemsToDeliver = new NetworkVariable<int>();


    public override void OnNetworkSpawn()
    {
        SetItemsToDeliverServerRpc(Random.Range(_rngMinDeliverableRequired, _rngMaxDeliverableRequired + 1));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetItemsToDeliverServerRpc(int items)
    {
        n_itemsToDeliver.Value = items;
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
        Debug.Log("ItemCarry: OnTriggerEnter");
        // Item itema = other.gameObject.GetComponent<Item>();
        // Debug.Log("ItemCarry: Item is " + itema.uniqueID);
        if (other.gameObject.CompareTag("Item"))
        {
            Item item = other.gameObject.GetComponent<Item>();
            if (item == null)
            {
                Debug.Log("ItemCarry: Item is null");
                return;
            }
            if (item.IsPickedUp)
            {
                Debug.Log("ItemCarry: Item is already picked up");
                return;
            }

            if (item.uniqueID == "deliverable"){
                Debug.Log("ItemCarry: Item is DeliverableQuestItem");
                if (n_itemsToDeliver.Value > 0){
                    StartCoroutine (_DelayedDespawn(other.gameObject, 1.0f));  
                }   
            }
        }
}

    #region = DespawnItem
    private IEnumerator _DelayedDespawn(GameObject obj, float delay)
    {
        Debug.Log ("ItemCarry: _DelayedDespawn");
        obj.GetComponentInChildren<PlayerDissolveAnimator>().AnimateDissolveServerRpc();
        yield return new WaitForSeconds(delay);
        Debug.Log ("ItemCarry: Despawning item");
        if (obj == null) { yield break; }
        n_itemsToDeliver.Value--;
        NetworkObjectReference objRef = new NetworkObjectReference(obj.GetComponent<NetworkObject>());
        DespawnItemServerRpc(objRef);
        // ParticleManager.instance.SpawnSelfThenAll(1, obj.transform.position, Quaternion.Euler(0, 0, 0));
        if (n_itemsToDeliver.Value <= 0){
            ClearObjective();
        }
        yield return null;
    }
    #endregion

    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemServerRpc(NetworkObjectReference objRef)
    {
        if (objRef.TryGet(out NetworkObject obj))
        {
            obj.Despawn();
        }
    }
}
