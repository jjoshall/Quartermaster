using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.ReorderableList;
using Unity.Netcode.Components; // Add this for NetworkTransform
using Unity.VisualScripting;
using UnityEditor.PackageManager;

public class PortalKey_MONO : Item
{

    #region Item Settings
    [Header("Item Settings")]
    [SerializeField] private float _teleportRadius = 20.0f;
    [SerializeField] private float _teleportRange = 5.0f;
    [SerializeField, Tooltip("Excluding self(player)")] private int _maxTeleportableItems = 5;

    [SerializeField] 
    private GameObject _teleportDestination; // static. don't change this.
    [SerializeField, Tooltip("radius around teleport position to consider 'inside' the teleport destination.")] 
    private float _destinationRadius = 10.0f; // 
    [SerializeField] 
    private Vector3 _teleportOffset = new Vector3(0, 1, 0); // offset from teleport destination to avoid falling through ground.
    
    private NetworkVariable<Vector3> _teleportTarget = new NetworkVariable<Vector3>();
    
    #endregion

    #region InternalVars
    // server synced variables. used without reference to an owning player object. 
    private NetworkVariable<NetworkObjectReference> n_lastOwner = new NetworkVariable<NetworkObjectReference>(); // used to return key if dropped in storage regardless of owner
    private NetworkList<NetworkObjectReference> n_playersInPocket = new NetworkList<NetworkObjectReference>(); // used to return players regardless of owner
    private NetworkList<Vector3> n_returnPositions = new NetworkList<Vector3>();
    private NetworkList<Quaternion> n_returnRotations = new NetworkList<Quaternion>();
            // can't seem to put a struct in a networklist. 
            // if someone can figure that out, go ahead and refactor 
            // n_returnPositions/Rotations and n_playersInPocket into one struct

    // local client variables. cleared on drop.
    private List<GameObject> _playersToTeleport = new List<GameObject>();
    private List<GameObject> _itemsToTeleport = new List<GameObject>();
    private bool _isTeleporting = false;

    [SerializeField] private string destinationTag = "PortalDestination";
    public override void OnNetworkSpawn()
    {
        // Only once, on both host & clients
        if (_teleportDestination == null)
        {
            _teleportDestination = GameObject.FindWithTag(destinationTag);
        }
    }

    void Start() {
        if (!IsServer) return; // only run on server.
        n_playersInPocket = new NetworkList<NetworkObjectReference>();
        // n_timeEnteredPocketNetworkVar.Value = 0;
        _teleportTarget.Value = _teleportDestination.transform.position + new Vector3 (0, 2, 0);
        Debug.Log ("PortalKey_MONO: Start() _teleportTarget set to " + _teleportTarget.Value.ToString());
    }

    #endregion

    #region BaseOverrides
    #endregion

    public override void PickUp(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: PickUp() NullChecks failed.");
            return;
        }

        // set last owner to user.
        NetworkObject n_user = user.GetComponent<NetworkObject>();
        if (n_user == null) {
            Debug.LogError("PortalKey_MONO: PickUp() user has no NetworkObject component.");
            return;
        }
        SetLastOwnerServerRpc(n_user); // set last owner to user.
    }

    public override void ButtonUse(GameObject user)
    {
        // Initiate teleportation obj selection
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        _isTeleporting = true;
    }

    public override void ButtonHeld(GameObject user)
    {
        // Select items and players while held(?)
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: ButtonHeld() NullChecks failed.");
            return;
        }

        if (!_isTeleporting) return; // occurs if swapped into item while holding.

        Transform camera = user.GetComponent<Inventory>().orientation;
        LayerMask validTpTargets = LayerMask.GetMask("Player", "Items"); // Add teleportable layer to teleportable items.
        AddRaycastToTp(camera, _teleportRange, validTpTargets);
    }

    public override void ButtonRelease(GameObject user)
    {
        // Teleport
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: ButtonRelease() NullChecks failed.");
            return;
        }

        if (!_isTeleporting) return;

        if (_teleportDestination == null) {
            Debug.LogError("PortalKey_MONO: ButtonRelease() _teleportDestination is null.");
            return;
        }


        NetworkObject n_user = user.GetComponent<NetworkObject>();
        if (n_user == null) {
            Debug.LogError("PortalKey_MONO: ButtonRelease() user has no NetworkObject component.");
            return;
        }
        if (PlayerIsInPocket(user)){
            Debug.Log ("PortalKey_MONO: ButtonRelease() Teleporting players to world.");
            Return();
            TeleportItems();
            RemoveAllObjOutlines();
        }
        else {
            Debug.Log ("PortalKey_MONO: ButtonRelease() Teleporting players to pocket.");
            TeleportAll();
            TeleportItems();
            RemoveAllObjOutlines();
        }
        _isTeleporting = false;
    }

    public override void SwapCancel(GameObject user)
    {
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: SwapCancel() NullChecks failed.");
            return;
        }

        RemoveAllObjOutlines();
        _isTeleporting = false;
    }

    public override void Drop(GameObject user)
    {
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: Drop() NullChecks failed.");
            return;
        }

        RemoveAllObjOutlines();
        _isTeleporting = false;
        _playersToTeleport.Clear();
        _itemsToTeleport.Clear();
    }
    #region MainTeleport
    #endregion 
    
    // Teleport all players in _playersToTeleport to the pocket.
    // Save return position data in synced networklists.
    private void TeleportAll(){
        Vector3 offsetDestination = _teleportDestination.transform.position + _teleportOffset;
        foreach (GameObject player in _playersToTeleport){
            NetworkObject n_player = player.GetComponent<NetworkObject>();
            if (n_player == null) {
                Debug.LogError("PortalKey_MONO: Teleport() player has no NetworkObject component.");
                continue;
            }
            SaveReturnPosition (player);
            TeleportPlayerServerRpc (n_player, offsetDestination, Quaternion.Euler(0, 0, 0));
        }

        NetworkObject n_lastOwnerObj;
        // get lastOwner from n_lastOwnerReference
        if (n_lastOwner.Value.TryGet(out n_lastOwnerObj)){
            SaveReturnPosition (n_lastOwnerObj.gameObject);
            TeleportPlayerServerRpc (n_lastOwnerObj, offsetDestination, Quaternion.Euler(0, 0, 0));
        } else {
            Debug.LogError("PortalKey_MONO: Teleport() n_lastOwnerReference is null.");
            return;
        }
    }

    // Returns all players in n_playersInPocket to index positions.
    private void Return(){
        for (int i = 0; i < n_playersInPocket.Count; i++){
            NetworkObjectReference n_player = n_playersInPocket[i];
            NetworkObject n_playerObj;
            if (n_player.TryGet(out n_playerObj)){
                // GameObject player = n_playerObj.gameObject;
                Vector3 returnPosition = n_returnPositions[i];
                Quaternion returnRotation = n_returnRotations[i];
                TeleportPlayerServerRpc (n_player, returnPosition, returnRotation);
            } else {
                Debug.LogError("PortalKey_MONO: Return() n_player is null.");
            }
        }
        ClearPocketServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClearPocketServerRpc(){
        // clear the pocket of the player.
        n_playersInPocket.Clear();
        n_returnPositions.Clear();
        n_returnRotations.Clear();
    }


    #region Teleport Helpers
    #endregion 
    private void SaveReturnPosition (GameObject user){
        // networkobjectreference get
        NetworkObject n_user = user.GetComponent<NetworkObject>();
        // get position and rotation of player
        Vector3 position = user.transform.position;
        Quaternion rotation = user.transform.rotation;

        // add data to the network lists
        AddPlayerToPocketServerRpc(n_user, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerToPocketServerRpc(NetworkObjectReference player, Vector3 position, Quaternion rotation){
        n_playersInPocket.Add(player);
        n_returnPositions.Add(position);
        n_returnRotations.Add(rotation);
    }

    // Spherecasts on teleport destination to check if user param is in vicinity.
    private bool PlayerIsInPocket(GameObject user){
        if (user == null) {
            Debug.LogError("PortalKey_MONO: PlayerIsInPocket() user is null.");
            return false;
        }
        foreach (NetworkObjectReference n_player in n_playersInPocket){
            NetworkObject n_playerObj;
            if (n_player.TryGet(out n_playerObj)){
                GameObject player = n_playerObj.gameObject;
                if (player == user){
                    Debug.Log ("PortalKey_MONO: PlayerIsInPocket() player is in pocket.");
                    return true;
                }
            } else {
                Debug.LogError("PortalKey_MONO: PlayerIsInPocket() n_player is null.");
            }
        }
        return false;
    }


    private void TeleportItems(){
        // tryget 
        GameObject lastOwnerObj = null;
        NetworkObject n_lastOwnerObj;
        // get lastOwner from n_lastOwnerReference
        if (n_lastOwner.Value.TryGet(out n_lastOwnerObj)){
            lastOwnerObj = n_lastOwnerObj.gameObject;
        }
        Vector3 destination = lastOwnerObj.transform.position + _teleportOffset;
        foreach (GameObject item in _itemsToTeleport){
            item.transform.position = destination;
        }
    }

    #region TeleportRPCs
    #endregion
    [ServerRpc(RequireOwnership = false)]
    private void TeleportPlayerServerRpc(NetworkObjectReference player, Vector3 destination, Quaternion rotation){
        NetworkObject n_playerObj;
        if (player.TryGet(out n_playerObj)){
            // call the client teleport for every client
            TeleportPlayerClientRpc(player, destination, rotation);
        } else {
            Debug.LogError("PortalKey_MONO: TeleportPlayerServerRpc() player is null.");
        }
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(NetworkObjectReference player, Vector3 destination, Quaternion rotation){
        if (player.TryGet(out NetworkObject n_playerObj)){
            GameObject playerObj = n_playerObj.gameObject;
                        // turn off interpolation and char controller temporarily for teleport
            playerObj.GetComponent<NetworkTransform>().Interpolate = false;
            playerObj.GetComponent<PlayerController>().disableCharacterController();

            playerObj.transform.position = destination; // teleport player
            playerObj.transform.rotation = rotation;

            playerObj.GetComponent<PlayerController>().enableCharacterController();
            playerObj.GetComponent<NetworkTransform>().Interpolate = true;
            Debug.Log ("PortalKey_MONO: TeleportPlayerClientRpc() player teleported to " + destination.ToString() + " with rotation " + rotation.ToString());
        } else {
            Debug.LogError("PortalKey_MONO: TeleportPlayerClientRpc() player is null.");
        }
    }

    #region TeleportListHelpers
    #endregion 

    public void AddRaycastToTp(Transform camera, float distance, LayerMask layerMask){
        if (camera == null) {   
            Debug.LogError ("PortalKey_MONO: RaycastFromCamera() camera is null.");
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(camera.position, camera.forward, distance, layerMask);
        if (hits.Length == 0) return; // no hits.
        foreach (RaycastHit hit in hits){
            GameObject hitObj = hit.collider.gameObject;

            if (_playersToTeleport.Contains(hitObj) || _itemsToTeleport.Contains(hitObj)){
                Debug.Log ("PortalKey_MONO: AddRaycastToTp() obj already in teleport list, ignoring.");
                continue;
            }

            Item item = hitObj.GetComponent<Item>();
            if (item != null){
                if (item.IsPickedUp){
                    Debug.Log ("PortalKey_MONO: AddRaycastToTp() item is picked up, ignoring.");
                    continue;
                }
            }

            AddObjToTp(hit.collider.gameObject);
        }
    }

    public void AddObjToTp(GameObject obj){
        if (obj == null) {
            Debug.LogError ("PortalKey_MONO: AddObjToTp() obj is null.");
            return;
        }

        if (_playersToTeleport.Count + _itemsToTeleport.Count >= _maxTeleportableItems){
            Debug.LogWarning ("PortalKey_MONO: AddObjToTp() max teleportable items reached.");
            return;
        }

        if (obj.CompareTag("Player")){
            _playersToTeleport.Add(obj);
            AddTpOutline(obj);
        } else if (obj.CompareTag("Item")){
            _itemsToTeleport.Add(obj);
            Debug.Log ("PortalKey_MONO: AddObjToTp() item, " + obj.GetComponent<Item>().uniqueID + ", added to teleport list.");
            AddTpOutline(obj);
        } else {
            Debug.LogWarning ("PortalKey_MONO: AddObjToTp() obj is not a player or item.");
        }
    }

    // Called on OnTriggerExit.
    // Remove single objs from TP list. 
    // For teleport execute and swapcancel, delete entire list instead
    public void RemoveObjFromTp(GameObject obj){
        if (obj == null) {
            Debug.LogError ("PortalKey_MONO: RemoveObjFromTp() obj is null.");
            return;
        }
        if (_playersToTeleport.Contains(obj)){
            _playersToTeleport.Remove(obj);
            Debug.Log ("PortalKey_MONO: RemoveObjFromTp() player removed from teleport list.");
        } else if (_itemsToTeleport.Contains(obj)){
            _itemsToTeleport.Remove(obj);
            Debug.Log ("PortalKey_MONO: RemoveObjFromTp() item removed from teleport list.");
        } else {
            Debug.LogWarning ("PortalKey_MONO: RemoveObjFromTp() obj not in teleport list.");
        }
        RemoveTpOutline (obj);
    }

    public void RemoveAllObjOutlines(){

        foreach (GameObject obj in _playersToTeleport){
            RemoveTpOutline(obj);
        }

        foreach (GameObject obj in _itemsToTeleport){
            RemoveTpOutline(obj);
        }

        _playersToTeleport.Clear();
        _itemsToTeleport.Clear();
    }

    private void AddTpOutline(GameObject obj){
        TeleportOutline outline = obj.GetComponent<TeleportOutline>();
        if (outline == null) {
            outline = obj.AddComponent<TeleportOutline>();
        }
        outline.OutlineWidth = 15f;
    }

    private void RemoveTpOutline(GameObject obj){
        TeleportOutline outline = obj.GetComponent<TeleportOutline>();
        if (outline != null) {
            Destroy(obj.GetComponent<TeleportOutline>());
        }
    }

    void OnTriggerExit(Collider other){
        if (other.gameObject.CompareTag("Player")){
            RemoveObjFromTp(other.gameObject);
        } else if (other.gameObject.CompareTag("Item")){
            RemoveObjFromTp(other.gameObject);
        }
    }
    
    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("PortalKey_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("PortalKey_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("PortalKey_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("PortalKey_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
    #region NetworkVarRpcs
    #endregion 
    
    [ServerRpc(RequireOwnership = false)]
    private void SetLastOwnerServerRpc(NetworkObjectReference lastOwner){
        n_lastOwner.Value = lastOwner;
    }
}
