using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using Steamworks;

public class IPRelay : NetworkBehaviour {

    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private GameObject loadingPanel;

    [SerializeField] private TMP_Text steamRelayDebugCodeText;


    protected Callback<LobbyCreated_t> lobbyCreated;
    // protected Callback<LobbyDataUpdate_t> lobbyDataUpdated;\
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;

    private CSteamID currentLobbyID;

    private string unityRelayJoinCode;


    private void Awake()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("SteamManager not initialized!");
            enabled = false;
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
    }


    private async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.LogError("Signed in as " + AuthenticationService.Instance.PlayerId);
            AnalyticsManager_SCRIPT.Instance?.OnSignedIn();
        };

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

    }

    // -------------------------- STEAM FUNCTIONS -------------------------- //

    // Create a new Steam Lobby
    public void CreateLobby(ELobbyType type = ELobbyType.k_ELobbyTypeFriendsOnly, int maxMembers = 4)
    {
        SteamMatchmaking.CreateLobby(type, maxMembers);
        Debug.LogError("Creating lobby...");
    }

    // Callback function for when the lobby is created, called by Steam
    public void OnLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create Steam lobby: " + result.m_eResult);
            return;
        }

        currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        Debug.LogError("Steam lobby created with ID: " + currentLobbyID);
    }


    // Callback function for when lobby is joined
    private void OnLobbyEntered(LobbyEnter_t enter)
    {


        currentLobbyID = new CSteamID(enter.m_ulSteamIDLobby);
        Debug.LogError($"Entered steam lobby with steam id: {currentLobbyID}");

        string relayCode = SteamMatchmaking.GetLobbyData(currentLobbyID, "relay_join_code");
        if (!string.IsNullOrEmpty(relayCode))
        {
            Debug.LogError($"Found relay code in lobby data: {relayCode}");
            JoinRelay(relayCode);
        }
        else
        {
            Debug.LogError("No relay_join_code set for this lobby.");
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t request)
    {
        Debug.LogError("GameLobbyJoinRequested fired!");
        SteamMatchmaking.JoinLobby(request.m_steamIDLobby);
    }


    public void SetRelayJoinCodeManual()
    {
        SetRelayJoinCode(unityRelayJoinCode);
    }

    // Put Unity Relay join code into the lobby data
    public void SetRelayJoinCode(string joinCode)
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
            return;
        }


        bool success = SteamMatchmaking.SetLobbyData(currentLobbyID, "relay_join_code", joinCode);
        if (success)
        {
            Debug.LogError($"Set lobby relay join code to : {joinCode}");
        }
        else
        {
            Debug.LogError($"Setting lobby relay join code failed!");
        }
    }

    // Get Unity Relay join code from lobby data
    public string GetRelayJoinCode()
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
        }

        string code = SteamMatchmaking.GetLobbyData(currentLobbyID, "relay_join_code");
        Debug.LogError($"Fetched relay join code for lobby: {code}");
        return code;
    }

    // Invite friend to lobby via Steam Overlay
    public void InviteFriendToLobby(CSteamID friendSteamID)
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
        }

        bool success = SteamMatchmaking.InviteUserToLobby(currentLobbyID, friendSteamID);
        Debug.LogError(success
                ? $"Sent steam lobby invite to {friendSteamID}"
                : $"Failed to send invite to {friendSteamID}");
    }

    // Open steam friends overlay
    public void OpenInviteDialog()
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
        }

        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
        Debug.LogError("Opened steam overlay invite dialog.");

    }


    // HELPER STEAM FUNCTIONS

    // helper function to print relay join code if it exists
    public void DisplayJoinCode()
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
        }

        string code = SteamMatchmaking.GetLobbyData(currentLobbyID, "relay_join_code");
        steamRelayDebugCodeText.text = $"Steam relay code: {code}";
        Debug.LogError($"Set steam relay debug text with code: {code}");

    }



    // --------------------------------------------------------------------- //


    // -------------------------- UNITY FUNCTIONS -------------------------- //
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            unityRelayJoinCode = joinCode;
            Debug.LogError("Relay join code: " + joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
            SetRelayJoinCode(joinCode);

        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RelayServiceException caught: " + e);
        }

    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.LogError("Joining relay with code: " + joinCode);
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

        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay join failed: " + e);
        }
    }


    // enables player ui and movement, disables lobby menu
    // meant to be used when host starts game
    public void TransitionToGameState(string joinCode)
    {
        // enable player ui 
        if (playerUICanvas != null)
        {
            playerUICanvas.gameObject.SetActive(true);
        }




        //TODO: Write player movement enable   
        //TODO: Write lobby menu disable

    }

}

