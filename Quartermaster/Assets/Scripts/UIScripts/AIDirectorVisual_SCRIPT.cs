using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AIDirectorVisual : MonoBehaviour {
    public Slider _intensitySlider;
    public TextMeshProUGUI _stateText;
    public TextMeshProUGUI _timeRemainingText;
    public TextMeshProUGUI _enemyCountText;
    private AIDirector _aiDirector;

    private void Start() {
        _aiDirector = GetComponent<AIDirector>();
        if (_aiDirector == null) {
            Debug.LogWarning("AIDirector not set in AIDirectorVisual!");
        }
    }

    private void Update() {
        if (_aiDirector == null || !_aiDirector.IsSpawned) {
            return;
        }

        UpdateIntensitySlider(_aiDirector._currentIntensity.Value);
        UpdateStateText(_aiDirector._currentState.Value.ToString());
        UpdateTimeRemainingText(_aiDirector._stateTimeRemaining.Value);
        UpdateEnemyCountText(EnemySpawner.instance.enemyList.Count);
    }

    public void UpdateIntensitySlider(float intensity) {
        if (_intensitySlider != null) {
            _intensitySlider.value = intensity / 100f;
        }
        else {
            Debug.LogWarning("Intensity Slider not set in AIDirectorVisual!");
        }
    }

    public void UpdateStateText(string state) {
        if (_stateText != null) {
            _stateText.text = "State: " + state;
        }
        else {
            Debug.LogWarning("State Text not set in AIDirectorVisual!");
        }
    }

    public void UpdateTimeRemainingText(float timeRemaining) {
        if (_timeRemainingText != null) {
            _timeRemainingText.text = "Time Remaining: " + Mathf.CeilToInt(timeRemaining).ToString() + "s";
        }
        else {
            Debug.LogWarning("Time Remaining Text not set in AIDirectorVisual!");
        }
    }

    public void UpdateEnemyCountText(int count) {
        if (_enemyCountText != null) {
            _enemyCountText.text = "Enemy Count: " + count.ToString();
        }
        else {
            Debug.LogWarning("Enemy Count Text not set in AIDirectorVisual!");
        }
    }
}
