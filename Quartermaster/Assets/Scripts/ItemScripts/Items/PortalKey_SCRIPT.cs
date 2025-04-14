using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.ReorderableList;

public class PortalKey_MONO : Item
{

    #region Item Settings
    [Header("Item Settings")]
    [SerializeField] private float _teleportRadius = 20.0f;
    [SerializeField] private float _teleportRange = 5.0f;
    [SerializeField, Tooltip("Excluding self(player)")] private int _maxTeleportableItems = 5;
    #endregion

    #region InternalVars
    private GameObject _playerCamera;
    private List<GameObject> _playersToTeleport = new List<GameObject>();
    private List<GameObject> _itemsToTeleport = new List<GameObject>();
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

        NetworkObject n_user = user.GetComponent<NetworkObject>();
        if (n_user == null) {
            Debug.LogError("PortalKey_MONO: ButtonRelease() user has no NetworkObject component.");
            return;
        }
        if (PocketInventory.instance.PlayerIsInPocket(n_user)){
            Debug.Log ("PortalKey_MONO: ButtonRelease() Teleporting players to world.");
            Vector3 itemDestination = PocketInventory.instance.GetTeleportDestination(n_user);
            TeleportItems(itemDestination);
            Return();
            RemoveAllObjs();
        }
        else {
            Debug.Log ("PortalKey_MONO: ButtonRelease() Teleporting players to pocket.");
            Vector3 itemDestination = PocketInventory.instance.GetTeleportDestination(n_user);
            TeleportItems(itemDestination);
            Teleport(user);
            RemoveAllObjs();
        }
        _isTeleporting = false;
    }

    private void Teleport(GameObject user){
        foreach (GameObject player in _playersToTeleport){
            NetworkObject n_player = player.GetComponent<NetworkObject>();
            if (n_player == null) {
                Debug.LogError("PortalKey_MONO: Teleport() player has no NetworkObject component.");
                continue;
            }
            PocketInventory.instance.TeleportToPocketServerRpc (player);
        }
        _playersToTeleport.Clear();
        PocketInventory.instance.TeleportToPocketServerRpc (user);
    }
    private void Return(){
        PocketInventory.instance.ReturnAllPlayersServerRpc();
    }

    private void TeleportItems(Vector3 destination){
        foreach (GameObject item in _itemsToTeleport){
            item.transform.position = destination;
        }
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

    public void RemoveAllObjs(){

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
}
