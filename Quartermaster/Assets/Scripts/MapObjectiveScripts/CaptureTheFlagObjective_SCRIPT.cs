using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CaptureTheFlagObjective : IObjective {
    [Header("Defense Settings")]
    [Tooltip("Duration in seconds to defend the node before it is cleared.")]
    public float nodeDefenseDuration = 4f;

    [SerializeField] private GameObject objectiveRing;
    [SerializeField] private Transform beacon;

    private float _currentDefenseTimer = 0f;
    private List<Transform> _playersInRange = new List<Transform>();
    private bool _isComplete = false;

    public override bool IsComplete() => _isComplete;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        _currentDefenseTimer = 0f;
        _playersInRange.Clear();
        _isComplete = false;
    }

    private void Update() {
        if (!IsServer || _isComplete) return;

        if (_playersInRange.Count > 0) {
            _currentDefenseTimer += Time.deltaTime;
            ObjectiveManager.instance.NodeZoneTextHelperClientRpc(true);
        }
        else {
            if (_currentDefenseTimer > 0f)
                _currentDefenseTimer -= Time.deltaTime;

            ObjectiveManager.instance.NodeZoneTextHelperClientRpc(false);
        }

        if (_currentDefenseTimer >= nodeDefenseDuration) {
            _isComplete = true;
            ClearObjective();
            GameManager.instance.AddScoreServerRpc(200);
            Debug.Log("Total score " + GameManager.instance.totalScore.Value);
            beacon.gameObject.SetActive(false);
        }
    }


    public void PublicTriggerEnter(Collider other) {
        if (!IsServer || !other.CompareTag("Player")) return;

        Transform player = other.transform;
        if (!_playersInRange.Contains(player)) {
            _playersInRange.Add(player);
            beacon.SetParent(player, worldPositionStays: true);
        }
    }

    public void PublicTriggerExit(Collider other) {
        if (!IsServer || !other.CompareTag("Player")) return;

        Transform player = other.transform;
        if (_playersInRange.Remove(player)) {
            // reset timer when everyone leaves
            if (_playersInRange.Count == 0) {
                _currentDefenseTimer = 0f;
                ObjectiveManager.instance.NodeZoneTextHelperClientRpc(false);
            }
        }
    }
}
