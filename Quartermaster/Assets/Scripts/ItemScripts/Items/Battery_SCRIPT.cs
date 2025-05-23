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
        base.OnButtonUse(user);
        if (!_isCharging.Value)
        {
            SetChargingStatusServerRpc(true);
        }
    }

    public override void OnButtonHeld(GameObject user)
    {
        base.OnButtonHeld(user);
        if (!_isCharging.Value)
        {
            SetChargingStatusServerRpc(true);
        }
    }

    public override void OnButtonRelease(GameObject user)
    {
        base.OnButtonRelease(user);
        // set charging status to false.
        SetChargingStatusServerRpc(false);
    }

    public override void OnSwapOut(GameObject user)
    {
        base.OnSwapOut(user);
        // set charging status to false.
        SetChargingStatusServerRpc(false);
    }

    public override void OnDrop(GameObject user)
    {
        base.OnDrop(user);
        // set charging status to false.
        SetChargingStatusServerRpc(false);
    }

    #region BatteryUpdate
    #endregion
    public void Update()
    {
        if (_isCharging.Value) Charge();
        if (!_hasBeenFilled.Value && _currentCharge > 0) Leak();
        ClampCharge();
        UpdateLitSegmentCount();
        UpdateSegmentDisplay();

        _serverSyncTimer += Time.deltaTime;
        if (_serverSyncTimer >= _serverSyncInterval)
        {
            SyncClientsToServerRpc();
            _serverSyncTimer = 0f;
        }
    }

    public void Charge()
    {
        _currentCharge = _currentCharge + Time.deltaTime * _chargeRate;
    }

    public void Leak()
    {
        _currentCharge = _currentCharge - Time.deltaTime * _chargingLeakRate;
    }

    public void ClampCharge()
    {
        _currentCharge = Mathf.Clamp(_currentCharge, 0, _maxChargeCapacity);
    }


    #region SegmentDsplyHelpers
    #endregion
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


    #region ServerSync
    #endregion

    // only call this periodically (charge variables managed locally to avoid network bloat)
    [ServerRpc(RequireOwnership = false)]
    public void SyncClientsToServerRpc()
    {
        SyncClientsToServerClientRpc(_currentCharge);
        Debug.Log("Syncing clients to server. Current charge(server): " + _currentCharge);
    }

    [ClientRpc]
    public void SyncClientsToServerClientRpc(float charge)
    {
        _currentCharge = charge;
        Debug.Log("Synced client to server. Current charge(client): " + _currentCharge);
    }


    #region NetworkVarWriteHelpers
    // should only call these two functions inside if (!isServer) return; guard. write to network var ONCE from server only.
    [ServerRpc(RequireOwnership = false)]
    public void SetChargingStatusServerRpc(bool isCharging)
    {
        _isCharging.Value = isCharging;
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetFillStatusServerRpc(bool hasBeenFilled)
    {
        _hasBeenFilled.Value = hasBeenFilled;
    }
    #endregion

}
