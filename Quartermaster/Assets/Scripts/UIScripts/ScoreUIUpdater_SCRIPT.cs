using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ScoreUIUpdater_SCRIPT : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if (!IsOwner) return;

        GameManager.instance.totalScore.OnValueChanged += OnScoreChanged;
        UpdateScore(GameManager.instance.totalScore.Value);
    }

    private void OnDestroy() {
        if (!IsOwner || GameManager.instance == null) return;

        GameManager.instance.totalScore.OnValueChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int oldValue, int newValue) {
        UpdateScore(newValue);
    }

    private void UpdateScore(int score) {
        if (scoreText != null) {
            scoreText.text = $"{score}";
        }
    }
}
