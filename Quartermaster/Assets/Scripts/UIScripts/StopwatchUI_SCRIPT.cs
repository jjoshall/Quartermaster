using UnityEngine;
using TMPro;
using Unity.Netcode;

public class StopwatchUI_SCRIPT : NetworkBehaviour {
    [SerializeField] private TextMeshProUGUI stopwatchText;

    private void Update() {
        if (stopwatchText == null) {
            Debug.LogError("Stopwatch Text is not assigned in the inspector.");
            return;
        }

        float time = GameManager.instance.stopwatchTime.Value;
        stopwatchText.text = FormatTime(time);
    }

    private string FormatTime(float time) {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
        return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }
}