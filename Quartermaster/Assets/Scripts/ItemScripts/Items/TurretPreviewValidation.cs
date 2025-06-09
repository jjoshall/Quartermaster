using System.Collections.Generic;
using UnityEngine;

public class TurretPreviewValidation : MonoBehaviour
{
    [SerializeField] LayerMask _invalidLayers;  // layers that make placing turret invalid
    public bool IsValid {get; private set;} = true;
    private List<Collider> _collidingObjects = new List<Collider>();

    public void ResetValidation()
    {
        _collidingObjects.Clear();
    }

    public void SphereCastInitCheck()
    {
        IsValid = true;
        _collidingObjects.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f, _invalidLayers);
        foreach (Collider collider in colliders)
        {
            _collidingObjects.Add(collider);
            Debug.Log("TurretPreviewValidation: obstruction noticed with layer: " + collider.gameObject.layer);
            IsValid = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _invalidLayers) != 0)
        {
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