using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AnalyticsManager_SCRIPT : MonoBehaviour
{
    public static AnalyticsManager_SCRIPT Instance;
    private bool _isInitialized = false;

    // FPS Tracking
    public float fpsSampleInterval = 10f;
    private float _fpsTimeElapsed = 0f;
    private int _fpsFrameCount = 0;

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
    }

    public bool IsAnalyticsReady() => _isInitialized;
}