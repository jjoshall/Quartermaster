using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.ReorderableList;

public class PortalKey_MONO : MonoItem
{

    #region Item Settings
    [Header("Item Settings")]
    [SerializeField] private float _teleportRadius = 20.0f;
    [SerializeField] private float _teleportRange = 5.0f;
    [SerializeField, Tooltip("Excluding self(player)")] private int _maxTeleportableItems = 5;
    #endregion

    #region InternalVars
    private GameObject _playerCamera;
    private List<GameObject> _playersToTeleport;
    private List<GameObject> _itemsToTeleport;
    private bool _isTeleporting = false;
    #endregion

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
        LayerMask validTpTargets = LayerMask.GetMask("Player", "Item"); // Add teleportable layer to teleportable items.
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

        NetworkObject n_user = user.GetComponent<NetworkObject>();
        if (n_user == null) {
            Debug.LogError("PortalKey_MONO: ButtonRelease() user has no NetworkObject component.");
            return;
        }
        if (PocketInventory.instance.PlayerIsInPocket(n_user)){
            Vector3 itemDestination = PocketInventory.instance.GetTeleportDestination(n_user);
            TeleportItems(itemDestination);
            Return();
        }
        else {
            Vector3 itemDestination = PocketInventory.instance.GetTeleportDestination(n_user);
            TeleportItems(itemDestination);
            Teleport();
        }
        _isTeleporting = false;
    }

    private void Teleport(){
        foreach (GameObject player in _playersToTeleport){
            NetworkObject n_player = player.GetComponent<NetworkObject>();
            if (n_player == null) {
                Debug.LogError("PortalKey_MONO: Teleport() player has no NetworkObject component.");
                continue;
            }
            PocketInventory.instance.TeleportToPocketServerRpc (n_player);
        }
        _playersToTeleport.Clear();
    }
    private void Return(){
        PocketInventory.instance.ReturnAllPlayersServerRpc();
    }

    private void TeleportItems(Vector3 destination){
        foreach (GameObject item in _itemsToTeleport){
            item.transform.position = destination;
        }

        _itemsToTeleport.Clear();
    }

    public override void SwapCancel(GameObject user)
    {
        if (NullChecks(user)) {
            Debug.LogError("PortalKey_MONO: SwapCancel() NullChecks failed.");
            return;
        }

        _isTeleporting = false;
    }

    #region Helpers
    #endregion 

    public void AddRaycastToTp(Transform camera, float distance, LayerMask layerMask){
        if (camera == null) {
            Debug.LogError ("PortalKey_MONO: RaycastFromCamera() camera is null.");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(camera.position, camera.forward, out hit, distance, layerMask)){
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

        if (_playersToTeleport == null) {
            _playersToTeleport = new List<GameObject>();
        }
        if (_itemsToTeleport == null) {
            _itemsToTeleport = new List<GameObject>();
        }

        if (obj.CompareTag("Player")){
            _playersToTeleport.Add(obj);
            AddBlueOutline(obj);
        } else if (obj.CompareTag("Item")){
            _itemsToTeleport.Add(obj);
            AddBlueOutline(obj);
        } else {
            Debug.LogWarning ("PortalKey_MONO: AddObjToTp() obj is not a player or item.");
        }
    }

    // Called on OnTriggerExit.
    // Remove single objs from TP list. 
    // For teleport execute and swapcancel, delete entire list instead
    public void RemoveObjFromTp(GameObject obj){

        RemoveBlueOutline (obj);
    }

    public void RemoveAllObjs(){

        foreach (GameObject obj in _playersToTeleport){
            RemoveBlueOutline(obj);
        }

        foreach (GameObject obj in _itemsToTeleport){
            RemoveBlueOutline(obj);
        }
    }

    private void AddBlueOutline(GameObject obj){

    }

    private void RemoveBlueOutline(GameObject obj){

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
}
