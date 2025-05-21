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

    #region Battery Item Settings
    #endregion
    [Header("Adjustable per battery type")]
    public float maxChargeTime = 30f;
    public float maxChargeCapacity = 90f;
    public float chargeLeakRate = 0.5f;
    public float serverSyncInterval = 10.0f; // syncs charge from server every interval.

    #region RuntimeVars
    #endregion
    private float _currentCharge = 0f;              // high frequency updates. update locally. periodic / event-based syncing from server.
    private float _currentChargeTime = 0f;          // high frequency updates. update locally. periodic / event-based syncing from server.
    private float _serverSyncTimer = 0f;
    private NetworkVariable<int> _litSegments = new NetworkVariable<int>(0); // periodic / event-based updates.



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
        // update _currentCharge
    }

    public void SyncChargeToServer()
    {
        
    }

    [ServerRpc(RequireOwnership = false)]
    

    public void UpdateSegmentDisplay()
    {
        // update battery segments
        for (int i = 0; i < batterySegments.Count; i++)
        {
            if (i < _litSegments.Value)
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

}
