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

public class IPRelay : NetworkBehaviour {
    #region Variables
    [SerializeField] private TMP_Text joinCodeText;

    [SerializeField] private GameObject startingDoor;

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

    private bool alreadyInSteamLobby = false;

    public UnityEvent OnGameStarted;

    [Header("Steam Callbacks + IP")]
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyMatchList_t> lobbyMatchList;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;

    private CSteamID currentLobbyID;
    private string unityRelayJoinCode;

    #endregion

    #region Unity Default Functions
    private void Awake() {
        // only load steam if not in editor
#if !UNITY_EDITOR
        if (!SteamManager.Initialized)
        {
            Debug.LogError("SteamManager not initialized!");
            enabled = false;
            return;
        }
#endif

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        // hide invite & start buttons initially
        inviteFriendsButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
    }

    private async void Start() {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () => {
            Debug.LogError("Signed in as " + AuthenticationService.Instance.PlayerId);
            AnalyticsManager_SCRIPT.Instance?.OnSignedIn();
        };

        if (!AuthenticationService.Instance.IsSignedIn) {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        NetworkManager.Singleton.OnClientStopped += HandleNetworkShutdown;
        NetworkManager.Singleton.OnServerStopped += HandleNetworkShutdown;

        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
#if !UNITY_EDITOR
            if (!NetworkManager.Singleton.IsHost && !alreadyInSteamLobby)
            {
                RequestSteamLobbyIDServerRpc();
            }
#endif
        };
    }

    private void Update() {
        if (gameObject.activeSelf) {
#if !UNITY_EDITOR
            RefreshLobbyProfiles();
#endif
        }
    }

#pragma warning disable 0114
    private void OnDestroy() {
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientStopped -= HandleNetworkShutdown;
            NetworkManager.Singleton.OnServerStopped -= HandleNetworkShutdown;
        }
    }
#pragma warning restore 0114
    #endregion

    #region Steam Functions

    public void CreateLobby(ELobbyType type = ELobbyType.k_ELobbyTypeFriendsOnly, int maxMembers = 4) {
        SteamMatchmaking.CreateLobby(type, maxMembers);

        // hide both lobby buttons once pressed
        joinLobbyButton.gameObject.SetActive(false);
        createLobbyButton.gameObject.SetActive(false);

        inviteFriendsButton.gameObject.SetActive(true);

#if !UNITY_EDITOR
        startGameButton.gameObject.SetActive(true);
        startGameButton.interactable = true;
#endif
    }

    public void OnLobbyCreated(LobbyCreated_t result) {
        if (result.m_eResult != EResult.k_EResultOK) {
            return;
        }

        alreadyInSteamLobby = true;
        currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t enter) {
        currentLobbyID = new CSteamID(enter.m_ulSteamIDLobby);

#if !UNITY_EDITOR
        RefreshLobbyProfiles();
#endif

        if (NetworkManager.Singleton.IsHost) {
            SetRelayJoinCode(unityRelayJoinCode);
        }

        if (!alreadyInSteamLobby) {
            string relayCode = SteamMatchmaking.GetLobbyData(currentLobbyID, "relay_join_code");
            if (!string.IsNullOrEmpty(relayCode)) {
                JoinRelay(relayCode);
                alreadyInSteamLobby = true;
            }
        }

        // hide lobby buttons
        createLobbyButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);

        // ensure start button is visible, only interactable for host
        startGameButton.gameObject.SetActive(true);
        startGameButton.interactable = NetworkManager.Singleton.IsHost;
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t request) {
        SteamMatchmaking.JoinLobby(request.m_steamIDLobby);
        manualJoinPanel.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
    }

    private void OnLobbyMatchList(LobbyMatchList_t result) {
        if (result.m_nLobbiesMatching <= 0) {
            Debug.LogError("No steam lobbies found with that relay code");
            return;
        }

        var foundLobby = SteamMatchmaking.GetLobbyByIndex(0);
        SteamMatchmaking.JoinLobby(foundLobby);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t result) {
        if (result.m_ulSteamIDLobby != (ulong)currentLobbyID) return;

        var steamID = new CSteamID(result.m_ulSteamIDUserChanged);
        uint flags = result.m_rgfChatMemberStateChange;

        if ((flags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0) {
            AddPlayerProfile(steamID);
            return;
        }

        bool left = (flags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0;
        if (left || (flags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0) {
            if (lobbyMenuCanvas?.gameObject.activeSelf == true) {
#if !UNITY_EDITOR
                RefreshLobbyProfiles();
#endif
            }

            var owner = SteamMatchmaking.GetLobbyOwner(currentLobbyID);
            if (steamID == owner && !IsOwner) {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene(MAIN_MENU_SCENE);
            }
        }
    }

    public void SetRelayJoinCodeManual() {
        SetRelayJoinCode(unityRelayJoinCode);
    }

    public void SetRelayJoinCode(string joinCode) {
        if (currentLobbyID == CSteamID.Nil) return;
        SteamMatchmaking.SetLobbyData(currentLobbyID, "relay_join_code", joinCode);
    }

    public string GetRelayJoinCode() {
        if (currentLobbyID == CSteamID.Nil) return null;
        return SteamMatchmaking.GetLobbyData(currentLobbyID, "relay_join_code");
    }

    public void InviteFriendToLobby(CSteamID friendSteamID) {
        if (currentLobbyID == CSteamID.Nil) return;
        SteamMatchmaking.InviteUserToLobby(currentLobbyID, friendSteamID);
    }

    public void OpenInviteDialog() {
#if !UNITY_EDITOR
        if (currentLobbyID != CSteamID.Nil) {
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
        }
#endif
    }

    private void ShutdownSteamLobby() {
        if (currentLobbyID != CSteamID.Nil) {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = CSteamID.Nil;
        }
    }

    private void AddPlayerProfile(CSteamID playerID) {
        int imgId = SteamFriends.GetLargeFriendAvatar(playerID);
        Texture2D tex = GetSteamImageAsTexture(imgId);

        var go = Instantiate(playerProfilePrefab, playerProfileDisplayArea.transform);
        var raw = go.GetComponentInChildren<RawImage>();
        if (raw != null && tex != null) raw.texture = tex;
    }

    private void RefreshLobbyProfiles() {
        foreach (Transform child in playerProfileDisplayArea.transform)
            Destroy(child.gameObject);

        int count = (int)SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        for (int i = 0; i < count; i++) {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
            AddPlayerProfile(member);
        }
    }

    private Texture2D GetSteamImageAsTexture(int iImage) {
        if (iImage < 0) return null;

        SteamUtils.GetImageSize(iImage, out uint w, out uint h);
        if (w == 0 || h == 0) return null;

        byte[] buf = new byte[w * h * 4];
        SteamUtils.GetImageRGBA(iImage, buf, buf.Length);

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(buf);
        tex.Apply();
        return tex;
    }
    #endregion

    #region Relay Functions
    public async void CreateRelay() {
#if !UNITY_EDITOR
        ShutdownSteamLobby();
#endif

        try {
            var allocation = await RelayService.Instance.CreateAllocationAsync(3);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            unityRelayJoinCode = joinCode;
            manualJoinCodeText.gameObject.SetActive(true);
            manualJoinCodeText.text = joinCode;

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

#if !UNITY_EDITOR
            CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
            SetRelayJoinCode(joinCode);
#endif

#if UNITY_EDITOR
            startGameButton.gameObject.SetActive(true);
            startGameButton.interactable = true;
#endif
        }
        catch (RelayServiceException) { }
    }

    public async void JoinRelay(string joinCode) {
        try {
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

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
        catch (RelayServiceException) { }
    }
    #endregion

    #region RPC Functions
    [ClientRpc]
    private void StartGameClientRpc(ClientRpcParams rpcParams = default) {
        lobbyMenuCanvas?.gameObject.SetActive(false);
        playerUICanvas?.gameObject.SetActive(true);

        var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localObj != null) {
            var pc = localObj.GetComponent<PlayerController>();
            if (pc != null) pc.movementRestricted = false;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    [ClientRpc]
    private void DoorOpenClientRpc(ClientRpcParams rpcParams = default) {
        var doorScript = startingDoor.GetComponent<FrontDoorAnimationController>();
        if (doorScript != null) doorScript.TriggerPlay();
    }

    [ClientRpc]
    private void ReceiveSteamLobbyIDClientRpc(ulong lobbySteamID, ClientRpcParams rpcParams = default) {
        var receieveLobbyID = new CSteamID(lobbySteamID);
        alreadyInSteamLobby = true;
        SteamMatchmaking.JoinLobby(receieveLobbyID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSteamLobbyIDServerRpc(ServerRpcParams rpcParams = default) {
        if (currentLobbyID == CSteamID.Nil) return;

        var sender = rpcParams.Receive.SenderClientId;
        var clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } }
        };
        ReceiveSteamLobbyIDClientRpc(currentLobbyID.m_SteamID, clientRpcParams);
    }
    #endregion

    #region Helper Functions
    public void HostPressedStart() {
        OnGameStarted.Invoke();
        StartGameClientRpc();
        DoorOpenClientRpc();
    }

    private void HandleNetworkShutdown(bool _) {
        ShutdownSteamLobby();
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }

    public void ManualExceptionNetworkShutdown() {
        NetworkManager.Singleton.Shutdown();
        ShutdownSteamLobby();
    }
    #endregion
}
