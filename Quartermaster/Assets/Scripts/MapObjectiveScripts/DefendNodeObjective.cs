using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;

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

    [Tooltip("Duration in seconds to defend the node before it is cleared.")]
    public float nodeDefenseDuration = 60f; // time until node complete.
    private float _currentDefenseTimer = 0f;
    private List<GameObject> _playersInRange = new List<GameObject>();

    // STRETCH GOAL: Additional node defense constraints.
    //               - Keep track of player. Each player has to contribute to the inRange condition.
    //               - 

    private float _particleTimer = 0f;
    private float _particleInterval = 2.0f;

    #endregion 

    #region = Setup

    void Start()
    {
        // n_defenseCompleted.Value = false;
        _currentDefenseTimer = 0f;
        // n_nodeDefenseActive.Value = false;
        _particleTimer = 0f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // n_defenseCompleted.Value = false;
        _currentDefenseTimer = 0f;
        // n_nodeDefenseActive.Value = false;
        _particleTimer = 0f;
    }

    void Update(){
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
            GameManager.instance.AddScoreServerRpc(200);
            Debug.Log("Total score " + GameManager.instance.totalScore.Value);
            return;
            // _particleInterval = 1000f;
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
    public void SetDefenseCompletedServerRpc(bool completed){
        n_defenseCompleted.Value = completed;
        NodeZoneTextHelper();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNodeDefenseActiveServerRpc(bool active){
        n_nodeDefenseActive.Value = active;
    }   

    // Using ObjectiveRing's trigger instead.
    public void PublicTriggerEnter (Collider other){
        if (other.gameObject.tag == "Player"){
            _playersInRange.Add(other.gameObject);
            SetNodeDefenseActiveServerRpc(true);
            NodeZoneTextHelper();
        }
    }

    public void PublicTriggerExit (Collider other){
        if(other.gameObject.tag == "Player"){
            _playersInRange.Remove(other.gameObject);
            if(!HasPlayersInRange()){
                SetNodeDefenseActiveServerRpc(false);
                _currentDefenseTimer = 0f;
                _particleTimer = 0f;
            }
            NodeZoneTextHelper();
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
