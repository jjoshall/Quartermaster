using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Threading.Tasks; // Needed for Task.Delay

public class IPRelay : MonoBehaviour {
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private LobbyManagerUI lobbyManagerUI;
    [SerializeField] private GameObject loadingPanel; // LoadingPanel in your level design scene

    private async void Start() {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
            AnalyticsManager_SCRIPT.Instance?.OnSignedIn();
        };

        if (!AuthenticationService.Instance.IsSignedIn) {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateRelay() {
        // Call LoadingScreenManager to handle loading screen logic.
        Task fadeOutTask = await LoadingScreenManager.Instance.ShowLoadingScreenAsync();

        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Relay join code: " + joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            // Update UI elements using the new LoadingScreenManager.
            StartCoroutine(DelayedUIEnable(joinCode));
        }
        catch (RelayServiceException e) {
            Debug.LogError("RelayServiceException caught: " + e);
            LoadingScreenManager.Instance.DisableLoadingPanel();
        }

        // Optionally wait for fade-out to finish.
        if (fadeOutTask != null) {
            await fadeOutTask;
            Debug.Log("Finished fade out");
        }
    }

    public async void JoinRelay(string joinCode) {
    // Show the loading screen (fade in, disable lobby UI, wait 3 seconds, then start fade-out)
    Task fadeOutTask = await LoadingScreenManager.Instance.ShowLoadingScreenAsync();

    try {
        Debug.Log("Joining relay with code: " + joinCode);
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
            joinAlloc.RelayServer.IpV4,
            (ushort)joinAlloc.RelayServer.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();

        // Update UI elements using the LoadingScreenManager, just like CreateRelay does.
        StartCoroutine(LoadingScreenManager.Instance.HandleDelayedUIEnable(joinCode));
    }
    catch (RelayServiceException e) {
        Debug.LogError("Relay join failed: " + e);
        LoadingScreenManager.Instance.DisableLoadingPanel();
    }
    
    // Optionally wait for fade-out to finish before completing the join.
    if (fadeOutTask != null) {
        await fadeOutTask;
        Debug.Log("Finished fade out");
    }
}


    // Delegate the delayed UI enable to LoadingScreenManager.
    private IEnumerator DelayedUIEnable(string joinCode) {
        return LoadingScreenManager.Instance.HandleDelayedUIEnable(joinCode);
    }

    // Async method to fade a CanvasGroup's alpha over a given duration.
    private async Task FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration) {
        float elapsed = 0f;
        cg.alpha = startAlpha;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            await Task.Yield();
        }
        cg.alpha = endAlpha;
    }
}

