using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Steamworks;

public class GameManager : NetworkBehaviour {
    #region InspectorSettings
    // Do not update these values during run-time. Set in inspector.
    [Header("Player Settings")]
    // HP and player speed set in the Player prefab settings.
    [SerializeField] private float _dropItemVelocity;
    [Header("Slow Trap Settings")]
    [SerializeField] private float slowTrap_SlowByPct;// give enemy a debuff flag. enemy checks for debuff then accesses this value to adjust their own speed.
    [SerializeField] private float slowTrap_Duration;   // duration of the trap itself. 
    [SerializeField] private float slowTrap_AoERadius;
    [SerializeField] private float slowTrap_Cooldown;
    [SerializeField] private int slowTrap_StackLimit;
    [SerializeField] private float slowTrap_ChargeTime;
    [SerializeField] private float slowTrap_MinVelocity;
    [SerializeField] private float slowTrap_MaxVelocity;

    #region Enemy Settings

    [Header("Melee Enemy Settings")]
    [SerializeField] private float meleeEnemy_Health;
    [SerializeField] private float meleeEnemy_AttackCooldown;
    [SerializeField] private float meleeEnemy_AttackRange;
    [SerializeField] private int meleeEnemy_AttackDamage;
    [SerializeField] private float meleeEnemy_AttackRadius;
    [SerializeField] private bool meleeEnemy_UseGlobalTarget;
    [SerializeField] private float meleeEnemy_Speed;

    [Header("Ranged Enemy Settings")]
    [SerializeField] private float rangedEnemy_Health;
    [SerializeField] private float rangedEnemy_AttackCooldown;
    [SerializeField] private float rangedEnemy_AttackRange;
    [SerializeField] private int rangedEnemy_AttackDamage;
    [SerializeField] private bool rangedEnemy_UseGlobalTarget;
    [SerializeField] private float rangedEnemy_Speed;

    [Header("Explosive Enemy Settings")]
    [SerializeField] private float explosiveEnemy_Health;
    [SerializeField] private float explosiveEnemy_AttackCooldown;
    [SerializeField] private float explosiveEnemy_AttackRange;
    [SerializeField] private int explosiveEnemy_AttackDamage;
    [SerializeField] private float explosiveEnemy_AttackRadius;
    [SerializeField] private bool explosiveEnemy_UseGlobalTarget;
    [SerializeField] private float explosiveEnemy_Speed;

    [Header("Big Melee Enemy Settings")]
    [SerializeField] private float bigMeleeEnemy_Health;
    [SerializeField] private float bigMeleeEnemy_AttackCooldown;
    [SerializeField] private float bigMeleeEnemy_AttackRange;
    [SerializeField] private int bigMeleeEnemy_AttackDamage;
    [SerializeField] private float bigMeleeEnemy_AttackRadius;
    [SerializeField] private bool bigMeleeEnemy_UseGlobalTarget;
    [SerializeField] private float bigMeleeEnemy_Speed;

    [Header("Small Exploding Enemy Settings")]
    [SerializeField] private float smallExplosiveEnemy_Health;
    [SerializeField] private float smallExplosiveEnemy_AttackCooldown;
    [SerializeField] private float smallExplosiveEnemy_AttackRange;
    [SerializeField] private int smallExplosiveEnemy_AttackDamage;
    [SerializeField] private float smallExplosiveEnemy_AttackRadius;
    [SerializeField] private bool smallExplosiveEnemy_UseGlobalTarget;
    [SerializeField] private float smallExplosiveEnemy_Speed;

    #endregion

    [SerializeField]
    private float[] _playersToEnemyHpMultiplier = new float[10] {  0.1f,
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

    //[Header("DramaFunction")]
    //[SerializeField] private float _dramaOverTimeCoefficient = 0.1f;
    #endregion

    #region ReadonlyAccess
    public float DropItemVelocity => _dropItemVelocity;
    public float SlowTrap_SlowByPct => slowTrap_SlowByPct;
    public float SlowTrap_Duration => slowTrap_Duration;
    public float SlowTrap_Cooldown => slowTrap_Cooldown;
    public float SlowTrap_AoERadius => slowTrap_AoERadius;
    public int SlowTrap_StackLimit => slowTrap_StackLimit;
    public float SlowTrap_ChargeTime => slowTrap_ChargeTime;
    public float SlowTrap_MinVelocity => slowTrap_MinVelocity;
    public float SlowTrap_MaxVelocity => slowTrap_MaxVelocity;

    public float EnemySpeedMultiplier { get; set; } = 1.0f;
    public float EnemyHealthMultiplier { get; set; } = 1.0f;
    public float SpawnRateMultiplier { get; set; } = 0.0f;

    public float MeleeEnemy_Health => meleeEnemy_Health * EnemyHealthMultiplier;
    public float MeleeEnemy_AttackCooldown => meleeEnemy_AttackCooldown;
    public float MeleeEnemy_AttackRange => meleeEnemy_AttackRange;
    public int MeleeEnemy_AttackDamage => meleeEnemy_AttackDamage;
    public float MeleeEnemy_AttackRadius => meleeEnemy_AttackRadius;
    public bool MeleeEnemy_UseGlobalTarget => meleeEnemy_UseGlobalTarget;
    public float MeleeEnemy_Speed => meleeEnemy_Speed * EnemySpeedMultiplier;

    public float RangedEnemy_Health => rangedEnemy_Health * EnemyHealthMultiplier;
    public float RangedEnemy_AttackCooldown => rangedEnemy_AttackCooldown;
    public float RangedEnemy_AttackRange => rangedEnemy_AttackRange;
    public int RangedEnemy_AttackDamage => rangedEnemy_AttackDamage;
    public bool RangedEnemy_UseGlobalTarget => rangedEnemy_UseGlobalTarget;
    public float RangedEnemy_Speed => rangedEnemy_Speed * EnemySpeedMultiplier;

    public float ExplosiveEnemy_Health => explosiveEnemy_Health * EnemyHealthMultiplier;
    public float ExplosiveEnemy_AttackCooldown => explosiveEnemy_AttackCooldown;
    public float ExplosiveEnemy_AttackRange => explosiveEnemy_AttackRange;
    public int ExplosiveEnemy_AttackDamage => explosiveEnemy_AttackDamage;
    public float ExplosiveEnemy_AttackRadius => explosiveEnemy_AttackRadius;
    public bool ExplosiveEnemy_UseGlobalTarget => explosiveEnemy_UseGlobalTarget;
    public float ExplosiveEnemy_Speed => explosiveEnemy_Speed * EnemySpeedMultiplier;

    public float BigMeleeEnemy_Health => bigMeleeEnemy_Health * EnemyHealthMultiplier;
    public float BigMeleeEnemy_AttackCooldown => bigMeleeEnemy_AttackCooldown;
    public float BigMeleeEnemy_AttackRange => bigMeleeEnemy_AttackRange;
    public int BigMeleeEnemy_AttackDamage => bigMeleeEnemy_AttackDamage;
    public float BigMeleeEnemy_AttackRadius => bigMeleeEnemy_AttackRadius;
    public bool BigMeleeEnemy_UseGlobalTarget => bigMeleeEnemy_UseGlobalTarget;
    public float BigMeleeEnemy_Speed => bigMeleeEnemy_Speed * EnemySpeedMultiplier;

    public float SmallExplosiveEnemy_Health => smallExplosiveEnemy_Health * EnemyHealthMultiplier;
    public float SmallExplosiveEnemy_AttackCooldown => smallExplosiveEnemy_AttackCooldown;
    public float SmallExplosiveEnemy_AttackRange => smallExplosiveEnemy_AttackRange;
    public int SmallExplosiveEnemy_AttackDamage => smallExplosiveEnemy_AttackDamage;
    public float SmallExplosiveEnemy_AttackRadius => smallExplosiveEnemy_AttackRadius;
    public bool SmallExplosiveEnemy_UseGlobalTarget => smallExplosiveEnemy_UseGlobalTarget;
    public float SmallExplosiveEnemy_Speed => smallExplosiveEnemy_Speed * EnemySpeedMultiplier;

    public int ScorePerObjective => scorePerObjective;
    public int ScorePerEnemyKill => scorePerEnemyKill;
    public int ScorePerPlayerHeal => scorePerPlayerHeal;
    public int ScorePenaltyOnDeath => scorePenaltyOnDeath;

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

    public NetworkVariable<int> totalPlayerDeaths = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> totalPlayers = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> totalScore = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> stopwatchTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private bool timerRunning = false;

    [Header("Score Values")]
    [SerializeField] private int scorePerObjective = 1000;
    [SerializeField] private int scorePerEnemyKill = 10;
    [SerializeField] private int scorePerPlayerHeal = 5;
    [SerializeField] private int scorePenaltyOnDeath = -150;

    //[Header("DramaFunction")]
    //// placeholder variables. nothing set in stone, just brainstorming
    //private float _drama = 0.0f;

    //private float DramaCurveEnemyHpMultiplier (float drama){
    //    // Return some function based on time(?)/enemieskilled(?)/drama(?) to increase enemyHp.
    //    return 0.0f; // placeholder
    //}
    //private float DramaCurveBurstDropRate (float drama){
    //    return 0.0f;
    //}

    #endregion

    #region Steam Stuff
    [Header("Steam Callbacks")]
    protected Callback<LeaderboardFindResult_t> leaderboardFindResult;

    private string pendingLeaderboardName;
    private int pendingScore;




    [ClientRpc]
    public void SetSteamLeaderboardClientRpc(string leaderboardName, int score) {
        pendingLeaderboardName = leaderboardName;
        pendingScore = score;


        SteamUserStats.FindLeaderboard(leaderboardName);

        Debug.Log($"Setting Steam leaderboard score for {leaderboardName} to {score}");
    }

    public void OnLeaderboardFindResult(LeaderboardFindResult_t result) {
        if (result.m_bLeaderboardFound == 1) {
            Debug.Log($"Leaderboard found: {result.m_hSteamLeaderboard}");

            SteamUserStats.UploadLeaderboardScore(
                result.m_hSteamLeaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                pendingScore,
                null,
                0
            );

            Debug.Log($"Score {pendingScore} uploaded to leaderboard {pendingLeaderboardName}");

        }
        else {
            Debug.LogError("Leaderboard not found.");
        }
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

    [ServerRpc(RequireOwnership = false)]
    public void IncrementPlayerDeathsServerRpc() {
        if (!IsServer) return;
        totalPlayerDeaths.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int score) {
        if (!IsServer) return;
        totalScore.Value += score;
    }

    #endregion

    #region GameManagerSetup
    public NetworkList<NetworkObjectReference> n_players { get; private set; }
    public NetworkList<NetworkObjectReference> n_enemies { get; private set; }
    public NetworkList<NetworkObjectReference> n_worldItems { get; private set; }

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
        leaderboardFindResult = Callback<LeaderboardFindResult_t>.Create(OnLeaderboardFindResult);
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

    private void Update() {
        if (!IsServer || !timerRunning) {
            return;
        }

        stopwatchTime.Value += Time.deltaTime;
    }

    public void ResetTimer() {
        if (!IsServer) {
            return;
        }
        stopwatchTime.Value = 0f;
    }

    public void StopTimer() {
        if (!IsServer) {
            return;
        }
        timerRunning = false;
    }

    public void StartTimer() {
        if (!IsServer) {
            return;
        }
        timerRunning = true;
    }

    private void UpdatePlayerCount() {
        if (!IsServer) return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        totalPlayers.Value = count;

    }

    private void InitializeRuntimeVars() {
        n_players = new NetworkList<NetworkObjectReference>();
        n_enemies = new NetworkList<NetworkObjectReference>();
        n_worldItems = new NetworkList<NetworkObjectReference>();
    }

    public override void OnNetworkDespawn() {
        if (!IsServer || NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    #endregion
}
