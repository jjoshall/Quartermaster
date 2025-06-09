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
    [Tooltip("Turret use cooldown")] public float turretCooldown = 0.25f;
    [Tooltip("Max stack size")] public int StackLimit = 1;
    [Tooltip("Weight per unit of item")] public float weight = 0.0f;
    [Tooltip("Current stack quantity, also modified during runtime")] public int quantity = 1;
    [Tooltip("Item icon")] public Texture icon = null;
    [Tooltip("OnUse sound emitters")] public SoundEmitter[] soundEmitters = null;
    [Tooltip("IsAutolootable")] public bool IsAutolootable = false;
    // [HideInInspector]                                                       public NetworkVariable<bool> CurrentlySelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #endregion






    #region RUNTIME
    // RUNTIME VARIABLES ===================================================================================================
    [HideInInspector] public GameObject userRef;
    [HideInInspector] public NetworkVariable<NetworkObjectReference> n_userRef = new NetworkVariable<NetworkObjectReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector] public bool IsPickedUp = false;
    [HideInInspector] public NetworkVariable<bool> n_isPickedUp = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector] public NetworkVariable<bool> n_isCurrentlySelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector] public GameObject attachedWeaponSlot = null;
    [HideInInspector] public NetworkVariable<int> n_syncedQuantity = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float GetLastUsed()
    {
        if (userRef == null)
        {
            Debug.LogError("GetLastUsed: userRef is null.");
            return 0f;
        }
        var ps = userRef.GetComponent<PlayerStatus>();
        if (ps == null)
        {
            Debug.LogError("GetLastUsed: PlayerStatus component not found on userRef.");
            return 0f;
        }
        return userRef.GetComponent<PlayerStatus>().GetLastUsed(uniqueID);
    }
    public void SetLastUsed(float time)
    {
        if (userRef == null)
        {
            Debug.LogError("SetLastUsed: userRef is null.");
            return;
        }
        var ps = userRef.GetComponent<PlayerStatus>();
        if (ps == null)
        {
            Debug.LogError("SetLastUsed: PlayerStatus component not found on userRef.");
            return;
        }
        userRef.GetComponent<PlayerStatus>().SetLastUsed(uniqueID, time);
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
    public override void OnNetworkSpawn()
    {
        // Called when the item is spawned on the server.
        //Debug.Log("Spawned item: " + gameObject.name);
        // owner client id
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        SyncLocalToServerServerRpc(clientId);
    }
    public virtual void OnSpawn()
    {
        SyncServerNetVarToLocal();
    }

    public virtual void OnPickUp(GameObject user)
    {
        // On pickup.
        //Debug.Log("Picked up item: " + gameObject.name);
        userRef = user;
        IsPickedUp = true;
        attachedWeaponSlot = GetWeaponSlot(userRef);

        SyncServerNetVarToLocal();
    }
    public virtual void OnDrop(GameObject user)
    {
        // On drop.
        //Debug.Log("Dropped item: " + gameObject.name);
        SyncServerNetVarToLocal();
    }
    public virtual void OnButtonUse(GameObject user)
    {
        // Fire once when use is pressed.
        //Debug.Log("Used item: " + gameObject.name);
    }
    public virtual void OnButtonHeld(GameObject user)
    {
        // Fire every frame when use is held.
        //Debug.Log("Held item: " + gameObject.name);
    }
    public virtual void OnButtonRelease(GameObject user)
    {
        // Fire once when use is released.
        //Debug.Log("Released item: " + gameObject.name);
    }

    public virtual void OnAltUse(GameObject user)
    {
        // Fire once when alt use is pressed.
        //Debug.Log("Alt used item: " + gameObject.name);
    }
    public virtual void OnAltHeld(GameObject user)
    {
        // Fire every frame when alt use is held.
        //Debug.Log("Alt held item: " + gameObject.name);
    }
    public virtual void OnAltRelease(GameObject user)
    {
        // Fire once when alt use is released.
        //Debug.Log("Alt released item: " + gameObject.name);
    }
    public virtual void OnSwapOut(GameObject user)
    {
        // Called when the item is swapped out.
        //Debug.Log("Switched items, charge cancelled for: " + gameObject.name);
    }

    // called when turret locks on target (facing target)
    // ex. start channeled behaviors such as flamethrower FlameStarted = true;
    public virtual void OnTurretStartLockon(GameObject turret)
    {
        //Debug.Log("Turret active: " + gameObject.name);
    }
    
    public virtual void TurretItemLoopBehavior(GameObject turret, float lastUsed)
    {

    }

    // called when turret no longer has a target.
    // ex. stop channeled behaviors such as flamethrower FlameStarted = false;
    public virtual void OnTurretStopLockon(GameObject turret)
    {
        //Debug.Log("Turret inactive: " + gameObject.name);
    }

    #endregion


    #region SYNCHELPERS
    // Helpers. Used by the UI cooldown script. =======================================================================
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0, GetLastUsed() + cooldown - Time.time);
    }
    public float GetMaxCooldown()
    {
        return cooldown;
    }


    // Sets server quantity to local quantity (passed as parameter).
    public void SyncServerNetVarToLocal()
    {
        if (userRef == null)
        {
            SyncServerToLocalServerRpc(quantity, IsPickedUp);
        }
        else
        {
            var userNetObj = userRef.GetComponent<NetworkObject>();
            if (userNetObj == null)
            {
                Debug.LogError("SyncServerNetVarToLocal: userRef is null.");
                return;
            }
            SyncServerToLocalServerRpc(quantity, IsPickedUp, userNetObj);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SyncServerToLocalServerRpc(int quantity, bool isPickedUp)
    {
        n_syncedQuantity.Value = quantity;
        n_isPickedUp.Value = isPickedUp;
        // SyncClientsToServer();
    }
    [ServerRpc(RequireOwnership = false)]
    private void SyncServerToLocalServerRpc(int quantity, bool isPickedUp, NetworkObjectReference userNetObj)
    {
        // Sync the local variables to the server.
        n_syncedQuantity.Value = quantity;
        n_isPickedUp.Value = isPickedUp;
        n_userRef.Value = userNetObj;
        // SyncClientsToServer();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncLocalToServerServerRpc(ulong clientId = 0)
    {
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        SyncLocalToServerClientRpc(rpcParams);
    }

    [ClientRpc]
    private void SyncLocalToServerClientRpc(ClientRpcParams clientRpcParams = default)
    {

        //Debug.Log("Syncing local to server for item: " + uniqueID);
        quantity = n_syncedQuantity.Value;
        IsPickedUp = n_isPickedUp.Value;
        bool hideItem = (!n_isCurrentlySelected.Value && IsPickedUp) ? true : false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = !hideItem;
        }
        userRef = GetGameObjectFromNetObj(n_userRef.Value);
        attachedWeaponSlot = GetWeaponSlot(userRef);
    }

    #endregion



    #region TURRETHELPERS
    #endregion

    public bool TurretNullChecksFailed(GameObject turret)
    {
        if (turret == null)
        {
            Debug.LogError("Pistol_MONO: TurretNullChecksFailed() turret is null.");
            return true;
        }
        return false;
    }
    public bool TurretCooldownCheckFailed(float lastUsed)
    {
        if (lastUsed + cooldown > Time.time)
        {
            return true;
        }
        return false;
    }

    public float GetLastUsedTurret(GameObject turret){
        return turret.GetComponent<TurretController_SCRIPT>()._timeLastUsed;
    }

    public void SetLastUsedTurret(GameObject turret, float time){
        // set turret's last used to time
        TurretController_SCRIPT tc = turret.GetComponent<TurretController_SCRIPT>();
        tc._timeLastUsed = time;
    }


    #region HELPERS
    protected void PlaySoundEmitter(string soundId)
    { 
        soundEmitters = GetComponents<SoundEmitter>();
        string emitterId = soundId;


        foreach (SoundEmitter emitter in soundEmitters)
        {
            if (emitter.emitterID == emitterId)
            {
                emitter.PlayNetworkedSound(transform.position);

            }
        }
    }

    private GameObject GetWeaponSlot(GameObject player)
    {
        if (player == null)
        {
            //Debug.Log("GetWeaponSlot: player is null.");
            return null;
        }
        Inventory inv = player.GetComponent<Inventory>();
        if (inv == null)
        {
            Debug.LogError("GetWeaponSlot: Inventory component not found on player.");
            return null;
        }
        GameObject weaponSlot = inv.weaponSlot;
        if (weaponSlot == null)
        {
            Debug.LogError("GetWeaponSlot: Weapon slot not found.");
            return null;
        }
        return weaponSlot;
    }
    private GameObject GetGameObjectFromNetObj(NetworkObjectReference netObj)
    {
        if (netObj.TryGet(out NetworkObject obj))
        {
            return obj.gameObject;
        }
        else
        {
            Debug.Log("GetGameObjectFromNetObj: Could not get game object from network object reference. UserRef set null.");
            return null;
        }
    }
    #endregion
}
