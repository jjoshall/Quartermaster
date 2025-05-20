using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ScoreUIUpdater_SCRIPT : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreNum;
    [SerializeField] private TextMeshProUGUI killsNum;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if (!IsOwner) return;

        GameManager.instance.totalScore.OnValueChanged += OnScoreChanged;
        UpdateScore(GameManager.instance.totalScore.Value);

        GameManager.instance.totalEnemyKills.OnValueChanged += OnKillsChanged;
        UpdateKills(GameManager.instance.totalEnemyKills.Value);
    }

    //private void OnDestroy() {
    //    if (!IsOwner || GameManager.instance == null) return;

    //    GameManager.instance.totalScore.OnValueChanged -= OnScoreChanged;
    //}

    private void OnScoreChanged(int oldValue, int newValue) {
        UpdateScore(newValue);
    }

    private void OnKillsChanged(int oldValue, int newValue) {
        UpdateKills(newValue);
    }

    private void UpdateScore(int score) {
        if (scoreNum != null) {
            scoreNum.text = $"{score}";
        }
    }

    private void UpdateKills(int kills) {
        if (killsNum != null) {
            Debug.Log($"Kills: {kills}");
            killsNum.text = $"{kills}";
        }
    }
}
