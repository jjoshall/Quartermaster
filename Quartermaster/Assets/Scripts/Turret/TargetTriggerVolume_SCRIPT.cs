using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class TargetTriggerVolume_SCRIPT : MonoBehaviour
{
    private List<GameObject> _targets = new List<GameObject>();
    [HideInInspector] public float targetAcquisitionRange = 10f;

    public void FlushTargets()
    {
        _targets.Clear();
        Debug.Log("All targets flushed.");
    }
    public void ReacquireTargets()
    {
        _targets.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetAcquisitionRange); // Adjust radius as needed
        foreach (var collider in colliders)
        {
            if (collider.gameObject.CompareTag("Enemy"))
            {
                _targets.Add(collider.gameObject);
                Debug.Log($"Reacquired target: {collider.gameObject.name}");
            }
        }
    }

    public GameObject GetTurretTarget(){
        GameObject target = null;
        return target;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            _targets.Add(other.gameObject);
            // Optionally, you can notify the turret or any other component about the new target.
            Debug.Log($"Target entered: {other.gameObject.name}");
        }
    }

}