using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ItemCarryObjective : IObjective
{
    [SerializeField] private int _rngMinDeliverableRequired = 1;
    [SerializeField] private int _rngMaxDeliverableRequired = 1;

    private NetworkVariable<int> n_totalItems = new NetworkVariable<int>();

    private NetworkVariable<int> n_itemsToDeliver = new NetworkVariable<int>();
    private NetworkVariable<int> n_storePrevItemsToDeliver = new NetworkVariable<int>();


    public override void OnNetworkSpawn()
    {
        SetItemsToDeliverServerRpc(Random.Range(_rngMinDeliverableRequired, _rngMaxDeliverableRequired + 1));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetItemsToDeliverServerRpc(int items)
    {
        n_itemsToDeliver.Value = items;

        n_totalItems.Value = n_itemsToDeliver.Value;
        MailboxTextHelper(n_itemsToDeliver.Value, n_totalItems.Value, n_totalItems.Value);
    }

    public override bool IsComplete()
    {
        if (n_itemsToDeliver.Value > 0)
        {
            return false;
        }
        return true;
    }

    // OnTriggerEnter check for item DeliverableQuestItem
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
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
        n_storePrevItemsToDeliver.Value = n_itemsToDeliver.Value;
        n_itemsToDeliver.Value -= obj.GetComponent<Item>().n_syncedQuantity.Value;
        NetworkObjectReference objRef = new NetworkObjectReference(obj.GetComponent<NetworkObject>());
        DespawnItemServerRpc(objRef);
        // ParticleManager.instance.SpawnSelfThenAll(1, obj.transform.position, Quaternion.Euler(0, 0, 0));
        MailboxTextHelper(n_itemsToDeliver.Value, n_totalItems.Value, n_storePrevItemsToDeliver.Value);
        if (n_itemsToDeliver.Value <= 0) {
            GameManager.instance.totalScore.Value += GameManager.instance.ScorePerObjective;
            Debug.Log("Total score " + GameManager.instance.totalScore.Value);

            n_storePrevItemsToDeliver.Value = n_itemsToDeliver.Value;
            n_itemsToDeliver.Value = 0;
            MailboxTextHelper(n_itemsToDeliver.Value, n_totalItems.Value, n_storePrevItemsToDeliver.Value);
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
