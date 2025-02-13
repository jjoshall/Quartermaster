using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;


public class PocketInventory : NetworkBehaviour
{
    // Script for the pocket inventory prefab.
        // Handles teleportation, return position, cooldown.
        // Singleton (there is only one pocket inventory).

    // Struct for pairing player netObj with a vector3 return position.

    public static PocketInventory instance;
    void Awake(){
        // Singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Vector3 teleportPosition; // static. doesn't need to be networked.
    // public float timeEnteredPocket;
    public NetworkVariable<double> timeEnteredPocketNetworkVar = new NetworkVariable<double>(0);
    private static readonly float MAX_TIME_IN_POCKET = 10.0f;
    private static readonly float RADIUS_OF_POCKET = 10.0f;

    // private GameObject playerInPocket;
    private NetworkList<NetworkObjectReference> playersInPocket = new NetworkList<NetworkObjectReference>();
    // dict mapping networkobjectreference to a vector3 return position
    private Dictionary<NetworkObjectReference, Vector3> playerReturnPositions = new Dictionary<NetworkObjectReference, Vector3>();

    public NetworkObjectReference n_storedKeyObj; // keep reference to key WorldItem if it's dropped inside the pocket.
    public NetworkVariable<bool> n_droppedPortalKeyInPocket = new NetworkVariable<bool>(false);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playersInPocket = new NetworkList<NetworkObjectReference>();
        timeEnteredPocketNetworkVar.Value = 0;
        teleportPosition = this.transform.position + new Vector3 (0, 2, 0);
        n_storedKeyObj = new NetworkObjectReference();
    }

    // Update is called once per frame
    void Update()
    {
        if (playersInPocket.Count > 0)
        {
            if (NetworkManager.Singleton.ServerTime.Time - timeEnteredPocketNetworkVar.Value > MAX_TIME_IN_POCKET)
            {
                foreach (NetworkObjectReference player in playersInPocket){
                    if (player.TryGet(out NetworkObject playerObj)){
                        ReturnToPreviousPositionClientRpc(playerObj);
                    }
                }
            }
        }
    }

    // public GameObject playerInsidePocket(){
    //     return playerInPocket;
    // }

    [ClientRpc]
    public void TeleportToPocketClientRpc(NetworkObjectReference userRef){

        // if (playerInPocket != null)
        if (playersInPocket.Count > 0)
        {
            Debug.Log ("already a player in pocket");
            return;
        }

        if (userRef.TryGet(out NetworkObject user))
        {
            playerReturnPositions[userRef] = user.transform.position; // save return spot
            TeleportUserToPositionClientRpc(userRef, teleportPosition); // teleport
            playersInPocket.Add(userRef);
            timeEnteredPocketNetworkVar.Value = NetworkManager.Singleton.ServerTime.Time;
        }
    }

    [ClientRpc]
    private void TeleportUserToPositionClientRpc(NetworkObjectReference userRef, Vector3 position){
        if (userRef.TryGet(out NetworkObject user))
        {
            while (user.GetComponent<PlayerController>().toggleCharacterController()){
                // until toggle returns false for toggled off.
            }
            user.transform.position = position;
            while (!user.GetComponent<PlayerController>().toggleCharacterController()){
                // until toggle returns true for toggled on.
            }
        }
    }

    [ClientRpc]
    public void ReturnToPreviousPositionClientRpc(NetworkObject playerObj){
        NetworkObjectReference playerRef = playerObj.GetComponent<NetworkObjectReference>();
        if (playerReturnPositions.TryGetValue(playerRef, out Vector3 playerReturnPosition))
        {
            TeleportUserToPositionClientRpc(playerRef, playerReturnPosition);
            if (n_storedKeyObj.TryGet(out NetworkObject keyObj))
            {
                Debug.Log("dropped key returned at user's position");
                keyObj.transform.position = playerReturnPosition;
            }
            playersInPocket.Remove(playerRef);
            n_storedKeyObj = new NetworkObjectReference();
        }
    }

    public void ReturnAllPlayersClientRpc(){
        foreach (NetworkObjectReference player in playersInPocket){
            if (player.TryGet(out NetworkObject playerObj)){
                ReturnToPreviousPositionClientRpc(playerObj);
            }
        }
    }

    [ClientRpc]
    public void FindDroppedKeyClientRpc(){
        // physics overlap sphere, find dropped key
        Collider[] colliders = Physics.OverlapSphere(teleportPosition, RADIUS_OF_POCKET);
        foreach (Collider col in colliders){
            if (col.gameObject.GetComponent<PocketInventoryPortalKey>() != null){
                n_storedKeyObj = col.gameObject.GetComponent<NetworkObject>().GetComponent<NetworkObjectReference>();
                Debug.Log ("found dropped key: " + n_storedKeyObj);
                return;
            }
        }
    }

    public bool PlayerIsInPocket(NetworkObjectReference playerRef){
        return playersInPocket.Contains(playerRef);
    }
}
