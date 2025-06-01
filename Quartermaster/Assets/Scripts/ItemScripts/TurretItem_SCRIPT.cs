using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.BossRoom.Infrastructure;

public class TurretItem_MONO : Item
{
    [SerializeField] private GameObject _previewGameObjectPrefab; // assign in editor
    [SerializeField] private GameObject _turretObjectPrefab;
    private GameObject _previewGameObject;
    #region item settings
    [SerializeField] private float _previewDistance = 10f;
    private bool _previewIsActive = false;
    private bool _previewIsValid = false;
    private GameObject _userCamera = null;
    private Vector3 _currentPlacementPosition = Vector3.zero;

    #endregion
    #region color modifications
    [SerializeField] private Material _previewMaterial;     // assign in editor
    [SerializeField] private Color _validColor;             // assign in editor
    [SerializeField] private Color _invalidColor;           // assign in editor
    #endregion
    private NetworkObjectPool _objectPool;

    public override void OnPickUp(GameObject user)
    {
        base.OnPickUp(user);
        _previewGameObject = Instantiate(_previewGameObjectPrefab, user.transform.position, user.transform.rotation);
        _previewGameObject.SetActive(false);
        _objectPool = NetworkObjectPool.Singleton;
        if (_objectPool == null) {
            Debug.LogError("NetworkObjectPool not found.");
            return;
        }

    }
    public override void OnButtonHeld(GameObject user)
    {
        //base.OnButtonUse(user);
        /*
        spawn preview that can change colors based on validity of placement
        */
        if (NullChecks(user)){
            Debug.LogError("TurretItem_MONO: Button hold failed");
            return;
        }
        //Debug.Log("TurretItem_MONO: turret holding");
        if (_userCamera == null){
            _userCamera = user.transform.Find("Camera").gameObject;
        }
        if (Physics.Raycast(_userCamera.transform.position, _userCamera.transform.forward, out RaycastHit hit, _previewDistance, LayerMask.GetMask("whatIsGround"))){
            _currentPlacementPosition = hit.point;
            Quaternion rotation = Quaternion.Euler(0f, _userCamera.transform.eulerAngles.y, 0f);
            _previewGameObject.SetActive(true);
            _previewGameObject.transform.position = _currentPlacementPosition;
            _previewGameObject.transform.rotation = rotation;
            _previewIsActive = true;
            
            //modify color based on if placeable
            _previewIsValid = _previewGameObject.GetComponent<TurretPreviewValidation>().IsValid;
            if (_previewIsValid){
                // make color valid
                _previewMaterial.color = _validColor;
            }else{
                // make color invalid
                _previewMaterial.color = _invalidColor;
            }
        }else{
            RemovePreview();
        }
    }

    public override void OnButtonRelease(GameObject user)
    {
        //base.OnButtonRelease(user);
        /*
        if preview is valid, remove preview and spawn turret
        */
        if (NullChecks(user)){
            Debug.LogError("TurretItem_MONO: Button release failed");
            return;
        }
        if (_previewIsValid){
            // instantiate turret and destroy preview object
            Debug.Log("TurretItem_MONO: turret placed here");
            RemovePreview();
            quantity--;                       // uncomment when turret actually gets placed
            Destroy(_previewGameObject);      // uncomment when implementing item getting used up
            //_turretObjectPrefab.Spawn(true);  // swap to using pool
            SpawnTurretServerRPC();             // change later to be a function that grabs from object pool
        }else{
            Debug.Log("TurretItem_MONO: turret cannot be placed here");
            RemovePreview();
        }
    }

    public override void OnSwapOut(GameObject user)
    {
        //base.OnSwapOut(user);
        /*
        if preview exists, remove it
        */
        if (NullChecks(user)){
            Debug.LogError("TurretItem_MONO: Swap out failed");
            return;
        }
        if (_previewIsActive){
            RemovePreview();
        }
    }
    public override void OnDrop(GameObject user)
    {
        //base.OnDrop(user);
        /*
        if preview exists, remove it
        */
        if (NullChecks(user)){
            Debug.LogError("TurretItem_MONO: Item Drop failed");
            return;
        }
        if (_previewIsActive){
            RemovePreview();
        }
        Destroy(_previewGameObject);
    }

    private void RemovePreview(){
        _previewGameObject.SetActive(false);
        _previewIsActive = false;
        _previewIsValid = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnTurretServerRPC(){
        /* This part tried to grab the turret from the object pool, not as a serverRPC
        NetworkObject networkTurret = _objectPool.GetNetworkObject(
            _turretObjectPrefab,
            _previewGameObject.transform.position,
            Quaternion.identity
        );
        if (networkTurret.IsSpawned) {
            Debug.Log($"Tried to spawn {networkTurret.name} but it is already spawned.");
            return;
        }
        networkTurret.Spawn(true);*/
        GameObject newTurret = Instantiate(_turretObjectPrefab, _previewGameObject.transform.position, Quaternion.identity);
        newTurret.GetComponent<NetworkObject>().Spawn(true);
    }
    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("TurretItem_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("TurretItem_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("TurretItem_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("TurretItem_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}