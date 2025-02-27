using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;


public class PocketInventory : NetworkBehaviour {
    // Script for the pocket inventory prefab.
        // Handles teleportation, return position, cooldown.
        // Singleton (there is only one pocket inventory).

    // Struct for pairing player netObj with a vector3 return position.

    #region Variables
    public static PocketInventory instance;

    // public float timeEnteredPocket;
    public NetworkVariable<double> n_timeEnteredPocketNetworkVar = new NetworkVariable<double>(0);
    public NetworkObjectReference n_storedKeyObj; // keep reference to key WorldItem if it's dropped inside the pocket.
    public NetworkVariable<bool> n_droppedPortalKeyInPocket = new NetworkVariable<bool>(false);
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Vector3 _teleportPosition; // static. doesn't need to be networked.
    private List<NetworkObjectReference> _playersInPocket = new List<NetworkObjectReference>();
    // dict mapping networkobjectreference to a vector3 return position
    private Dictionary<NetworkObjectReference, PlayerPosition> _playerReturnPositions = new Dictionary<NetworkObjectReference, PlayerPosition>();

    private struct PlayerPosition : INetworkSerializable {
        public Vector3 position;
        public Quaternion rotation;    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
        }
    }
    private static readonly float MAX_TIME_IN_POCKET = 10.0f;
    private static readonly float RADIUS_OF_POCKET_DETECTION = 20.0f;
    // private GameObject playerInPocket;
    #endregion

    #region Startup
    void Awake() {
        // Singleton
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    // Start works here because the PocketInventory prefab is placed in the scene by default.
    // Instead of spawned by the server during runtime. Don't need to move to OnNetworkSpawn()
    void Start() {
        _playersInPocket = new List<NetworkObjectReference>();
        n_timeEnteredPocketNetworkVar.Value = 0;
        _teleportPosition = this.transform.position + new Vector3 (0, 2, 0);
        n_storedKeyObj = new NetworkObjectReference();
    }
    #endregion
    // Update is called once per frame
    void Update() {
        if (_playersInPocket.Count > 0) {
            if (NetworkManager.Singleton.ServerTime.Time - n_timeEnteredPocketNetworkVar.Value > MAX_TIME_IN_POCKET) {
                ReturnAllPlayersClientRpc();
            }
        }
    }

    #region Teleport
    [ServerRpc(RequireOwnership = false)]
    public void TeleportToPocketServerRpc(NetworkObjectReference userRef){

        if (userRef.TryGet(out NetworkObject user)) {

            PlayerPosition savePos = new PlayerPosition();
            savePos.position = user.transform.position;
            savePos.rotation = user.transform.rotation;
            _playerReturnPositions[userRef] = savePos; // save return spot

            PlayerPosition teleportSpot = new PlayerPosition();
            teleportSpot.position = _teleportPosition;
            teleportSpot.rotation = user.transform.rotation;

            TeleportUserToPositionClientRpc(userRef, teleportSpot); // teleport
            _playersInPocket.Add(userRef);
            n_timeEnteredPocketNetworkVar.Value = NetworkManager.Singleton.ServerTime.Time;

        }
    }

    [ClientRpc]
    private void TeleportUserToPositionClientRpc(NetworkObjectReference userRef, PlayerPosition teleportPosition) {
        if (userRef.TryGet(out NetworkObject user)) {
            GameObject playerObj = user.gameObject;
            if (playerObj == null) return;

            // turn off interpolation and char controller temporarily for teleport
            playerObj.GetComponent<NetworkTransform>().Interpolate = false;
            playerObj.GetComponent<PlayerController>().disableCharacterController();

            playerObj.transform.position = teleportPosition.position; // teleport player
            playerObj.transform.rotation = teleportPosition.rotation;

            playerObj.GetComponent<PlayerController>().enableCharacterController();
            playerObj.GetComponent<NetworkTransform>().Interpolate = true;
        }
    }

    [ClientRpc]
    public void ReturnToPreviousPositionClientRpc(NetworkObjectReference n_playerObjRef) {
        // Grab return position from dictionary playerReturnPositions
        if (_playerReturnPositions.TryGetValue(n_playerObjRef, out PlayerPosition playerReturnPosition)) {
            
            TeleportUserToPositionClientRpc(n_playerObjRef, playerReturnPosition);

            // Return dropped portal key if exists.
            if (n_droppedPortalKeyInPocket.Value) {
                if (n_storedKeyObj.TryGet(out NetworkObject keyObj)) {
                    Debug.Log("dropped key returned at user's position");
                    keyObj.transform.position = playerReturnPosition.position;
                }
            }

            _playersInPocket.Remove(n_playerObjRef);
            n_storedKeyObj = new NetworkObjectReference();
        }
    }

    #endregion
    #region TeleportHelpers
    [ServerRpc(RequireOwnership = false)]
    public void clearDroppedKeyServerRpc() {
        n_droppedPortalKeyInPocket.Value = false;
        n_storedKeyObj = new NetworkObjectReference();
    }


    [ClientRpc]
    public void ReturnAllPlayersClientRpc() {
        debugMsgClientRpc("Returning all players, PlayerCount = " + _playersInPocket.Count);
        List<NetworkObjectReference> playersToRemove = new List<NetworkObjectReference>(_playersInPocket);  
        foreach (NetworkObjectReference n_player in playersToRemove){
            debugMsgClientRpc("Returning player: " + n_player);
            ReturnToPreviousPositionClientRpc(n_player);
        }
    }
    #endregion
    #region Helpers
    [ClientRpc]
    public void FindDroppedKeyClientRpc() {
        // physics overlap sphere, find dropped key
        Collider[] colliders = Physics.OverlapSphere(_teleportPosition, RADIUS_OF_POCKET_DETECTION);
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

    [ClientRpc]
    private void debugMsgClientRpc (string msg  ){
        Debug.Log(msg);
    }

    // public bool PlayerIsInPocket(GameObject user) {
    //     NetworkObject userNetobj = user.GetComponent<NetworkObject>();
    //     return _playersInPocket.Contains(userNetobj);
    // }
    #endregion
}   
