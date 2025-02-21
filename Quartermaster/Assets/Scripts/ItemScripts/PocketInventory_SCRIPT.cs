using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;


public class PocketInventory : NetworkBehaviour {
    // Script for the pocket inventory prefab.
        // Handles teleportation, return position, cooldown.
        // Singleton (there is only one pocket inventory).

    // Struct for pairing player netObj with a vector3 return position.

    public static PocketInventory instance;

    // public float timeEnteredPocket;
    public NetworkVariable<double> timeEnteredPocketNetworkVar = new NetworkVariable<double>(0);
    public NetworkObjectReference n_storedKeyObj; // keep reference to key WorldItem if it's dropped inside the pocket.
    public NetworkVariable<bool> n_droppedPortalKeyInPocket = new NetworkVariable<bool>(false);
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Vector3 _teleportPosition; // static. doesn't need to be networked.
    private NetworkList<NetworkObjectReference> _playersInPocket = new NetworkList<NetworkObjectReference>();
    // dict mapping networkobjectreference to a vector3 return position
    private Dictionary<NetworkObjectReference, Vector3> _playerReturnPositions = new Dictionary<NetworkObjectReference, Vector3>();

    private static readonly float MAX_TIME_IN_POCKET = 10.0f;
    private static readonly float RADIUS_OF_POCKET = 10.0f;
    // private GameObject playerInPocket;

    void Awake() {
        // Singleton
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        _playersInPocket = new NetworkList<NetworkObjectReference>();
        timeEnteredPocketNetworkVar.Value = 0;
        _teleportPosition = this.transform.position + new Vector3 (0, 2, 0);
        n_storedKeyObj = new NetworkObjectReference();
    }

    // Update is called once per frame
    void Update() {
        if (_playersInPocket.Count > 0) {
            if (NetworkManager.Singleton.ServerTime.Time - timeEnteredPocketNetworkVar.Value > MAX_TIME_IN_POCKET) {
                ReturnAllPlayersClientRpc();
            }
        }
    }

    // public GameObject playerInsidePocket(){
    //     return playerInPocket;
    // }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportToPocketServerRpc(NetworkObjectReference userRef){

        // if (playerInPocket != null)
        if (_playersInPocket.Count > 0) {
            Debug.Log ("already a player in pocket");
            return;
        }

        if (userRef.TryGet(out NetworkObject user)) {

            _playerReturnPositions[userRef] = user.transform.position; // save return spot
            debugMsgClientRpc("attempting teleport");
            TeleportUserToPositionClientRpc(userRef, _teleportPosition); // teleport
            debugMsgClientRpc("adding userRef to _playersInPocket");
            _playersInPocket.Add(userRef);
            debugMsgClientRpc("setting timeEntered var");
            timeEnteredPocketNetworkVar.Value = NetworkManager.Singleton.ServerTime.Time;
            debugMsgClientRpc("completed teleport attempt teleport");

        }
    }
    [ClientRpc]
    private void debugMsgClientRpc (string msg  ){
        Debug.Log(msg);
    }


    [ServerRpc(RequireOwnership = false)]
    public void clearDroppedKeyServerRpc() {
        n_droppedPortalKeyInPocket.Value = false;
        n_storedKeyObj = new NetworkObjectReference();
    }

    [ClientRpc]
    private void TeleportUserToPositionClientRpc(NetworkObjectReference userRef, Vector3 position) {
        if (userRef.TryGet(out NetworkObject user)) {
            GameObject playerObj = user.gameObject;
            if (playerObj == null) return;

            // turn off interpolation and char controller temporarily for teleport
            playerObj.GetComponent<NetworkTransform>().Interpolate = false;
            while (playerObj.GetComponent<PlayerController>().toggleCharacterController()){}

            playerObj.transform.position = position; // teleport player

            while (!playerObj.GetComponent<PlayerController>().toggleCharacterController()){}
            playerObj.GetComponent<NetworkTransform>().Interpolate = true;
        }
    }

    [ClientRpc]
    public void ReturnToPreviousPositionClientRpc(NetworkObjectReference n_playerObjRef) {
        // Grab return position from dictionary playerReturnPositions
        if (_playerReturnPositions.TryGetValue(n_playerObjRef, out Vector3 playerReturnPosition)) {
            TeleportUserToPositionClientRpc(n_playerObjRef, playerReturnPosition);

            // Return dropped portal key if exists.
            if (n_droppedPortalKeyInPocket.Value) {
                if (n_storedKeyObj.TryGet(out NetworkObject keyObj)) {
                    Debug.Log("dropped key returned at user's position");
                    keyObj.transform.position = playerReturnPosition;
                }
            }

            _playersInPocket.Remove(n_playerObjRef);
            n_storedKeyObj = new NetworkObjectReference();
        }
    }

    [ClientRpc]
    public void ReturnAllPlayersClientRpc() {
        foreach (NetworkObjectReference n_player in _playersInPocket){
            ReturnToPreviousPositionClientRpc(n_player);
        }
    }

    [ClientRpc]
    public void FindDroppedKeyClientRpc() {
        // physics overlap sphere, find dropped key
        Collider[] colliders = Physics.OverlapSphere(_teleportPosition, RADIUS_OF_POCKET);
        foreach (Collider col in colliders) {
            if (col.gameObject.GetComponent<PocketInventoryPortalKey>() != null) {
                n_storedKeyObj = col.gameObject.GetComponent<NetworkObject>().GetComponent<NetworkObjectReference>();
                Debug.Log ("found dropped key: " + n_storedKeyObj);
                return;
            }
        }
    }

    public bool PlayerIsInPocket(NetworkObjectReference playerRef) {
        return _playersInPocket.Contains(playerRef);
    }
}
