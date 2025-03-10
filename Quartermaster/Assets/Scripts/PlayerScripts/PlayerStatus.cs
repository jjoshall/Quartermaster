using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerStatus : NetworkBehaviour
{

    // Cooldown
    private List<float> _lastUsed = new List<float>();

    // Status Effects
    private NetworkVariable<bool> n_stimActive = new NetworkVariable<bool>(false); // runtime var
    public NetworkVariable<bool> n_healBuffActive = new NetworkVariable<bool>(false); // runtime var
    public NetworkVariable<bool> n_dmgBuffActive = new NetworkVariable<bool>(false);

    // Carried SpecItems   
    private NetworkVariable<int> n_healSpecLvl = new NetworkVariable<int>(0);
    private NetworkVariable<int> n_dmgSpecLvl = new NetworkVariable<int>(0);
    // 172 stuff: private NetworkList<int> n_tankSpecLvl = new NetworkList<int>(); // increase aggro range, hp, movespeed.


    // Effect values. TREAT AS CONSTANTS. Initialize from GameManager.
    public float stimAspdMultiplier = 1.0f;
    public float stimMspdMultiplier = 1.0f;
    public float stimDuration = 1.0f;
    private float _stimTimer = 0.0f;
    public float healMultiplier = 1.0f;
    public float healThrowVelocity = 0.0f;
    public float dmgMultiplier = 1.0f;


    #region Startup
    public override void OnNetworkSpawn(){
        InitValuesFromGameManager();
        InitLastUsedList();
    }

    void InitValuesFromGameManager(){

    }

    void InitLastUsedList(){
        // n_lastUsed = new NetworkList<float>();
        for (int i = 0; i < ItemManager.instance.itemEntries.Count; i++)
        {
            _lastUsed.Add(float.MinValue);
        }
    }


    #endregion 
    #region Update
    // Update is called once per frame
    void Update()
    {
        if (n_stimActive.Value && _stimTimer > 0.0f){
            _stimTimer -= Time.deltaTime;
        } else {
            DeactivateStimServerRpc();
        }
    }


    #endregion

    #region Stim
    [ServerRpc(RequireOwnership = false)]
    public void ActivateStimServerRpc(){
        n_stimActive.Value = true;
        _stimTimer = stimDuration;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeactivateStimServerRpc(){
        n_stimActive.Value = false;
        stimAspdMultiplier = 1.0f;
        stimMspdMultiplier = 1.0f;
    }

    #endregion
    #region CooldownHelpers
    public float GetLastUsed(int itemID){
        return _lastUsed[itemID];
    }

    public void SetLastUsed(int itemID, float time){
        _lastUsed[itemID] = time;
    }

    #endregion
    #region SpecItems
    // Getters
    public int GetHealSpecLvl(){
        return n_healSpecLvl.Value;
    } 
    public int GetDmgSpecLvl(){
        return n_dmgSpecLvl.Value;
    }
    // Pickup Specitems
    [ServerRpc(RequireOwnership = false)]
    public void UpdateDmgSpecServerRpc(int quantity){
        n_dmgSpecLvl.Value = quantity;
    }
    [ServerRpc(RequireOwnership = false)]
    public void UpdateHealSpecServerRpc(int quantity){
        n_healSpecLvl.Value = quantity;
    }

    #endregion
}
