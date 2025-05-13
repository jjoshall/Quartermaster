using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;

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

    private async void Start() {
        await UnityServices.InitializeAsync();
        AnalyticsService.Instance.StartDataCollection();
        _isInitialized = true;
    }

    public void PressedTKey() {
        if (!_isInitialized) {
            Debug.LogWarning("Analytics not initialized yet.");
            return;
        }

        CustomEvent myEvent = new CustomEvent("pressed_t_key") {
            {   "key", "T" }
        };
    }
}
