using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Turret_SCRIPT : NetworkBehaviour
{
    // needs a collider.
    // needs segmented child objects for rotation.

    [SerializeField, Tooltip("Parent")] 
    private GameObject _turretHorizontalRotator;
    [SerializeField, Tooltip("Child of horizontal")] 
    private GameObject _turretVerticalRotator;
    [SerializeField, Tooltip("Used for raycast")] 
    private Transform _barrelBase;
    [SerializeField, Tooltip("Used for spawning projectile")] 
    private Transform _barrelTip;
    [SerializeField, Tooltip("Used for tracking targets")]
    private GameObject _targetTriggerVolume;

    [SerializeField]
    private float _attackRange = 10f;
    [SerializeField]
    private float _maxHorizontalRotationSpeed = 2f; // degrees per second.
    [SerializeField]
    private float _maxVerticalRotationSpeed = 2f; // degrees per second.
    [SerializeField]
    private float _lookDifferenceThreshold = 5f; // degrees, how close the turret needs to be to the target direction before firing.


    public float lastAttackTime = float.MinValue;
    private GameObject weaponItem;
    private List<GameObject> _items = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (_turretHorizontalRotator == null) 
            Debug.LogError("Turret Horizontal Rotator is not assigned in Turret_SCRIPT.");
        if (_turretVerticalRotator == null)
            Debug.LogError("Turret Vertical Rotator is not assigned in Turret_SCRIPT.");
        if (_barrelBase == null)
            Debug.LogError("Barrel Base is not assigned in Turret_SCRIPT.");
        if (_barrelTip == null)
            Debug.LogError("Barrel Tip is not assigned in Turret_SCRIPT.");
        if (_targetTriggerVolume == null)  
            Debug.LogError("Target Trigger Volume is not assigned in Turret_SCRIPT.");
        _targetTriggerVolume.GetComponent<TargetTriggerVolume_SCRIPT>().targetAcquisitionRange = _attackRange; // set default range.
        _targetTriggerVolume.GetComponent<SphereCollider>().radius = _attackRange;
        _items.Clear(); // clear items list.
    }

    private void AttackTarget(){
        // trigger item turret logic.
        
    }

    private bool LockedOnTarget(GameObject target){
        // if look direction within 
        if (target == null) return false; // no target to lock on to.
        RotateHorizontal(target);
        RotateVertical(target);
        
        if (Vector3.Angle(_barrelTip.forward, target.transform.position - _barrelTip.position) < _lookDifferenceThreshold)
        {
            // if the target is within the look difference threshold, we are locked on.
            return true;
        }
        return false;
    }

    private void RotateHorizontal(GameObject target){
        Vector3 direction = target.transform.position - _barrelBase.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        // determine whether to rotate left or right
        float step = _maxHorizontalRotationSpeed * Time.deltaTime;
        if (Quaternion.Angle(_turretHorizontalRotator.transform.rotation, lookRotation) < step)
        {
            _turretHorizontalRotator.transform.rotation = lookRotation; // snap to target rotation.
            return; // no need to rotate further.
        } else {
            Quaternion targetRotation = Quaternion.RotateTowards(_turretHorizontalRotator.transform.rotation, lookRotation, step);
        }
    }

    private void RotateVertical(GameObject target){
        Vector3 direction = target.transform.position - _barrelBase.position;
        direction.y = 0; // ignore vertical component for horizontal rotation.
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        // determine whether to rotate up or down
        float step = _maxVerticalRotationSpeed * Time.deltaTime;
        if (Quaternion.Angle(_turretVerticalRotator.transform.rotation, lookRotation) < step)
        {
            _turretVerticalRotator.transform.rotation = lookRotation; // snap to target rotation.
            return; // no need to rotate further.
        } else {
            Quaternion targetRotation = Quaternion.RotateTowards(_turretVerticalRotator.transform.rotation, lookRotation, step);
        }
    }

    private bool HasWeapon(){
        if (weaponItem != null) return true; // has a weapon item.
        return false;
    }

    void Update()
    {
        GameObject target = _targetTriggerVolume.GetComponent<TargetTriggerVolume_SCRIPT>().GetTurretTarget();
        if (LockedOnTarget(target) && HasWeapon()){
            AttackTarget();
        }
        
    }
}