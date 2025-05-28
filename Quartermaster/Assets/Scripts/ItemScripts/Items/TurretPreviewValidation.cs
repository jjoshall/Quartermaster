using System.Collections.Generic;
using UnityEngine;

public class TurretPreviewValidation : MonoBehaviour
{
    [SerializeField] LayerMask _invalidLayers;  // layers that make placing turret invalid
    public bool IsValid {get; private set;} = true;
    private List<Collider> _collidingObjects = new List<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _invalidLayers) != 0){
            _collidingObjects.Add(other);
            Debug.Log("TurretPreviewValidation: obstruction noticed with layer: " + other.gameObject.layer);
            IsValid = false;
        }
    }
    private void OnTriggerExit(Collider other){
        if (((1 << other.gameObject.layer) & _invalidLayers) != 0){
            _collidingObjects.Remove(other);
            IsValid = _collidingObjects.Count <= 0;
        }
    }
}