using UnityEngine;

public class BatteryChargerDetector_SCRIPT : MonoBehaviour
{
    [SerializeField] private GameObject batteryObj;
    private Battery _battery;
    public bool _inRangeOfCharger = false;

    void OnTriggerEnter(Collider other)
    {
        if (_battery == null)
        {
            _battery = batteryObj.GetComponent<Battery>();
            if (_battery == null)
            {
                Debug.LogError("Battery component not found on batteryObj.");
                return;
            }
        }
        if (other.CompareTag("Charger"))
        {
            _inRangeOfCharger = true;
            Debug.Log("BatteryCharger In range of charger.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_battery == null)
        {
            _battery = batteryObj.GetComponent<Battery>();
            if (_battery == null)
            {
                Debug.LogError("Battery component not found on batteryObj.");
                return;
            }
        }
        if (other.CompareTag("Charger"))
        {
            _inRangeOfCharger = false;
            _battery.SetChargingStatusServerRpc(false);
            Debug.Log("BatteryCharger Out of range of charger.");
        }
    }
}
