using Unity.Netcode;
using Steamworks;
using TMPro;

public class SteamPlayerInfo : NetworkBehaviour {
    public NetworkVariable<ulong> SteamId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private TextMeshProUGUI _nameplate;

    private void Awake() {
        // cache player name text component (need to add)
        //_nameplate = GetComponentInChildren<TextMeshProUGUI>();
    }

    public override void OnNetworkSpawn() {
        SteamId.OnValueChanged += HandleNewSteamId;

        if (IsOwner) {
            SubmitSteamIdServerRpc(SteamUser.GetSteamID().m_SteamID);
        }

        if (SteamId.Value != 0) {
            HandleNewSteamId(0, SteamId.Value);
        }
    }

    private void HandleNewSteamId(ulong oldId, ulong newId) {
        var sid = new CSteamID(newId);
        // look up the friend’s name

        string name = SteamFriends.GetFriendPersonaName(sid);
        _nameplate.text = name;
        // TODO: avatar fetch
        // 
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitSteamIdServerRpc(ulong steam64, ServerRpcParams rpcParams = default) {
        SteamId.Value = steam64;
    }
}
