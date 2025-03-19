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
    // public enum PeakVariation {
    //     HealthBoost,
    //     SpeedBoost,
    //     SpawnRush,
    //     Mixed
    // }

    public float lastTimeDamageTaken;
    public float outOfCombatThreshold = 5f; // seconds before we consider the player out of combat.
    private PhaseData _bestBuildupData;
    private PhaseData _bestPeakData;
    private PhaseParameters _bestBuildupParams;
    private PhaseParameters _bestPeakParams;
    [HideInInspector] public PhaseParameters currPhaseParams;
    private PhaseData _currPhaseData;
    [SerializeField] private float minHpThreshold = 0.2f;
    [SerializeField] private float maxHpThreshold = 0.8f;
    [SerializeField] private float minEnemiesThreshold = 0.5f;

    #region GeneticAlgo
    public struct FitnessWeights {
        public float combatTimeWeight;
        public float timeBelowMinHpWeight;
        public float timeAboveMaxHpWeight;
        public float timeAboveMinEnemiesWeight;
    }

    public static readonly FitnessWeights buildUpWeights = new FitnessWeights {
        combatTimeWeight = 1.0f,
        timeBelowMinHpWeight = 0.5f,
        timeAboveMaxHpWeight = 0.5f,
        timeAboveMinEnemiesWeight = 0.5f
    };
    public static readonly FitnessWeights peakWeights = new FitnessWeights {
        combatTimeWeight = 1.0f,
        timeBelowMinHpWeight = 0.5f,
        timeAboveMaxHpWeight = 0.5f,
        timeAboveMinEnemiesWeight = 0.5f
    };

    public struct PhaseData{ // data used to collect fitness.
        // Collect Data
        public float combatTime;
        public float timeBelowMinHp; // affect enemies
        public float timeAboveMaxHp;
        public float timeAboveMinEnemies;
        public float phaseDuration;

    }

    public struct PhaseParameters{ // data used to adjust gameplay. mutated after comparing curr and updating best.
        public float enemyHealthMultiplier;
        public float enemySpeedMultiplier;
        public float enemyDamageMultiplier;
        public float enemyGlobalTargetInterval; // lower = better global tracking
        public float enemyLocalDetectionRange; // local is direct tracking
        // spawn rate handled elsewhere.
    }

    private float EvalFitnessForBuildUp(PhaseData phaseData){
        if (phaseData.phaseDuration <= 0) return 0f; // Avoid division by zero

        float fitness = 0.0f;
        float minCombatTimeRatio = 0.2f; // 20% of phase time
        float maxCombatTimeRatio = 0.4f; // 40% of phase time
        float minTimeAboveMinEnemies = 0.6f; // 60% of phase time
        float target_timeAboveMaxHp = 0.1f;
        float target_timeBelowMinHp = 0.1f;
        float combatTimeRatio = phaseData.combatTime / phaseData.phaseDuration;
        float timeAboveMinEnemiesRatio = phaseData.timeAboveMinEnemies / phaseData.phaseDuration;
        float timeAboveMaxHpRatio = phaseData.timeAboveMaxHp / phaseData.phaseDuration;
        float timeBelowMinHpRatio = phaseData.timeBelowMinHp / phaseData.phaseDuration;

        // Fitness++ if combat time is within min/max ratio of phase time. 
        if (combatTimeRatio < minCombatTimeRatio) {
            fitness -= (minCombatTimeRatio - combatTimeRatio) * 10f * buildUpWeights.combatTimeWeight;
        } else if (combatTimeRatio > maxCombatTimeRatio) {
            fitness -= (combatTimeRatio - maxCombatTimeRatio) * 10f * buildUpWeights.combatTimeWeight;
        } else {
            fitness += buildUpWeights.combatTimeWeight;
        }

        // Fitness++ if above minimum time where enemies are greater than min count ratio. 
        if (timeAboveMinEnemiesRatio > minTimeAboveMinEnemies){
            fitness += buildUpWeights.timeAboveMinEnemiesWeight;
        } else {
            fitness -= (minTimeAboveMinEnemies - timeAboveMinEnemiesRatio) * 10f * buildUpWeights.timeAboveMinEnemiesWeight;
        }

        // Fitness-- for distance away from target time for above max hp and below min hp. 
        fitness -= Mathf.Abs((target_timeAboveMaxHp - timeAboveMaxHpRatio)) * 10f * buildUpWeights.timeAboveMaxHpWeight;
        fitness -= Mathf.Abs((target_timeBelowMinHp - timeBelowMinHpRatio)) * 10f * buildUpWeights.timeBelowMinHpWeight;

        return fitness;
    }

    // weighted towards predicted relevant values for buildup
    private PhaseParameters MutateParamsTowardBuildup(PhaseParameters p){
        PhaseParameters newParams = p;
        newParams.enemyHealthMultiplier += Random.Range(-0.1f, 0.3f);
        newParams.enemySpeedMultiplier += Random.Range(-0.1f, 0.1f);
        newParams.enemyDamageMultiplier += Random.Range(-0.1f, 0.1f);
        newParams.enemyGlobalTargetInterval += Random.Range(-0.1f, 0.5f);
        newParams.enemyLocalDetectionRange += Random.Range(-0.5f, 0.1f);
        return newParams;
    }

    private float EvalFitnessForPeak(PhaseData phaseData){
        if (phaseData.phaseDuration <= 0) return 0f; // Avoid division by zero
        
        float fitness = 0.0f;
        float minCombatTimeRatio = 0.8f; // 20% of phase time
        float maxCombatTimeRatio = 1.0f; // 40% of phase time
        float target_timeAboveMaxHp = 0.1f;
        float target_timeBelowMinHp = 0.3f;
        float combatTimeRatio = phaseData.combatTime / phaseData.phaseDuration;
        float timeAboveMaxHpRatio = phaseData.timeAboveMaxHp / phaseData.phaseDuration;
        float timeBelowMinHpRatio = phaseData.timeBelowMinHp / phaseData.phaseDuration;

        // Fitness++ if combat time is within min/max ratio of phase time. 
        if (combatTimeRatio < minCombatTimeRatio) {
            fitness -= (minCombatTimeRatio - combatTimeRatio) * 10f * buildUpWeights.combatTimeWeight;
        } else if (combatTimeRatio > maxCombatTimeRatio) {
            fitness -= (combatTimeRatio - maxCombatTimeRatio) * 10f * buildUpWeights.combatTimeWeight;
        } else {
            fitness += buildUpWeights.combatTimeWeight;
        }

        // Fitness-- for distance away from target time for above max hp and below min hp. 
        fitness -= Mathf.Abs((target_timeAboveMaxHp - timeAboveMaxHpRatio)) * 10f * buildUpWeights.timeAboveMaxHpWeight;
        fitness -= Mathf.Abs((target_timeBelowMinHp - timeBelowMinHpRatio)) * 10f * buildUpWeights.timeBelowMinHpWeight;

        return fitness;
    }

    private PhaseParameters MutateParamsTowardPeak(PhaseParameters p){
        PhaseParameters newParams = p;
        newParams.enemyHealthMultiplier += Random.Range(-0.1f, 0.1f);
        newParams.enemySpeedMultiplier += Random.Range(-0.1f, 0.2f);
        newParams.enemyDamageMultiplier += Random.Range(-0.1f, 0.2f);
        newParams.enemyGlobalTargetInterval += Random.Range(-0.3f, 0.1f);
        newParams.enemyLocalDetectionRange += Random.Range(-0.1f, 0.3f);
        return newParams;
    }


    #endregion

    #region DramaPhases
    // affects spawnrate primarily.
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

    #endregion 
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

    // private PeakVariation _currentPeakVariation;

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


        // Genetic algo setup
        _bestBuildupData = InitNewPhaseData();
        _bestPeakData = InitNewPhaseData();
        _bestBuildupParams = new PhaseParameters();
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
        // Phase state machine. 
        // Transitions on intensity thresholds or timeouts.
        // Cyclical. BuildUp -> Peak -> Relax -> BuildUp
        if (EnemySpawner.instance.activePlayerList.Count > 0){
            DecayIntensity();
            StateMachineTimer();
        }
    }

    private void DecayIntensity(){
        _currentIntensity.Value = Mathf.Max(0, _currentIntensity.Value - (_intensityDecayRate * Time.deltaTime));
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
        PhaseDataUpdate();
    }

    #region GeneticAlgoDataUpdate
    // Increment time variables in _currPhaseData 
    private void PhaseDataUpdate(){

        // Increment combatTimeData if out of combat (last dmg taken was before threshold)
        if (lastTimeDamageTaken + outOfCombatThreshold < Time.time){
            _currPhaseData.combatTime += Time.deltaTime;
        }

        // Increment playerhpdata based on player health ratio averaged across players.
        foreach (GameObject player in EnemySpawner.instance.activePlayerList){
            // Null checks + get variables.
            if (player == null) continue;
            Health playerHp = player.GetComponent<Health>();
            if (playerHp == null) continue;
            float hpRatio = playerHp.GetRatio();
            int playerCount = EnemySpawner.instance.activePlayerList.Count;
            // Increment data if conditions met.
            if (hpRatio < minHpThreshold){
                _currPhaseData.timeBelowMinHp += Time.deltaTime / playerCount;
            }
            if (hpRatio > maxHpThreshold){
                _currPhaseData.timeAboveMaxHp += Time.deltaTime / playerCount;
            }
        }

        // Increment enemyMinCount time data if above threshold.
        if (EnemySpawner.instance.enemyList.Count / EnemySpawner.instance.maxEnemyInstanceCount > minEnemiesThreshold){
            _currPhaseData.timeAboveMinEnemies += Time.deltaTime;
        }

        // Increment total phase time.
        _currPhaseData.phaseDuration += Time.deltaTime;
    }
    #endregion
    private PhaseData InitNewPhaseData(){
        PhaseData newPhaseData = new PhaseData();
        newPhaseData.combatTime = 0f;
        newPhaseData.timeBelowMinHp = 0f;
        newPhaseData.timeAboveMaxHp = 0f;
        newPhaseData.timeAboveMinEnemies = 0f;
        newPhaseData.phaseDuration = 0f;
        return newPhaseData;
    }

    private void TransitionToPeak() {
        _currentState.Value = DirectorState.Peak;
        _stateTimeRemaining.Value = _peakDuration;
        UpdateEnemySpawnerSettings();
        Debug.Log ("AI Director transitioning to Peak phase");

        EvalBuildUp();

        currPhaseParams = MutateParamsTowardPeak(_bestPeakParams);
        UpdateEnemySpeedMultiplier(currPhaseParams.enemySpeedMultiplier);
        UpdateEnemyHpMultiplier(currPhaseParams.enemyHealthMultiplier);
        UpdateEnemyDmgMultiplier(currPhaseParams.enemyDamageMultiplier);

        _currPhaseData = InitNewPhaseData();

    }

    private void TransitionToRelax() {
        _currentState.Value = DirectorState.Relax;
        _stateTimeRemaining.Value = _relaxDuration;
        UpdateEnemySpawnerSettings();
        Debug.Log("AI Director transitioning to Relax phase");

        EvalPeak();
    }
    private void TransitionToBuildUp() {
        _currentIntensity.Value = _baseIntensity;
        _currentState.Value = DirectorState.BuildUp;
        _stateTimeRemaining.Value = _buildUpDuration;
        UpdateEnemySpawnerSettings();
        Debug.Log("AI Director transitioning to Build Up phase");

        currPhaseParams = MutateParamsTowardBuildup(_bestBuildupParams);
        UpdateEnemySpeedMultiplier(currPhaseParams.enemySpeedMultiplier);
        UpdateEnemyHpMultiplier(currPhaseParams.enemyHealthMultiplier);
        UpdateEnemyDmgMultiplier(currPhaseParams.enemyDamageMultiplier);

        _currPhaseData = InitNewPhaseData();
    }

    private void EvalBuildUp(){
        float currFitness = EvalFitnessForBuildUp(_currPhaseData);
        float bestBuildUpFitness = EvalFitnessForBuildUp(_bestBuildupData);
        if (currFitness > bestBuildUpFitness){
            _bestBuildupData = _currPhaseData;
            _bestBuildupParams = currPhaseParams;
        }
    }

    private void EvalPeak(){
        float currFitness = EvalFitnessForPeak(_currPhaseData);
        float bestPeakFitness = EvalFitnessForPeak(_bestPeakData);
        if (currFitness > bestPeakFitness){
            _bestPeakData = _currPhaseData;
            _bestPeakParams = currPhaseParams;
        }
    }

    #endregion

    #region Difficulty Scaling
    private void UpdateEnemySpeedMultiplier (float speedMultiplier){
        EnemySpawner.instance.UpdateEnemySpeed(speedMultiplier);
    }

    private void UpdateEnemyHpMultiplier (float hpMultiplier){
        EnemySpawner.instance.aiHpMultiplier = hpMultiplier; // assumes hpMultiplier is 1.0f + increase.
    }

    private void UpdateEnemyDmgMultiplier (float dmgMultiplier){
        EnemySpawner.instance.aiDmgMultiplier = 1.0f + dmgMultiplier; // assumes dmgMultiplier is 1.0f + increase.
    }

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
