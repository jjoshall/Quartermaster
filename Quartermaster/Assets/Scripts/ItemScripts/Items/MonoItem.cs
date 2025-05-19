using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public abstract class Item : NetworkBehaviour
{

    #region SUBCLASS PROPERTIES
    // DEFINITIONS ========================================================================================
    [Header("Item Type")]
    [Tooltip("Used for player CD dict and IDing the item on pickup")] public string uniqueID = "";
    [Tooltip("Drops current held weapon when picking up second weapon")] public bool IsWeapon = false;
    [Tooltip("Causes item drop on picking up unique class spec")] public bool IsClassSpec = false;
    [Tooltip("Enables repeated use on holding left click")] public bool CanAutoFire = false;
    [Tooltip("Use cooldown")] public float cooldown = 0f;
    [Tooltip("Max stack size")] public int StackLimit = 1;
    [Tooltip("Weight per unit of item")] public float weight = 0.0f;
    [Tooltip("Current stack quantity, also modified during runtime")] public int quantity = 1;
    private NetworkVariable<int> n_syncedQuantity = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [Tooltip("Item icon")] public Texture icon = null;
    [Tooltip("OnUse sound emitters")] public SoundEmitter[] soundEmitters = null;
    [HideInInspector] public bool IsPickedUp = false;
    [HideInInspector] public NetworkVariable<bool> n_isPickedUp = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector] public NetworkVariable<bool> n_isCurrentlySelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector] public GameObject attachedWeaponSlot = null;
    // [HideInInspector]                                                       public NetworkVariable<bool> CurrentlySelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #endregion






    #region RUNTIME
    // RUNTIME VARIABLES ===================================================================================================
    [HideInInspector] public GameObject userRef;
    public float lastUsed
    {
        get => userRef.GetComponent<PlayerStatus>().GetLastUsed(uniqueID);
        set => userRef.GetComponent<PlayerStatus>().SetLastUsed(uniqueID, value);
    }

    void LateUpdate()
    {
        if (IsPickedUp)
            UpdateHoldablePosition(); // causes jitter if placed in Update() instead.
    }

    void UpdateHoldablePosition()
    {
        if (attachedWeaponSlot == null)
            return;

        // Update the position of the item to match the weapon slot.
        transform.position = attachedWeaponSlot.transform.position;
        transform.rotation = attachedWeaponSlot.transform.rotation;
        transform.Rotate(0, -90, 0); // add 90 degree y axis rotation

        // WIP: rotate for vertical camera view. 
    }

    #endregion




    #region FUNCTIONS
    // FUNCTION ABSTRACTS ===============================================================================================
    public virtual void OnNetworkSpawn()
    {
        // Called when the item is spawned on the server.
        Debug.Log("Spawned item: " + gameObject.name);
        SyncLocalToServerServerRpc();
    }
    public virtual void OnSpawn()
    {
        Debug.Log("Spawned item: " + gameObject.name);
        SyncServerNetVarToLocal();
        SyncClientsToServer();
    }

    public virtual void OnPickUp(GameObject user)
    {
        // On pickup.
        Debug.Log("Picked up item: " + gameObject.name);
        SyncServerNetVarToLocal();
        SyncClientsToServer();
    }
    public virtual void OnDrop(GameObject user)
    {
        // On drop.
        Debug.Log("Dropped item: " + gameObject.name);
        SyncServerNetVarToLocal();
        SyncClientsToServer();
    }
    public virtual void OnButtonUse(GameObject user)
    {
        // Fire once when use is pressed.
        Debug.Log("Used item: " + gameObject.name);
    }
    public virtual void OnButtonHeld(GameObject user)
    {
        // Fire every frame when use is held.
        Debug.Log("Held item: " + gameObject.name);
    }
    public virtual void OnButtonRelease(GameObject user)
    {
        // Fire once when use is released.
        Debug.Log("Released item: " + gameObject.name);
    }
    public virtual void OnSwapOut(GameObject user)
    {
        // Called when the item is swapped out.
        Debug.Log("Switched items, charge cancelled for: " + gameObject.name);
    }


    #endregion


    #region HELPERS
    // Helpers. Used by the UI cooldown script. =======================================================================
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0, (lastUsed + cooldown) - Time.time);
    }
    public float GetMaxCooldown()
    {
        return cooldown;
    }


    // Sets server quantity to local quantity (passed as parameter).
    public void SyncServerNetVarToLocal()
    {
        SyncServerToLocalServerRpc(quantity, IsPickedUp);
    }
    [ServerRpc(RequireOwnership = false)]
    private void SyncServerToLocalServerRpc(int quantity, bool isPickedUp)
    {
        n_syncedQuantity.Value = quantity;
        n_isPickedUp.Value = isPickedUp;
    }


    // Sets all client quantities to server quantity.
    public void SyncClientsToServer()
    {
        quantity = n_syncedQuantity.Value;
        IsPickedUp = n_isPickedUp.Value;
        SyncLocalToServerServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void SyncLocalToServerServerRpc()
    {
        SyncLocalToServerClientRpc();
    }
    [ClientRpc]
    private void SyncLocalToServerClientRpc()
    {
        quantity = n_syncedQuantity.Value;
        IsPickedUp = n_isPickedUp.Value;
        bool hideItem = (!n_isCurrentlySelected.Value && IsPickedUp) ? true : false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = !hideItem;
        }
    }
    #endregion
}
