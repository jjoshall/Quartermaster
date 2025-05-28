using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class TCK_SCRIPT : Item
{
    // raycasting state while item is held.
    private bool _isRaycasting = false;

    [SerializeField] private GameObject _turretIndicatorObj; // child obj of this item.

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

    }

    void Update()
    {
        if (_isRaycasting)
        {
            // Raycast from the camera to the target position.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Draw(hitInfo.point);
            }
        }
        else
        {
            // Hide the indicator when not raycasting.
            if (_turretIndicatorObj != null)
            {
                _turretIndicatorObj.SetActive(false);
            }
        }
    }

    private Vector3 GetRaycastedPointFromCamera(){
        Vector3 target = Vector3.zero;
        return target;
    }

    private void Draw(Vector3 targetPosition){
        DrawLineToTarget(targetPosition);
        if (_turretIndicatorObj != null)
        {
            _turretIndicatorObj.transform.position = targetPosition;
            _turretIndicatorObj.SetActive(_isRaycasting);
        }
    }

    private void DrawLineToTarget(Vector3 targetPosition)
    {
        if (_isRaycasting)
        {
            Debug.DrawLine(transform.position, targetPosition, Color.red);
        }
    }
    private void DrawCircle(){

    }

    private void DrawIndicatorPrefab(){

    }
}