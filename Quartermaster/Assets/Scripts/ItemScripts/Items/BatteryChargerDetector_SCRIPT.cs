using Unity.VisualScripting;
using UnityEngine;

public class BatteryChargerDetector_SCRIPT : MonoBehaviour
{
    [SerializeField] private GameObject batteryObj;
    private Battery _battery;
    public bool _inRangeOfCharger = false;
    public bool _inRangeOfTurret = false;
    public GameObject _turretInRange;   // set up to be list later, otherwise battery targets furthest turret in range

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
        } else if (other.CompareTag("Turret"))
        {
            _inRangeOfTurret = true;
            _turretInRange = other.gameObject;
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
        } else if (other.CompareTag("Turret"))
        {
            _inRangeOfTurret = false;
            _turretInRange = null;
        }
    }
}
