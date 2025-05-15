using UnityEngine;
using System.Collections.Generic;

public class StimPack_MONO : Item
{
    #region ItemSettings
    [Header("StimPack Settings")]
    public float stimAspdMultiplier = 1.5f;
    public float stimMspdMultiplier = 1.3f;
    public float stimDuration = 10.0f;
    #endregion

    #region RuntimeVars
    #endregion 

    public override void OnButtonUse(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("StimPack_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        ImmediateStimPackUsage(user);
    }

    #region StimPackHelpers
    #endregion

    
    private void ImmediateStimPackUsage (GameObject user){
        Debug.Log ("StimPack_MONO: ImmediateStimPackUsage() called. Quantity before: " + quantity.ToString());
        quantity--;
        Debug.Log ("StimPack_MONO: ImmediateStimPackUsage() called. Quantity after: " + quantity.ToString());

        lastUsed = Time.time;
        // user.GetComponent<PlayerHealth>().Heal(HEAL_AMOUNT);
        // What handles health now?
        // Generate a quaternion for the particle effect to have no rotation
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s == null) {
            Debug.LogError("StimPack: ItemEffect: No status component found on user.");
            return;
        }

        s.ActivateStim(stimDuration, stimAspdMultiplier, stimMspdMultiplier);

        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));

    }


    #region GeneralHelpers
    #endregion 

    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("StimPack_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("StimPack_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("StimPack_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("StimPack_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}
