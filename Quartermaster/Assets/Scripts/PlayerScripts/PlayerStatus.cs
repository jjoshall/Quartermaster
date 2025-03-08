using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerStatus : NetworkBehaviour
{
    // Cooldown
    public NetworkList<float> n_lastUsed = new NetworkList<float>();

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
            n_lastUsed.Add(float.MinValue);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
