using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour {
    #region InspectorSettings
    // Do not update these values during run-time. Set in inspector.
    [Header("Player Settings")]
    // HP and player speed set in the Player prefab settings.
    [SerializeField] private float _dropItemVelocity;

    [Header("Weapon Settings")]
    // Weapons should not scale at run-time. Scale enemy hp instead.
    [SerializeField] private float flame_Damage;
    [SerializeField] private float flame_Range;
    [SerializeField] private float flame_EndRadius; // The radius of the circle at the end of the flamethrower capsulecast
    [SerializeField] private float flame_Cooldown;
    [SerializeField] private string flame_EnemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    [SerializeField] private string flame_BarrelEffect = "PistolBarrelFire"; // effect at player

    [SerializeField] private float railgun_Damage;
    [SerializeField] private float railgun_AoeRadius;
    [SerializeField] private float railgun_Cooldown;

    [SerializeField] private float pistol_Damage;
    [SerializeField] private float pistol_Cooldown;

    [SerializeField] public GameObject bulletTracerPrefab;

    [Header("Item Settings")]
    [SerializeField] private float portalKey_Cooldown;
    [SerializeField] private float portalKey_TeleportRadius;

    [SerializeField] private float grenade_Damage;
    [SerializeField] private float grenade_AoeRadius;
    [SerializeField] private float grenade_ChargeTime;
    [SerializeField] private float grenade_MinVelocity;
    [SerializeField] private float grenade_MaxVelocity;
    [SerializeField] private float grenade_Cooldown;
    [SerializeField] private float grenade_ExpireTimer;
    [SerializeField] private int grenade_StackLimit;

    [SerializeField] private float slowTrap_SlowByPct;// give enemy a debuff flag. enemy checks for debuff then accesses this value to adjust their own speed.
    [SerializeField] private float slowTrap_Duration;   // duration of the trap itself. 
    [SerializeField] private float slowTrap_AoERadius;
    [SerializeField] private float slowTrap_Cooldown;
    [SerializeField] private int slowTrap_StackLimit;
    [SerializeField] private float slowTrap_ChargeTime;
    [SerializeField] private float slowTrap_MinVelocity;
    [SerializeField] private float slowTrap_MaxVelocity;

    [SerializeField] private float medKit_HealAmount ;
    [SerializeField] private float medKit_Cooldown ;
    [SerializeField] private int medKit_StackLimit;
    [SerializeField] private float _medKit_ChargeTime;
    [SerializeField] private float _medKit_TapThreshold;
    [SerializeField] private float _medKit_MinVelocity;
    [SerializeField] private float _medKit_MaxVelocity;
    [SerializeField] private float _medKit_ExpireTimer;

    [SerializeField] private int questItem_StackLimit;

    [SerializeField] private int _healSpec_StackLimit;
    [SerializeField] private float _healSpec_MultiplierPer;

    [SerializeField] private int _dmgSpec_StackLimit;
    [SerializeField] private float _dmgSpec_MultiplierPer;

    [SerializeField] private float[] _playersToEnemyHpMultiplier = new float[10] {  0.1f, 
                                                                                    1.0f, 
                                                                                    1.5f, 
                                                                                    2.0f, 
                                                                                    2.25f, 
                                                                                    2.5f, 
                                                                                    2.675f, 
                                                                                    2.75f, 
                                                                                    2.875f, 
                                                                                    3.0f }; // Multiplier for enemy hp based on player count

    [Header("BurstDropRate")]
    // WIP. MOVE BURST DROP RATE LOGIC HERE FROM ITEMMANAGER.
    public float burstDropRate; // chance of burst weapon dropping from enemy

    [Header("DramaFunction")]
    [SerializeField] private float _dramaOverTimeCoefficient = 0.1f;
    #endregion

    #region ReadonlyAccess
    public float DropItemVelocity => _dropItemVelocity;

    public float Flame_Damage => flame_Damage;
    public float Flame_Range => flame_Range;
    public float Flame_EndRadius => flame_EndRadius;
    public float Flame_Cooldown => flame_Cooldown;
    public string Flame_EnemyHitEffect => flame_EnemyHitEffect;
    public string Flame_BarrelEffect => flame_BarrelEffect;

    public float Railgun_Damage => railgun_Damage;
    public float Railgun_AoeRadius => railgun_AoeRadius;
    public float Railgun_Cooldown => railgun_Cooldown;

    public float Pistol_Damage => pistol_Damage;
    public float Pistol_Cooldown => pistol_Cooldown;

    public GameObject Pistol_TrailPrefab => bulletTracerPrefab;

    public float PortalKey_Cooldown => portalKey_Cooldown;
    public float PortalKey_TeleportRadius => portalKey_TeleportRadius;

    public float Grenade_Damage => grenade_Damage;
    public float Grenade_AoeRadius => grenade_AoeRadius;
    public float Grenade_ChargeTime => grenade_ChargeTime;
    public float Grenade_MinVelocity => grenade_MinVelocity;
    public float Grenade_MaxVelocity => grenade_MaxVelocity;
    public float Grenade_Cooldown => grenade_Cooldown;
    public float Grenade_ExpireTimer => grenade_ExpireTimer;
    public int Grenade_StackLimit => grenade_StackLimit;

    public float SlowTrap_SlowByPct => slowTrap_SlowByPct;
    public float SlowTrap_Duration => slowTrap_Duration;
    public float SlowTrap_Cooldown => slowTrap_Cooldown;
    public float SlowTrap_AoERadius => slowTrap_AoERadius;
    public int SlowTrap_StackLimit => slowTrap_StackLimit;
    public float SlowTrap_ChargeTime => slowTrap_ChargeTime;
    public float SlowTrap_MinVelocity => slowTrap_MinVelocity;
    public float SlowTrap_MaxVelocity => slowTrap_MaxVelocity;

    public float MedKit_HealAmount => medKit_HealAmount;
    public float MedKit_Cooldown => medKit_Cooldown;
    public int MedKit_StackLimit => medKit_StackLimit;
    public float MedKit_ChargeTime => _medKit_ChargeTime;
    public float MedKit_TapThreshold => _medKit_TapThreshold;
    public float MedKit_MinVelocity => _medKit_MinVelocity;
    public float MedKit_MaxVelocity => _medKit_MaxVelocity;
    public float MedKit_ExpireTimer => _medKit_ExpireTimer;

    public int QuestItem_StackLimit => questItem_StackLimit;

    public float HealSpec_MultiplierPer => _healSpec_MultiplierPer;
    public int HealSpec_StackLimit => _healSpec_StackLimit;

    public float DmgSpec_MultiplierPer => _dmgSpec_MultiplierPer;
    public int DmgSpec_StackLimit => _dmgSpec_StackLimit;

    #endregion

    #region RuntimeVariables

    [Header("Enemy Settings")]
    // Enemy hp should scale at run-time with difficulty and playercount. Base values set in the enemy Prefabs.
    public float _enemyHpMultiplier { get; private set; }     // multiplier for all enemy's hp values when spawned.
    public float enemySpeedMultiplier { get; private set; }  // enemy classes will need to have their own logic to propagate this value to all the navmeshagent parameters for speed.
    // individual enemy settings set in their prefabs.

    [Header("Game Statistics")]
    public NetworkVariable<int> totalEnemyKills = new NetworkVariable<int>(0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public NetworkVariable<float> totalDamageDealtToEnemies = new NetworkVariable<float>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> totalPlayerDamageTaken = new NetworkVariable<float>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("DramaFunction")]
    // placeholder variables. nothing set in stone, just brainstorming
    private float _drama = 0.0f;

    private float DramaCurveEnemyHpMultiplier (float drama){
        // Return some function based on time(?)/enemieskilled(?)/drama(?) to increase enemyHp.
        return 0.0f; // placeholder
    }
    private float DramaCurveBurstDropRate (float drama){
        return 0.0f;
    }

    #endregion

    #region GameStatistics
    [ServerRpc(RequireOwnership = false)]
    public void IncrementEnemyKillsServerRpc() {
        if (!IsServer) return;
        totalEnemyKills.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddEnemyDamageServerRpc(float damageAmount) {
        if (!IsServer) return;
        totalDamageDealtToEnemies.Value += damageAmount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerDamageServerRpc(float damageAmount) {
        if (!IsServer) return;
        totalPlayerDamageTaken.Value += damageAmount;
    }

    #endregion

    #region GameManagerSetup
    public NetworkList<NetworkObjectReference> n_players { get; private set;}
    public NetworkList<NetworkObjectReference> n_enemies { get; private set;}
    public NetworkList<NetworkObjectReference> n_worldItems { get; private set;}

    // singleton code
    public static GameManager instance;
    private void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(this);
        }
        InitializeRuntimeVars();
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        UpdatePlayerCount();
    }

    private void OnClientConnected(ulong clientId) {
        if (!IsServer) return;
        UpdatePlayerCount();
    }

    private void OnClientDisconnected(ulong clientId) {
        if (!IsServer) return;
        UpdatePlayerCount();
    }

    private void UpdatePlayerCount() {
        if (!IsServer) return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        totalPlayers.Value = count;

        //Debug.Log("Player count: " + count);
    }

    private void InitializeRuntimeVars() {
        n_players = new NetworkList<NetworkObjectReference>();
        n_enemies = new NetworkList<NetworkObjectReference>();
        n_worldItems = new NetworkList<NetworkObjectReference>();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    #endregion
}
