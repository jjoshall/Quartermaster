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
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay() {
        // Activate and fade in the loading panel.
        if (loadingPanel != null) {
            loadingPanel.SetActive(true);
            CanvasGroup cg = loadingPanel.GetComponent<CanvasGroup>();
            if (cg != null) {
                Debug.Log("Fading in LoadingPanel");
                await FadeCanvasGroup(cg, 0f, 1f, 1f); // Fade in over 1 second.
                Debug.Log("Finished fade in");
            }
        } else {
            Debug.LogWarning("Loading Panel reference is missing!");
        }

        // Optionally disable the lobby menu canvas.
        if (lobbyManagerUI != null && lobbyManagerUI.GetComponent<Canvas>() != null) {
            Canvas lobbyCanvas = lobbyManagerUI.GetComponent<Canvas>();
            Debug.Log("Disabling LobbyMenuCanvas");
            lobbyCanvas.enabled = false;
            Debug.Log("LobbyMenuCanvas enabled state: " + lobbyCanvas.enabled);
        } else {
            Debug.LogWarning("LobbyManagerUI or its Canvas reference is missing!");
        }

        // Wait 3 seconds to allow the loading panel to be visible.
        Debug.Log("Waiting 3 seconds before proceeding...");
        await Task.Delay(3000);

        // Start fade out concurrently with network/scene initialization.
        Task fadeOutTask = null;
        if (loadingPanel != null) {
            CanvasGroup cg = loadingPanel.GetComponent<CanvasGroup>();
            if (cg != null) {
                Debug.Log("Starting fade out concurrently with scene initialization");
                fadeOutTask = FadeCanvasGroup(cg, 1f, 0f, 1f); // Fade out over 1 second concurrently.
            }
        }

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

            // Update UI elements.
            StartCoroutine(DelayedUIEnable(joinCode));
        }
        catch (RelayServiceException e) {
            Debug.LogError("RelayServiceException caught: " + e);
            if (loadingPanel != null) {
                Debug.Log("Disabling LoadingPanel due to error");
                loadingPanel.SetActive(false);
            }
        }

        // Optionally, wait for fade out to finish before finishing CreateRelay().
        if (fadeOutTask != null) {
            await fadeOutTask;
            Debug.Log("Finished fade out");
        }
    }

    public async void JoinRelay(string joinCode) {
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

            playerUICanvas.gameObject.SetActive(true);
            StartCoroutine(lobbyManagerUI.HideLobbyUI());
        }
        catch (RelayServiceException e) {
            Debug.LogError(e);
        }
    }

    private IEnumerator DelayedUIEnable(string joinCode) {
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Activating playerUICanvas and joinCodeText");
        playerUICanvas.gameObject.SetActive(true);
        joinCodeText.gameObject.SetActive(true);
        joinCodeText.text = joinCode;
        Debug.Log("JoinCode set to: " + joinCode);

        if (loadingPanel != null) {
            Debug.Log("Disabling LoadingPanel after UI update");
            loadingPanel.SetActive(false);
            Debug.Log("LoadingPanel active state after SetActive(false): " + loadingPanel.activeSelf);
        } else {
            Debug.LogWarning("Loading Panel reference is missing during UI update!");
        }

        lobbyManagerUI.HideLobbyUI();
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

