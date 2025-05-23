using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class DeliverableQuestItem_MONO : Item
{

    #region Deliverable Item Game Settings
    [Header("Deliverable Settings")]
    [SerializeField] private Tooltippable t;
    #endregion

    public override void OnNetworkSpawn() {
        GameObject found = GameObject.FindWithTag("Mailbox");
        if (found == null) {
            Destroy(gameObject); // auto destroys if no mailbox obj is found ingame. might cause lag? idk
        }
    }

    public override void OnButtonUse(GameObject user) {
        if (GetLastUsed() + cooldown > Time.time) {
            return;
        }

        if (t == null){
            t = this.GetComponent<Tooltippable>();
            if (t == null){
                Debug.LogError("DeliverableQuestItem_MONO: ButtonUse() - t is null.");
                return;
            }
        }

        // trigger tooltippable t
        t.SendMyTooltipTo(user.GetComponent<NetworkObject>().OwnerClientId);

        SetLastUsed(Time.time);
    }

}
