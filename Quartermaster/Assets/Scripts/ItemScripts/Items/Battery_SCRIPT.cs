using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class Battery : Item
{
    [Header("InspectorSetup")]
    public List<GameObject> batterySegments = new List<GameObject>();
    public bool invertSegmentFillDirection = false;
    public Material emptyBatteryMaterial;
    public Material fullBatteryMaterial;

    #region Settings
    #endregion
    [SerializeField] private float _chargeRate = 3f;
    [SerializeField] private float _maxChargeCapacity = 90f;
    [SerializeField] private float _chargingLeakRate = 0.5f;
    [SerializeField] private float _idleLeakRate = 0.1f;
    [SerializeField] private float _serverSyncInterval = 10.0f; // syncs charge from server every interval.

    #region RuntimeLocal
    #endregion // update()
    private float _currentCharge = 0f;              // high frequency updates. update locally. periodic / event-based syncing from server.
    private int _litSegments = 0;                   // _currentCharge / _maxChargeCapacity * batterySegments.Count; rounded.
    private float _serverSyncTimer = 0f;
    // ischarging should allow server write

    #region RuntimeNetwork
    #endregion // updates periodically
    private NetworkVariable<bool> _isCharging = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> _hasBeenFilled = new NetworkVariable<bool>(false); // true if battery has been filled at least once.


    public override void OnButtonUse(GameObject user)
    {
        // initialize charge.
    }

    public override void OnButtonHeld(GameObject user)
    {
        base.OnButtonHeld(user);
    }

    #region BatteryUpdate
    #endregion
    public void Update()
    {
        if (_isCharging.Value) Charge();
        if (!_hasBeenFilled.Value && _currentCharge > 0) Leak();
        UpdateLitSegmentCount();
        UpdateSegmentDisplay();
    }

    public void Charge()
    {
        if (_currentCharge < _maxChargeCapacity)
        {
            _currentCharge = Mathf.Clamp(_currentCharge + Time.deltaTime * _chargeRate, 0, _maxChargeCapacity);
        }
        else
        {
            // battery is full. do nothing.
        }
    }

    public void Leak()
    {
        
    }

    public void UpdateLitSegmentCount()
    {
        // update lit segments
        if (_currentCharge > 0)
        {
            _litSegments = Mathf.RoundToInt(_currentCharge / _maxChargeCapacity * batterySegments.Count);
        }
        else
        {
            _litSegments = 0;
        }
    }

    public void UpdateSegmentDisplay()
    {
        // update battery segments
        for (int i = 0; i < batterySegments.Count; i++)
        {
            if (i < _litSegments)
            {
                batterySegments[i].GetComponent<MeshRenderer>().material = fullBatteryMaterial;
            }
            else
            {
                batterySegments[i].GetComponent<MeshRenderer>().material = emptyBatteryMaterial;
            }
        }
    }


    #region NetworkVarWriteHelpers
    #endregion

    // only call this periodically (charge variables managed locally to avoid network bloat)
    public void SyncChargeToServer()
    {

    }

    [ServerRpc(RequireOwnership = false)]
    public void SetChargingStatusServerRpc(bool isCharging)
    {
        _isCharging.Value = isCharging;
    }

}
