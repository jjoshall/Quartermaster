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

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AIDirector : NetworkBehaviour {
    public enum PeakVariation {
        HealthBoost,
        SpeedBoost,
        SpawnRush,
        Mixed
    }

    [Header("DRAMA PHASE timeout transition triggers")]
    [SerializeField] private float _buildUpDuration = 180f; // 3 minutes
    [SerializeField] private float _peakDuration = 30f; // 30 seconds
    [SerializeField] private float _relaxDuration = 45f; // 45 seconds

    [Header("DRAMA PHASE intensity trigger settings")]
    [SerializeField] private float _baseIntensity = 10f; // Base intensity level
    [SerializeField] private float _intensityDecayRate = 0.5f; // How fast intensity decreases over time
    [SerializeField] private float _peakIntensityThreshold = 60f; // When to trigger peak
    [SerializeField] private float _relaxIntensityThreshold = 30f; // When to end relax phase early

    [Header("DRAMA PHASE intensity gain modifiers")]
    [SerializeField] private float _enemyKillIntensity = 0.5f; // Intensity gained per kill
    [SerializeField] private float _damageDealtMultiplier = 0.25f; // Intensity gained per damage dealt
    [SerializeField] private float _damageTakenMultiplier = 0.25f; // Intensity gained per damage taken

    [Header("DRAMA PHASE BASED spawn rates")]
    [SerializeField] private float _buildUpSpawnRate = 6f; // Seconds between spawns
    [SerializeField] private float _peakSpawnRate = 0.5f; // Seconds between spawns
    [SerializeField] private float _relaxSpawnRate = 10f; // Seconds between spawns

    [Header("DRAMA PHASE BASED enemytype spawn weights")]
    [SerializeField] private List<EnemyWeightData> _buildUpEnemyWeights = new List<EnemyWeightData>();
    [SerializeField] private List<EnemyWeightData> _peakEnemyWeights = new List<EnemyWeightData>();
    [SerializeField] private List<EnemyWeightData> _relaxEnemyWeights = new List<EnemyWeightData>();

    [Header("TIME BASED permanent scaling")]
    // These settings are the original gradual values.
    [SerializeField] private float _scalingIncrement = 0.05f; // Increment of scaling each peak
    private float _scalingRawTotal = 0.0f; // value before curve.
    private float _lastScaled = 0.0f; // timer. 
    [SerializeField] private float _scalingIntervalSeconds = 30.0f; // timer.
    
    [SerializeField] private float _maxSpeedScale = 0.3f; // 20% max speed increase
    [SerializeField] private float _maxHealthScale = 0.5f; // 50% max health increase
    [SerializeField] private float _maxDamageScale = 0.5f; // 50% max damage increase
    [SerializeField] private float _maxSpawnRateScale = 0.5f; // 50% max spawn rate increase

    [SerializeField] private float _peakBuffMultiplier = 2f;

    // State machine properties
    public enum DirectorState {
        BuildUp,
        Peak,
        Relax
    }
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

    private int _lastEnemyKills = 0;
    private float _lastDamageDealt = 0f;
    private float _lastDamageTaken = 0f;

    private PeakVariation _currentPeakVariation;

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
            } else {
                int typeValue = (int)enemyType;
                serializer.SerializeValue(ref typeValue);
            }
        }
    }

    #region Setup

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        _enemySpawner = EnemySpawner.instance;
        _gameManager = GameManager.instance;

        _currentState.Value = DirectorState.BuildUp;
        _stateTimeRemaining.Value = _buildUpDuration;

        _gameManager.totalEnemyKills.OnValueChanged += OnEnemyKillsChanged;
        _gameManager.totalDamageDealtToEnemies.OnValueChanged += OnDamageDealtChanged;
        _gameManager.totalPlayerDamageTaken.OnValueChanged += OnDamageTakenChanged;


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

        // Exit if all players in rest area (either inventory or start room), 
        // Managed by colliders adding/removing from activePlayerList
        if (EnemySpawner.instance.activePlayerList.Count <= 0){
            _lastScaled += Time.deltaTime;
            return;
        }

        _currentIntensity.Value = Mathf.Max(0, _currentIntensity.Value - (_intensityDecayRate * Time.deltaTime));
        
        // Phase state machine. 
        // Transitions on intensity thresholds or timeouts.
        // Cyclical. BuildUp -> Peak -> Relax -> BuildUp
        StateMachineTimer();

        // Increase enemy scaling on timer. 
        // Enemy scaling is asymptotic to max values at top of this file.
        if (Time.time > _lastScaled + _scalingIntervalSeconds){
            IncreaseEnemyScaling();
            _lastScaled = Time.time;
        }
    }

    #endregion

    #region Event Handlers

    private void OnEnemyKillsChanged(int oldVal, int newVal) {
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

    private void StateMachineTimer() {
        _stateTimeRemaining.Value -= Time.deltaTime;

        switch (_currentState.Value) {
            case DirectorState.BuildUp:
                if (_currentIntensity.Value >= _peakIntensityThreshold || _stateTimeRemaining.Value <= 0) {
                    TransitionToPeak();
                }
                break;
            case DirectorState.Peak:
                if (_stateTimeRemaining.Value <= 0) {
                    TransitionToRelax();
                }
                break;
            case DirectorState.Relax:
                if (_currentIntensity.Value <= _relaxIntensityThreshold || _stateTimeRemaining.Value <= 0) {
                    TransitionToBuildUp();
                }
                break;
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

        // System.Array variations = System.Enum.GetValues(typeof(PeakVariation));
        // _currentPeakVariation = (PeakVariation)variations.GetValue(Random.Range(0, variations.Length));
        // ApplyPeakVariation(_currentPeakVariation);

        UpdateEnemySpawnerSettings();

        Debug.Log($"AI Director transitioning to Peak phase - Variation: {_currentPeakVariation}, " +
                  $"Speed: +{GameManager.instance.EnemySpeedMultiplier * 100 - 100:F1}%, " +
                  $"Health: +{GameManager.instance.EnemyHealthMultiplier * 100 - 100:F1}%, " +
                  $"Spawn Rate Scale: x{1f / (1f - GameManager.instance.SpawnRateMultiplier):F2}");
    }

    private void TransitionToRelax() {
        _currentState.Value = DirectorState.Relax;
        _stateTimeRemaining.Value = _relaxDuration;
        UpdateEnemySpawnerSettings();
        Debug.Log("AI Director transitioning to Relax phase");
    }

    #endregion

    #region Difficulty Scaling

    private void IncreaseEnemyScaling() { // increase periodically enemy hp scaling based on time.
        if (!IsServer) return;
        Debug.Log ("increasing enemy scaling");

        _scalingRawTotal += _scalingIncrement;
        float curvedHp = ScaledEaseOut(_scalingRawTotal, _maxHealthScale);


        float curvedDmg = ScaledEaseOut(_scalingRawTotal, _maxDamageScale);
        float curvedSpeed = ScaledEaseOut(_scalingRawTotal, _maxSpeedScale);

        float curvedSpawnRate = ScaledEaseOut(_scalingRawTotal, _maxSpawnRateScale);

        // affects new spawns.
        EnemySpawner.instance.aiDmgMultiplier = 1.0f + curvedDmg;
        EnemySpawner.instance.aiHpMultiplier = 1.0f + curvedHp;

        // affects all active enemies
        UpdateEnemySpeedMultiplier(1.0f + curvedSpeed);

        // float currentSpeedScale = gm.EnemySpeedMultiplier - 1f;
        // float currentHealthScale = gm.EnemyHealthMultiplier - 1f;
        // float currentSpawnRateScale = gm.SpawnRateMultiplier;

        // float newSpeedScale = ApplyDiminishingReturns(currentSpeedScale + scalingIncrement, _maxSpeedScale);
        // float newHealthScale = ApplyDiminishingReturns(currentHealthScale + scalingIncrement, _maxHealthScale);
        // float newSpawnRateScale = ApplyDiminishingReturns(currentSpawnRateScale + scalingIncrement, _maxSpawnRateScale);

        // gm.EnemySpeedMultiplier = 1f + newSpeedScale;
        // gm.EnemyHealthMultiplier = 1f + newHealthScale;
        // gm.SpawnRateMultiplier = newSpawnRateScale;
        Debug.Log ("scaling raw total: " + _scalingRawTotal);   
    }

    // Asymptotic easing function. 1 - e^(-x). Scaled to maxValue.
    private float ScaledEaseOut(float x, float maxValue) {
        float xCoefficient = -0.1f; // -0.1f returns ~0.9 at x = 24
        float y = 1f - Mathf.Pow(2.71828f, xCoefficient * x);
        return y * maxValue;
    }
    private void UpdateEnemySpeedMultiplier (float speedMultiplier){
        EnemySpawner.instance.UpdateEnemySpeed(speedMultiplier);
    }

    // private void ApplyPeakVariation(PeakVariation variation) {
    //     GameManager gm = GameManager.instance;
    //     float baseIncrement = _scalingIncrement;

    //     switch (variation) {
    //         case PeakVariation.HealthBoost:
    //             {
    //                 float newHealthScale = ScaledEaseOut(baseIncrement * _peakBuffMultiplier, _maxHealthScale);
    //                 gm.EnemyHealthMultiplier = 1f + newHealthScale;
    //             }
    //             break;
    //         case PeakVariation.SpeedBoost:
    //             {
    //                 float newSpeedScale = ScaledEaseOut(baseIncrement * _peakBuffMultiplier, _maxSpeedScale);
    //                 // gm.EnemySpeedMultiplier = 1f + newSpeedScale;
    //                 UpdateEnemySpeedMultiplier(1f + newSpeedScale);
    //             }
    //             break;
    //         case PeakVariation.SpawnRush:
    //             {
    //                 float newSpawnRateScale = ScaledEaseOut(baseIncrement * _peakBuffMultiplier, _maxSpawnRateScale);
    //                 gm.SpawnRateMultiplier = newSpawnRateScale;
    //             }
    //             break;
    //         case PeakVariation.Mixed:
    //             {
    //                 IncreaseEnemyScaling();
    //             }
    //             break;
    //     }
    // }

    #endregion


    #region Enemy Spawning

    private void UpdateEnemySpawnerSettings() {
        if (_enemySpawner == null) return;

        float currBaseSpawnInterval = _buildUpSpawnRate;
        DirectorState currentState = _currentState.Value;

        switch (currentState) {
            case DirectorState.BuildUp:
                currBaseSpawnInterval = _buildUpSpawnRate;
                break;
            case DirectorState.Peak:
                currBaseSpawnInterval = _peakSpawnRate;
                break;
            case DirectorState.Relax:
                currBaseSpawnInterval = _relaxSpawnRate;
                break;
        }

        float playerCountScaling = CalculatePlayerCountScaling(); 
        currBaseSpawnInterval /= playerCountScaling; // <---- why are we scaling spawnrate DOWN based on playercount?

        if (currentState == DirectorState.BuildUp || currentState == DirectorState.Peak) {
            currBaseSpawnInterval *= (1f - GameManager.instance.SpawnRateMultiplier);
        }
        UpdateSpawnerSettingsServerRpc(currBaseSpawnInterval, currentState);
    }

    private float CalculatePlayerCountScaling() {
        int playerCount = _gameManager.totalPlayers.Value;
        if (playerCount < 1) playerCount = 1;
        return Mathf.Sqrt(playerCount);
    }

    [ServerRpc]
    private void UpdateSpawnerSettingsServerRpc(float spawnRate, DirectorState state) {
        if (!IsServer) return;
        _enemySpawner.spawnCooldown = spawnRate;
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
        float playerCount = _gameManager.totalPlayers.Value;
        float playerScaling = 1f / Mathf.Sqrt(playerCount);
        _currentIntensity.Value += amount * playerScaling;
    }

    #endregion
}
