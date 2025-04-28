using Steamworks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class IPRelay : MonoBehaviour
{
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private LobbyManagerUI lobbyManagerUI;
    [SerializeField] private GameObject loadingPanel;

    private ulong  m_SteamId;
    private string m_UgsPlayerId;

    private void Awake()
    {
        if (!SteamAPI.Init())
        {
            Debug.LogError("SteamAPI.Init() failed – check steam_appid.txt");
            enabled = false;
            return;
        }
        Debug.Log("SteamAPI initialized");
        m_SteamId = SteamUser.GetSteamID().m_SteamID;
        Debug.Log($"Logged in to Steam as {m_SteamId}");
    }

    private async void Start()
    {
        // Initialize UGS (including Relay)
        await UnityServices.InitializeAsync();

        // Sign in anonymously so RelayService calls will work
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in to UGS anon as {AuthenticationService.Instance.PlayerId}");
        }
        m_UgsPlayerId = AuthenticationService.Instance.PlayerId;

        // Now you can CreateRelay/JoinRelay as before
    }

    private void Update()
    {
        SteamAPI.RunCallbacks();
    }

    public async void CreateRelay()
    {
        await LoadingScreenManager.Instance.ShowLoadingScreenAsync();
        try
        {
            var alloc    = await RelayService.Instance.CreateAllocationAsync(3);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("Relay join code: " + joinCode);


            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData
            );
            NetworkManager.Singleton.StartHost();
            StartCoroutine(LoadingScreenManager.Instance.HandleDelayedUIEnable(joinCode));
        }
        catch
        {
            LoadingScreenManager.Instance.DisableLoadingPanel();
            Debug.LogError("Failed to create Relay");
        }
    }

    public async void JoinRelay(string joinCode)
    {
        await LoadingScreenManager.Instance.ShowLoadingScreenAsync();
        try
        {
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var utp       = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetClientRelayData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );
            NetworkManager.Singleton.StartClient();
            StartCoroutine(LoadingScreenManager.Instance.HandleDelayedUIEnable(joinCode));
        }
        catch
        {
            LoadingScreenManager.Instance.DisableLoadingPanel();
            Debug.LogError("Failed to join Relay");
        }
    }


    public async void JoinLobbyThroughSteam() {
    }
}
