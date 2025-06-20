using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine.Events;

public class DefendNodeObjective : IObjective
{
    // public List<GameObject> nodes;

    private bool _isComplete = false;

    public override bool IsComplete()
    {
        return _isComplete;
        // throw new System.NotImplementedException();
    }
    #region Inspector
    [SerializeField] private GameObject objectiveRing;

    #endregion

    #region Variables
    public NetworkVariable<bool> n_defenseCompleted = new NetworkVariable<bool>(false); // completed when players successfully complete the defense.
    private NetworkVariable<bool> n_nodeDefenseActive = new NetworkVariable<bool>(false); // active with players in range.
    #endregion

    #region Settings
    [Tooltip("Duration in seconds to defend the node before it is cleared.")]
    public float nodeDefenseDuration = 60f; // time until node complete.
    public float spawnCdMultiplier = 0.5f; // 0.5 for double spawn rate 
    public float globalAggroMultiplier = 0.5f; // 0.5 for global aggro target updates twice as fast.
    private float _currentDefenseTimer = 0f;
    private List<GameObject> _playersInRange = new List<GameObject>();

    [SerializeField]
    private GameObject _turretItemPrefab;
    private List<GameObject> _turretItemsSpawned = new List<GameObject>();

    // create a Unity event for when the node defense is completed or deactivated 
    [HideInInspector] public UnityEvent onNodeDefenseDeactivated = new UnityEvent();

    // STRETCH GOAL: Additional node defense constraints.
    //               - Keep track of player. Each player has to contribute to the inRange condition.
    //               - 

    // private float _particleTimer = 0f;
    // private float _particleInterval = 2.0f;

    #endregion






    #region = Setup

    void Start()
    {
        // n_defenseCompleted.Value = false;
        _currentDefenseTimer = 0f;
        // n_nodeDefenseActive.Value = false;
        // _particleTimer = 0f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // n_defenseCompleted.Value = false;
        _currentDefenseTimer = 0f;
        // n_nodeDefenseActive.Value = false;
        // _particleTimer = 0f;
    }

    void Update() {
        if (!IsServer) return;
        if (n_defenseCompleted.Value){
            return;
        }
        UpdateDefenseTimer();
    }

    #endregion 


    #region = Logic

    private void UpdateDefenseTimer(){
        // increment if hasPlayersinRange, decrement if no players in range
        if (_currentDefenseTimer >= nodeDefenseDuration){
            SetDefenseCompletedServerRpc(true);
            ClearObjective();
            GameManager.instance.totalScore.Value += GameManager.instance.ScorePerObjective;
            Debug.Log("Total score " + GameManager.instance.totalScore.Value);
            return;
            // _particleInterval = 1000f;
        }
        if (HasPlayersInRange()) {
            for (int i = 0; i < _playersInRange.Count; i++) {
                Health health = _playersInRange[i].GetComponent<Health>();
                if (health) {
                    if (health.Invincible.Value) {
                        SetNodeDefenseActiveServerRpc(false);
                        NodeZoneTextHelper(false);   
                        _playersInRange.Remove(_playersInRange[i]);
                    }
                    else {
                        SetNodeDefenseActiveServerRpc(true);
                        NodeZoneTextHelper(true);  
                    }
                }
            }
        }
        else {
            SetNodeDefenseActiveServerRpc(false);
            NodeZoneTextHelper(false);
        }

        if (n_nodeDefenseActive.Value){
            _currentDefenseTimer += Time.deltaTime;
        } else {
            if (_currentDefenseTimer >= 0){
                _currentDefenseTimer -= Time.deltaTime;
            } else {
                _currentDefenseTimer = 0;
            }
        }

        // throw new System.NotImplementedException();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetDefenseCompletedServerRpc(bool completed)
    {
        n_defenseCompleted.Value = completed;
        NodeZoneTextHelper(false);
        EnemySpawner.instance.globalAggroUpdateIntervalMultiplier = 1f;
        EnemySpawner.instance.spawnCooldownMultiplier = 1f;
        
        SetNodeDefenseActiveServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNodeDefenseActiveServerRpc(bool active)
    {
        if (active && !n_nodeDefenseActive.Value)
            SpawnTurretItemsServerRpc();
        if (!active && n_nodeDefenseActive.Value)
            ClearItemsServerRpc();

        n_nodeDefenseActive.Value = active;
        EnemySpawner.instance.globalAggroUpdateIntervalMultiplier = active ? globalAggroMultiplier : 1f;
        EnemySpawner.instance.spawnCooldownMultiplier = active ? spawnCdMultiplier : 1f;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnTurretItemsServerRpc()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPosition = new Vector3(
                transform.position.x + Random.Range(-2f, 2f),
                transform.position.y + 2.0f,
                transform.position.z + Random.Range(-2f, 2f)
            );
            GameObject turretItem = Instantiate(_turretItemPrefab, spawnPosition, Quaternion.identity);
            turretItem.GetComponent<NetworkObject>().Spawn(true);
            turretItem.GetComponent<TurretItem_MONO>().InitEvent(onNodeDefenseDeactivated);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClearItemsServerRpc() {
        // If the node defense is deactivated, we can clear the turret items.
        // foreach (GameObject turretItem in _turretItemsSpawned)
        // {
        //     if (turretItem != null)
        //     {
        //         Destroy(turretItem);
        //     }
        // }
        onNodeDefenseDeactivated.Invoke();
        _turretItemsSpawned.Clear();
    }

    // Using ObjectiveRing's trigger instead.
    public void PublicTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            _playersInRange.Add(other.gameObject);
            SetNodeDefenseActiveServerRpc(true);
            NodeZoneTextHelper(true);
        }
    }

    public void PublicTriggerExit (Collider other){
        if(other.gameObject.tag == "Player"){
            _playersInRange.Remove(other.gameObject);
            if(!HasPlayersInRange()){
                SetNodeDefenseActiveServerRpc(false);
                _currentDefenseTimer = 0f;
                // _particleTimer = 0f;
            }
            NodeZoneTextHelper(false);
        }
    }

    #endregion

    #region = Helpers
    private float GetRatio(){
        return _currentDefenseTimer / nodeDefenseDuration;
    }

    bool HasPlayersInRange(){
        return _playersInRange.Count > 0;
    }

    #endregion


}
