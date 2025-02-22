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
        // Activate the loading panel immediately when the button is clicked.
        if (loadingPanel != null) {
            Debug.Log("Activating LoadingPanel");
            loadingPanel.SetActive(true);
            Debug.Log("LoadingPanel active state after SetActive(true): " + loadingPanel.activeSelf);
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

            // Update UI elements and hide the loading panel after a short delay.
            StartCoroutine(DelayedUIEnable(joinCode));
        }
        catch (RelayServiceException e) {
            Debug.LogError("RelayServiceException caught: " + e);
            if (loadingPanel != null) {
                Debug.Log("Disabling LoadingPanel due to error");
                loadingPanel.SetActive(false);
            }
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
}
