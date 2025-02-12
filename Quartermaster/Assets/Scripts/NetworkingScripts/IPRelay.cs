using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
using System.Collections;

public class IPRelay : MonoBehaviour {
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private LobbyManagerUI lobbyManagerUI;

    private async void Start() {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay() {
        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        
            Debug.Log(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            StartCoroutine(DelayedUIEnable(joinCode));
        
        } catch (RelayServiceException e) {
            Debug.LogError(e);
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

        } catch (RelayServiceException e) {
            Debug.LogError(e);
        }
    }

    private IEnumerator DelayedUIEnable(string joinCode) {
        yield return new WaitForSeconds(.5f);

        playerUICanvas.gameObject.SetActive(true);
        joinCodeText.gameObject.SetActive(true);
        joinCodeText.text = joinCode;

        lobbyManagerUI.HideLobbyUI();
    }

}
