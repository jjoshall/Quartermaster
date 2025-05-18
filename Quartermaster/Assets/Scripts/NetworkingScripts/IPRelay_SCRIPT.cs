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
using UnityEngine.UI;

public class IPRelay : NetworkBehaviour
{
    [SerializeField] private TMP_Text joinCodeText;

    [Header("Lobby → Game UI")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Canvas lobbyMenuCanvas;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button inviteFriendsButton;

    [SerializeField] private GameObject manualJoinPanel;
    //[SerializeField] private Button manualJoinButton;
    [SerializeField] private TMP_Text manualJoinCodeText;

    [Header("Player Profile Display Area")]
    [SerializeField] private GameObject playerProfileDisplayArea;
    [SerializeField] private GameObject playerProfilePrefab;

    private const string MAIN_MENU_SCENE = "MainMenu_SCENE";

    public UnityEvent OnGameStarted;

    [Header("Steam Callbacks + IP")]
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyMatchList_t> lobbyMatchList;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;

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
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        inviteFriendsButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
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

        NetworkManager.Singleton.OnClientStopped += HandleNetworkShutdown;
        NetworkManager.Singleton.OnServerStopped += HandleNetworkShutdown;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped -= HandleNetworkShutdown;
            NetworkManager.Singleton.OnServerStopped -= HandleNetworkShutdown;
        }
    }

    #region Steam Functions

    public void CreateLobby(ELobbyType type = ELobbyType.k_ELobbyTypeFriendsOnly, int maxMembers = 4)
    {
        SteamMatchmaking.CreateLobby(type, maxMembers);
        Debug.LogError("Creating lobby...");
        createLobbyButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);

        inviteFriendsButton.gameObject.SetActive(true);
        startGameButton.gameObject.SetActive(true);
    }

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

    private void OnLobbyEntered(LobbyEnter_t enter)
    {
        currentLobbyID = new CSteamID(enter.m_ulSteamIDLobby);
        Debug.LogError($"Entered steam lobby with steam id: {currentLobbyID}");

        RefreshLobbyProfiles();

        if (IsOwner)
        {
            SetRelayJoinCode(unityRelayJoinCode);
            Debug.LogError("set relay join code from OnLobbyEntered callback");
        }

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

        manualJoinPanel.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
    }

    private void OnLobbyMatchList(LobbyMatchList_t result)
    {
        uint count = result.m_nLobbiesMatching;
        if (count <= 0)
        {
            Debug.LogError("No steam lobbies foudn with that relay code");
            return;
        }

        CSteamID foundLobby = SteamMatchmaking.GetLobbyByIndex(0);
        Debug.LogError($"Found steam lobby {foundLobby}");

        SteamMatchmaking.JoinLobby(foundLobby);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t result)
    {
        if (result.m_ulSteamIDLobby != (ulong)currentLobbyID)
            return;

        var steamID = new CSteamID(result.m_ulSteamIDUserChanged);
        uint flags = result.m_rgfChatMemberStateChange;

        if ((flags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
        {
            AddPlayerProfile(steamID);
            return;
        }

        bool left = (flags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0;
        bool dropped = (flags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0;
        if (left || dropped)
        {
            if (lobbyMenuCanvas != null && lobbyMenuCanvas.gameObject.activeSelf)
                RefreshLobbyProfiles();

            CSteamID owner = SteamMatchmaking.GetLobbyOwner(currentLobbyID);
            if (steamID == owner && !IsOwner)
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene(MAIN_MENU_SCENE);
            }
        }
    }

    public void SetRelayJoinCodeManual()
    {
        SetRelayJoinCode(unityRelayJoinCode);
    }

    public void SetRelayJoinCode(string joinCode)
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
            return;
        }

        bool success = SteamMatchmaking.SetLobbyData(currentLobbyID, "relay_join_code", joinCode);
        if (success)
            Debug.LogError($"Set lobby relay join code to : {joinCode}");
        else
            Debug.LogError("Setting lobby relay join code failed!");
    }

    public string GetRelayJoinCode()
    {
        if (currentLobbyID == CSteamID.Nil)
            Debug.LogError("No lobby created yet!");

        string code = SteamMatchmaking.GetLobbyData(currentLobbyID, "relay_join_code");
        Debug.LogError($"Fetched relay join code for lobby: {code}");
        return code;
    }

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

    public void OpenInviteDialog()
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogError("No lobby created yet!");
        }

        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
        Debug.LogError("Opened steam overlay invite dialog.");
    }

    private void ShutdownSteamLobby()
    {
        if (currentLobbyID != CSteamID.Nil)
        {
            Debug.LogError("leaving steam lobby after shutdown");
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = CSteamID.Nil;
        }
    }

    private void AddPlayerProfile(CSteamID playerID)
    {
        int imgId = SteamFriends.GetLargeFriendAvatar(playerID);
        Texture2D tex = GetSteamImageAsTexture(imgId);

        var go = Instantiate(playerProfilePrefab, playerProfileDisplayArea.transform);
        var raw = go.GetComponentInChildren<RawImage>();
        if (raw != null && tex != null)
            raw.texture = tex;
    }

    private void RefreshLobbyProfiles()
    {
        foreach (Transform child in playerProfileDisplayArea.transform)
            Destroy(child.gameObject);

        int count = (int)SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        for (int i = 0; i < count; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
            AddPlayerProfile(member);
        }
    }

    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        if (iImage < 0) return null;

        uint w, h;
        if (!SteamUtils.GetImageSize(iImage, out w, out h) || w == 0 || h == 0)
            return null;

        byte[] buf = new byte[w * h * 4];
        SteamUtils.GetImageRGBA(iImage, buf, buf.Length);

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(buf);
        tex.Apply();
        return tex;
    }

    #endregion

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            unityRelayJoinCode = joinCode;
            manualJoinCodeText.gameObject.SetActive(true);
            manualJoinCodeText.text = joinCode;

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

    [ClientRpc]
    private void StartGameClientRpc(ClientRpcParams rpcParams = default)
    {
        if (lobbyMenuCanvas != null)
            lobbyMenuCanvas.gameObject.SetActive(false);

        if (playerUICanvas != null)
            playerUICanvas.gameObject.SetActive(true);

        var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localObj != null)
        {
            var pc = localObj.GetComponent<PlayerController>();
            if (pc != null)
                pc.movementRestricted = false;
        }
    }

    public void HostPressedStart()
    {
        OnGameStarted?.Invoke();
        StartGameClientRpc();
    }

    private void HandleNetworkShutdown(bool _)
    {
        ShutdownSteamLobby();
    }
}
