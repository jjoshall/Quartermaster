using UnityEngine;
using System.Collections.Generic;

public class DmgSpec_MONO : Item
{

    #region DmgSpec Item Game Settings
    [Header("DmgSpec Settings")]
    [SerializeField] private float _dmgIncreasePerLevel = 0.1f;

    // Make an effect string = "" to disable spawning an effect.
    [SerializeField] private string _pickUpEffect = ""; // effect spawned on pickup.
    [SerializeField] private string _dropEffect = "";

    [SerializeField] private ParticleSystem persistentEffect; // particle system for flamethrower.
    #endregion


    #region InternalVars
    #endregion


    public override void OnPickUp(GameObject user)
    {
        base.OnPickUp(user);
        UpdateDmgSpecCount(user);
        // play pick up effect. 
    }

    public override void OnDrop(GameObject user)
    {
        base.OnDrop(user);
        UpdateDmgSpecCount(user);
        // play drop effect.
    }

    #region Effect

    #endregion 
    #region HELPERS
    private void UpdateDmgSpecCount (GameObject user) {
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        Inventory i = user.GetComponent<Inventory>();

        if (!s){
            Debug.LogError("DmgSpec_MONO: UpdateDmgSpecCount() - user or status is null.");
            return;
        }
        if (!i){
            Debug.LogError("DmgSpec_MONO: UpdateDmgSpecCount() - user or inventory is null.");
            return;
        }

        int quantity = i.GetItemQuantity(uniqueID);
        float bonus = quantity * _dmgIncreasePerLevel;

        s.UpdateDmgSpecServerRpc(quantity, bonus);
    }

    #endregion
}
