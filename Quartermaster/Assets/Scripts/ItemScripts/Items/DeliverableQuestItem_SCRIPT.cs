using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class DeliverableQuestItem_MONO : Item
{

    #region Deliverable Item Game Settings
    [Header("Deliverable Settings")]
    [SerializeField] private Tooltippable t;
    #endregion

    public override void OnButtonUse(GameObject user) {
        if (lastUsed + cooldown > Time.time) {
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

        lastUsed = Time.time;
    }

}
