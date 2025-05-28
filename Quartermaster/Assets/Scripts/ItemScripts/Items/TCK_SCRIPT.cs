using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class TCK_SCRIPT : Item
{
    // raycasting state while item is held.
    private bool _isRaycasting = false;

    [SerializeField] private GameObject _turretIndicatorObj; // child obj of this item.
    [SerializeField] private GameObject _turretPrefab; 
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (_turretIndicatorObj == null) Debug.LogError("Turret Indicator Object is not assigned in TCK_SCRIPT.");
        else _turretIndicatorObj.SetActive(false); // ensure it's inactive at start.
    }

    public override void OnButtonUse(GameObject user)
    {
        base.OnButtonUse(user);
        EnableDrawIndicator();
    }

    public override void OnButtonHeld(GameObject user)
    {
        base.OnButtonHeld(user);
        EnableDrawIndicator();
    }

    public override void OnButtonRelease(GameObject user)
    {
        base.OnButtonRelease(user);
        DisableDrawIndicator();
        SpawnTurret();
        // toggle raycasting state.
    }

    public override void OnSwapOut(GameObject user)
    {
        base.OnSwapOut(user);
        DisableDrawIndicator();
    }

    public override void OnDrop(GameObject user)
    {
        base.OnDrop(user);
        DisableDrawIndicator();
    }

    private void EnableDrawIndicator(){
        _isRaycasting = false;
        _turretIndicatorObj.SetActive(false);
    }

    private void DisableDrawIndicator()
    {
        _isRaycasting = false;
        _turretIndicatorObj.SetActive(false);
    }   

    private void SpawnTurret(){
        // Logic to spawn the turret at the raycasted position.
        Vector3 targetPosition = GetRaycastedPointFromCamera();
        if (targetPosition != Vector3.zero)
        {
            SpawnTurretServerRpc(targetPosition);
            Debug.Log("Spawning turret at: " + targetPosition);
        }
        else
        {
            Debug.LogWarning("Target position for turret spawn is invalid.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnTurretServerRpc(Vector3 targetPosition){
        GameObject turret = Instantiate(_turretPrefab, targetPosition, Quaternion.identity);
        turret.GetComponent<NetworkObject>().Spawn();
        Debug.Log("Spawning turret at: " + targetPosition + " on server.");
    }

    void Update()
    {
        if (_isRaycasting)
        {
            Draw();
        }
        else
        {
            _turretIndicatorObj.SetActive(false);
        }
    }


    private void Draw(){
        Vector3 targetPosition = GetRaycastedPointFromCamera();
        DrawLineToTarget(targetPosition);
        DrawCircle();
        UpdateIndicatorObj(targetPosition);
    }
    
    private Vector3 GetRaycastedPointFromCamera(){
        Vector3 target = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            target = hit.point;
        }
        else
        {
            target = Camera.main.transform.position + Camera.main.transform.forward * 10f; // 10 units in front of the camera.
        }
        return target;
    }

    private void DrawLineToTarget(Vector3 targetPosition)
    {
        Vector3 origin = transform.position;
        Debug.DrawLine(transform.position, targetPosition, Color.red);
    }
    private void DrawCircle(){

    }

    private void UpdateIndicatorObj(Vector3 targetPosition)
    {
        _turretIndicatorObj.transform.position = targetPosition;
        _turretIndicatorObj.SetActive(_isRaycasting);
    }
}