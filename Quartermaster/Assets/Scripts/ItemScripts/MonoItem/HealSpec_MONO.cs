using UnityEngine;
using System.Collections.Generic;

public class HealSpec_MONO : MonoItem
{

    #region HealSpec Item Game Settings
    [Header("HealSpec Settings")]
    [SerializeField] private float _healIncreasePerLevel = 0.1f;

    // Make an effect string = "" to disable spawning an effect.
    [SerializeField] private string _pickUpEffect = ""; // effect spawned on pickup.
    [SerializeField] private string _dropEffect = "";

    [SerializeField] private ParticleSystem persistentEffect; // particle system for flamethrower.
    #endregion


    #region InternalVars
    #endregion


    public override void PickUp(GameObject user)
    {
        UpdateHealSpecCount(user);
        // play pick up effect. 
    }

    public override void Drop(GameObject user)
    {
        UpdateHealSpecCount(user);
        // play drop effect.
    }

    #region Effect

    #endregion 
    #region HELPERS
    private void UpdateHealSpecCount (GameObject user) {
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        Inventory i = user.GetComponent<Inventory>();

        if (!s){
            Debug.LogError("HealSpec_MONO: UpdateHealSpecCount() - user or status is null.");
            return;
        }
        if (!i){
            Debug.LogError("HealSpec_MONO: UpdateHealSpecCount() - user or inventory is null.");
            return;
        }

        int quantity = i.GetItemQuantity(uniqueID);
        float bonus = quantity * _healIncreasePerLevel;

        s.UpdateHealSpecServerRpc(quantity, bonus);
    }

    #endregion
}
