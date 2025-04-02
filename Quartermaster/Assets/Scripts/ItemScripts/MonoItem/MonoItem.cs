using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public abstract class MonoItem : NetworkBehaviour
{
   // Static dictionary mapping each item type to a unique id. 
   // NOTE: Unique Type/ID maps constructed locally, and used locally for player cooldown lookup.
   //       Do not try to sync these IDs across network.
    // private static Dictionary<Type, int> typeToIdMap = new Dictionary<Type, int>();
    // private static int nextId = 1;

    // // Unique id for this item’s type.
    // public int UniqueTypeId { get; private set; } = 0;

    // protected virtual void Awake()
    // {
    //     // Register or retrieve a unique id for this item type.
    //     Type itemType = this.GetType();
    //     if (!typeToIdMap.TryGetValue(itemType, out int id))
    //     {
    //         id = nextId++;
    //         typeToIdMap[itemType] = id;
    //     }
    //     UniqueTypeId = id;
    // }

    #region SUBCLASS PROPERTIES
    // DEFINITIONS ========================================================================================
    [Header("Item Type")]
    [Tooltip("Used for player CD dict and IDing the item on pickup")]       public string uniqueID = "";
    [Tooltip("Causes quantity-- on use.")]                                  public bool IsConsumable = false;
    [Tooltip("Drops current held weapon when picking up second weapon")]    public bool IsWeapon = false;
    [Tooltip("Causes item drop on picking up unique class spec")]           public bool IsClassSpec = false;
    [Tooltip("Enables repeated use on holding left click")]                 public bool CanAutoFire = false;
    [Tooltip("Use cooldown")]                                               public float cooldown = 0f;
    [Tooltip("Max stack size")]                                             public int StackLimit = 1;
    [Tooltip("Current stack quantity, also modified during runtime")]       public int quantity = 1;
    [Tooltip("Item icon")]                                                  public Texture icon = null;
    [Tooltip("OnUse sound emitters")]                                       public SoundEmitter[] soundEmitters = null;
    [HideInInspector]                                                       public bool IsPickedUp = false;
    [HideInInspector]                                                       public GameObject attachedWeaponSlot = null;

    #endregion






    #region RUNTIME
    // RUNTIME VARIABLES ===================================================================================================
    [HideInInspector] public GameObject userRef;
    public float lastUsed {
        get => userRef.GetComponent<PlayerStatus>().GetLastUsed(uniqueID);
        set => userRef.GetComponent<PlayerStatus>().SetLastUsed(uniqueID, value);
    }

    void LateUpdate(){
        if (IsPickedUp)
            UpdateHoldablePosition(); // causes jitter if placed in Update() instead.
    }

    void UpdateHoldablePosition(){
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
    public virtual void PickUp(GameObject user){
        // On pickup.
        Debug.Log("Picked up item: " + gameObject.name);
    }
    public virtual void Drop(GameObject user){
        // On drop.
        Debug.Log("Dropped item: " + gameObject.name);
    }
    public virtual void ButtonUse(GameObject user){
        // Fire once when use is pressed.
        Debug.Log("Used item: " + gameObject.name);
    }
    public virtual void ButtonHeld(GameObject user){
        // Fire every frame when use is held.
        Debug.Log("Held item: " + gameObject.name);
    }
    public virtual void ButtonRelease(GameObject user){
        // Fire once when use is released.
        Debug.Log("Released item: " + gameObject.name);
    }


    #endregion









    #region HELPERS
    // Helpers. Used by the UI cooldown script. =======================================================================
    public float GetCooldownRemaining(){
        return Mathf.Max(0, (lastUsed + cooldown) - Time.time);
    }
    public float GetMaxCooldown(){
        return cooldown;
    }

    #endregion
}
