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
    public NetworkVariable<bool> n_stimActive = new NetworkVariable<bool>(false); // runtime var
    public NetworkVariable<bool> n_healBuffActive = new NetworkVariable<bool>(false); // runtime var
    public NetworkVariable<bool> n_dmgBuffActive = new NetworkVariable<bool>(false);

    // Effect values. TREAT AS CONSTANTS. Initialize from GameManager.
    public float stimAspdMultiplier = 1.0f;
    public float stimMspdMultiplier = 1.0f;
    public float stimDuration = 1.0f;
    public float healMultiplier = 1.0f;
    public float healThrowVelocity = 0.0f;
    public float dmgMultiplier = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public float GetLastUsed(int itemID){
        return _lastUsed[itemID];
    }

    public void SetLastUsed(int itemID, float time){
        _lastUsed[itemID] = time;
    }

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
