using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AnalyticsManager_SCRIPT : MonoBehaviour
{
    public static AnalyticsManager_SCRIPT Instance;
    private bool _isInitialized = false;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void OnSignedIn() {
        AnalyticsService.Instance.StartDataCollection();
        _isInitialized = true;
        Debug.Log("Analytics initialized and data collection started.");
    }

    public bool IsAnalyticsReady() => _isInitialized;
}

public class EnemyKilledEvent : Unity.Services.Analytics.Event {
    public EnemyKilledEvent() : base("EnemyKilled") { }

    public string EnemyType { set { SetParameter("enemyType", value); } }
}

public class PlayerDeathEvent : Unity.Services.Analytics.Event {
    public PlayerDeathEvent() : base("PlayerDeath") { }
}