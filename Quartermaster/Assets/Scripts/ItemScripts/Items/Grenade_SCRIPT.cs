using UnityEngine;
using System.Collections.Generic;

public class Grenade_MONO : Item
{
    #region ItemSettings
    #endregion
    [Header("Grenade Settings")]
    // [SerializeField] private float _grenadeBaseVelocity = 5f; // originally 5f
    [SerializeField] private float _grenadeMaxVelocity = 30f; // originally 30f
    // [SerializeField] private float _grenadeMaxChargeTime = 1.0f; // originally 1.0f
    [SerializeField] private float _grenadeExpireTimer = 0.5f; // expire timer after hitting a non enemy.

    [SerializeField] private float _grenadeDamage = 40.0f; // originally 6.0f
    [SerializeField] private float _capsuleRadius = 4.0f; // 4.0f
    [SerializeField] private bool throwCameraOrRaycast = true; // if true, throw grenade in direction of camera. if false, throw grenade in direction of raycast hit point.
    [SerializeField] private LayerMask _throwRaycastables = 0;

    #region RuntimeVars
    #endregion
    // private float _grenadeChargeTime = 0f; // time grenade has been charged for.
    // private float _grenadeVelocity = 0f; // velocity of grenade.
    // private bool _isCharging = false; 

    public override void OnButtonUse(GameObject user)
    {
        if (NullChecks(user))
        {
            Debug.LogError("Grenade_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction;
        Vector3 throwOrigin;
        GetThrowData(camera, user.transform, user.GetComponent<Inventory>().weaponSlot.transform, out throwOrigin, out direction);

        var totalDmg = _grenadeDamage * (1 + s.GetDmgBonus());

        // Throw the grenade.
        ProjectileManager.instance.SpawnSelfThenAll("Grenade",
                throwOrigin,
                camera.transform.rotation,
                direction,
                _grenadeMaxVelocity,
                _grenadeExpireTimer,
                user,
                totalDmg,
                _capsuleRadius);

        quantity--;
        SetLastUsed(Time.time);
        // _grenadeVelocity = _grenadeBaseVelocity; // reset velocity.
        // _grenadeChargeTime = 0f; // reset charge time.
        // _isCharging = true; // set charging to true.

        // UpdateLineRenderer(user); // update line renderer.
    }

    #region GrenadeHelpers
    #endregion
    private void GetThrowData(Transform camera, Transform user, Transform weaponSlot, out Vector3 throwOriginPosition, out Vector3 throwDirection)
    {
        if (throwCameraOrRaycast)
        {
            throwOriginPosition = camera.position;
            throwDirection = camera.forward;
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(camera.position, camera.forward, out hit, 100f, _throwRaycastables))
            {
                // if hit is not an enemy, throw grenade in direction of raycast hit point.
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy"))
                {
                    throwOriginPosition = hit.point;
                    throwDirection = (hit.point - weaponSlot.position).normalized;
                }
                else
                {
                    throwOriginPosition = weaponSlot.position;
                    throwDirection = camera.forward;
                }
            }
            else
            {
                throwOriginPosition = weaponSlot.position;
                var throwDestination = camera.position + camera.forward * 100f;
                throwDirection = (throwDestination - weaponSlot.position).normalized;
            }
        }
    }

    #region GeneralHelpers
    #endregion

    private bool NullChecks(GameObject user)
    {
        if (user == null)
        {
            Debug.LogError("Grenade_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null)
        {
            Debug.LogError("Grenade_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null)
        {
            Debug.LogError("Grenade_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null)
        {
            Debug.LogError("Grenade_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}
