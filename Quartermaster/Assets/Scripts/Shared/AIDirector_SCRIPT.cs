// NEXT THINGS TO DO:
// - Add scaling based on intensity number. Maybe >=400 intensity makes game a lot harder
//   because players will be really good if they hit that high of a number but if they are below 30 for a long time
//   then the game will be easier. This will make the game more dynamic and interesting.
// - Heavily increase enemy speed and enemy spawning during peak state (map is big so enemies should overwhelm players)
// - Maybe custom states where its only ranged enemies or explosive enemies?
// - Add something for player deaths (maybe increase intensity by 10% of current intensity)
// - ** DO FIRST** Every peak state is different (one peak state will add more health to enemies, another will increase speed,
//   maybe a combination of stuff, maybe it'l spawn like WAY more than normal
// - Make objectives the main intensity increaser or maybe if you start objective it goes straight to peak

using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class AIDirector : NetworkBehaviour {
    [Header("State Machine Settings")]
    [SerializeField] private float _buildUpDuration = 180f; // 3 minutes
    [SerializeField] private float _peakDuration = 30f; // 30 seconds
    [SerializeField] private float _relaxDuration = 45f; // 45 seconds

    [Header("Intensity Settings")]
    [SerializeField] private float _baseIntensity = 10f; // Base intensity level
    [SerializeField] private float _intensityDecayRate = 0.5f; // How fast intensity decreases over time
    [SerializeField] private float _peakIntensityThreshold = 60f; // When to trigger peak
    [SerializeField] private float _relaxIntensityThreshold = 30f; // When to end relax phase early

    [Header("Intensity Gain Multipliers")]
    [SerializeField] private float _enemyKillIntensity = .5f; // Intensity gained per kill
    [SerializeField] private float _damageDealtMultiplier = 0.25f; // Intensity gained per damage dealt
    [SerializeField] private float _damageTakenMultiplier = 0.25f; // Intensity gained per damage taken

    [Header("Spawn Rate Settings")]
    [SerializeField] private float _buildUpSpawnRate = 6f; // Seconds between spawns
    [SerializeField] private float _peakSpawnRate = 0.5f; // Seconds between spawns
    [SerializeField] private float _relaxSpawnRate = 10f; // Seconds between spawns

    [Header("Enemy Type Weights")]
    [SerializeField] private List<EnemyWeightData> _buildUpEnemyWeights = new List<EnemyWeightData>();
    [SerializeField] private List<EnemyWeightData> _peakEnemyWeights = new List<EnemyWeightData>();
    [SerializeField] private List<EnemyWeightData> _relaxEnemyWeights = new List<EnemyWeightData>();

    // State machine properties
    public NetworkVariable<DirectorState> _currentState = new NetworkVariable<DirectorState>(
        DirectorState.BuildUp,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> _currentIntensity = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> _stateTimeRemaining = new NetworkVariable<float>(
        180f, // Default to build up time
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // References
    private EnemySpawner _enemySpawner;
    private GameManager _gameManager;

    // Track last stats to calculate deltas
    private int _lastEnemyKills = 0;
    private float _lastDamageDealt = 0f;
    private float _lastDamageTaken = 0f;

    [System.Serializable]
    public class EnemyWeightData : INetworkSerializable {
        public Transform enemyPrefab;
        public EnemyType enemyType;
        public float spawnWeight = 1f;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref spawnWeight);

            if (serializer.IsReader) {
                int typeValue = 0;
                serializer.SerializeValue(ref typeValue);
                enemyType = (EnemyType)typeValue;
            }
            else {
                int typeValue = (int)enemyType;
                serializer.SerializeValue(ref typeValue);
            }
        }
    }

    public enum DirectorState {
        BuildUp,
        Peak,
        Relax
    }

    #region Setup

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        _enemySpawner = EnemySpawner.instance;
        _gameManager = GameManager.instance;

        // Start w/ build up phase
        _currentState.Value = DirectorState.BuildUp;
        _stateTimeRemaining.Value = _buildUpDuration;

        // Subscribe to game manager events
        _gameManager.totalEnemyKills.OnValueChanged += OnEnemyKillsChanged;
        _gameManager.totalDamageDealtToEnemies.OnValueChanged += OnDamageDealtChanged;
        _gameManager.totalPlayerDamageTaken.OnValueChanged += OnDamageTakenChanged;

        // Start the state machine
        StartCoroutine(StateMachineCoroutine());

        UpdateEnemySpawnerSettings();
    }

    public override void OnNetworkDespawn() {
        if (IsServer && _gameManager != null) {
            _gameManager.totalEnemyKills.OnValueChanged -= OnEnemyKillsChanged;
            _gameManager.totalDamageDealtToEnemies.OnValueChanged -= OnDamageDealtChanged;
            _gameManager.totalPlayerDamageTaken.OnValueChanged -= OnDamageTakenChanged;
        }
    }

    private void Update() {
        if (!IsServer) return;

        // Decay intensity over time?
        _currentIntensity.Value = Mathf.Max(0, _currentIntensity.Value - (_intensityDecayRate * Time.deltaTime));

        // Update UI with debug info
        //DebugUIClientRpc(_currentState.Value.ToString(), _currentIntensity.Value, _stateTimeRemaining.Value);
    }

    #endregion

    #region Event Handlers

    private void OnEnemyKillsChanged(int oldVal, int newVal)  {
        int killChange = newVal - _lastEnemyKills;
        _lastEnemyKills = newVal;

        if (killChange > 0) {
            AddIntensity(killChange * _enemyKillIntensity);
        }
    }

    private void OnDamageDealtChanged(float oldVal, float newVal) {
        float damageChange = newVal - _lastDamageDealt;
        _lastDamageDealt = newVal;

        if (damageChange > 0) {
            AddIntensity(damageChange * _damageDealtMultiplier);
        }
    }

    private void OnDamageTakenChanged(float oldVal, float newVal) {
        float damageChange = newVal - _lastDamageTaken;
        _lastDamageTaken = newVal;

        if (damageChange > 0) {
            AddIntensity(damageChange * _damageTakenMultiplier);
        }
    }

    #endregion

    #region State Machine

    private IEnumerator StateMachineCoroutine() {
        while (true) {
            // Update state time remaining
            _stateTimeRemaining.Value -= Time.deltaTime;

            switch (_currentState.Value) {
                case DirectorState.BuildUp:
                    // Check to see if we can transition to peak
                    if (_currentIntensity.Value >= _peakIntensityThreshold || _stateTimeRemaining.Value <= 0) {
                        TransitionToPeak();
                    }
                    break;

                case DirectorState.Peak:
                    // Check if peak is over
                    if (_stateTimeRemaining.Value <= 0) {
                        TransitionToRelax();
                    }
                    break;

                case DirectorState.Relax:
                    // Check if relax is over or intensity is low enough
                    if (_currentIntensity.Value <= _relaxIntensityThreshold || _stateTimeRemaining.Value <= 0) {
                        TransitionToBuildUp();
                    }
                    break;

            }
            yield return null;
        }
    }

    private void TransitionToBuildUp() {
        _currentIntensity.Value = _baseIntensity;
        _currentState.Value = DirectorState.BuildUp;
        _stateTimeRemaining.Value = _buildUpDuration;
        UpdateEnemySpawnerSettings();   

        Debug.Log("AI Director transitioning to Build Up phase");
    }

    private void TransitionToPeak() {
        _currentState.Value = DirectorState.Peak;
        _stateTimeRemaining.Value = _peakDuration;
        UpdateEnemySpawnerSettings();

        Debug.Log("AI Director transitioning to Peak phase");
    }

    private void TransitionToRelax() {
        _currentState.Value = DirectorState.Relax;
        _stateTimeRemaining.Value = _relaxDuration;
        UpdateEnemySpawnerSettings();

        Debug.Log("AI Director transitioning to Relax phase");
    }

    #endregion

    #region Enemy Spawning

    private void UpdateEnemySpawnerSettings() { 
        if (_enemySpawner == null) return;

        // Update spawn cooldown based on current state
        float spawnRate = _buildUpSpawnRate;
        DirectorState currentState = _currentState.Value;

        switch (currentState) {
            case DirectorState.BuildUp:
                spawnRate = _buildUpSpawnRate;
                break;

            case DirectorState.Peak:
                spawnRate = _peakSpawnRate;
                break;

            case DirectorState.Relax:
                spawnRate = _relaxSpawnRate;
                break;
        }

        // Scale w/ how many players are in the game
        float playerCountScaling = CalculatePlayerCountScaling();
        spawnRate /= playerCountScaling;

        UpdateSpawnerSettingsServerRpc(spawnRate, currentState);
    }

    private float CalculatePlayerCountScaling() {
        int playerCount = _gameManager.totalPlayers.Value;

        if (playerCount < 1) playerCount = 1;

        // More players = more enemies
        return Mathf.Sqrt(playerCount);
    }

    [ServerRpc]
    private void UpdateSpawnerSettingsServerRpc(float spawnRate, DirectorState state) {
        if (!IsServer) return;
        _enemySpawner._spawnCooldown = spawnRate;
        _enemySpawner._enemySpawnData.Clear();

        List<EnemyWeightData> weights;
        switch (state) {
            case DirectorState.BuildUp:
                weights = _buildUpEnemyWeights;
                break;

            case DirectorState.Peak:
                weights = _peakEnemyWeights;
                break;

            case DirectorState.Relax:
                weights = _relaxEnemyWeights;
                break;

            default:
                weights = _buildUpEnemyWeights;
                break;
        }

        foreach (var weightData in weights) {
            EnemySpawner.EnemySpawnData spawnData = new EnemySpawner.EnemySpawnData {
                enemyPrefab = weightData.enemyPrefab,
                spawnWeight = weightData.spawnWeight
            };

            _enemySpawner._enemySpawnData.Add(spawnData);
        }

        _enemySpawner.CalculateTotalWeight();
    }

    #endregion

    #region Intensity

    private void AddIntensity(float amount) {
        // Scale with players (less players = faster intensity gain)
        float playerCount = _gameManager.totalPlayers.Value;
        float playerScaling = 1f / Mathf.Sqrt(playerCount);
        _currentIntensity.Value += amount * playerScaling;
    }

    #endregion
}
