using UnityEngine;
using System.Collections.Generic;

public class MedKit_MONO : Item
{
    #region ItemSettings
    #endregion
    [Header("MedKit Settings")]
    [SerializeField] private float _medKitMaxVelocity = 10f; // originally 30f
    [SerializeField] private float _medKitExpireTimer = 2.0f; // expire timer after hitting non player.
    [SerializeField] private float _healAmount = 6.0f; // originally 6.0f
    [SerializeField] private bool throwCameraOrRaycast = true; // if true, throw grenade in direction of camera. if false, throw grenade in direction of raycast hit point.
    [SerializeField] private LayerMask _throwRaycastables = 0;

    #region RuntimeVars
    #endregion 

    public override void OnButtonUse(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("MedKit_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        ImmediateMedKitUsage(user);
    }


    public override void OnAltUse(GameObject user)
    {
        if (NullChecks(user)) {
            Debug.LogError("MedKit_MONO: AltUse() NullChecks failed.");
            return;
        }
        var ps = user.GetComponent<PlayerStatus>();
        
        if (ps.GetHealSpecLvl() == 0){
            return;
        }

        var totalHeal = _healAmount * (1 + ps.GetHealBonus());

        Transform camera = user.GetComponent<Inventory>().orientation;

        Vector3 direction;
        Vector3 throwOrigin;
        GetThrowData(camera, user.transform, user.GetComponent<Inventory>().weaponSlot.transform, out throwOrigin, out direction);

        // Throw the MedKit.
        ProjectileManager.instance.SpawnSelfThenAll("MedKit", 
                throwOrigin, 
                camera.transform.rotation, 
                direction, 
                _medKitMaxVelocity, 
                _medKitExpireTimer, 
                user,
                totalHeal);

        quantity--;
        SetLastUsed(Time.time);
    }




    #region MedKitHelpers
    #endregion

    
    private void ImmediateMedKitUsage (GameObject user){
        Debug.Log ("MedKit_MONO: ImmediateMedKitUsage() called. Quantity before: " + quantity.ToString());
        quantity--;
        Debug.Log ("MedKit_MONO: ImmediateMedKitUsage() called. Quantity after: " + quantity.ToString());

        SetLastUsed(Time.time);
        Health hp = user.GetComponent<Health>();
        if (hp == null) {
            Debug.LogError("MedKit: ItemEffect: No Health component found on user.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s == null) {
            Debug.LogError("MedKit: ItemEffect: No PlayerStatus component found on user.");
            return;
        }

        float totalHeal = _healAmount * (1 + s.GetHealBonus());

        hp.HealServerRpc(totalHeal);
        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));

    }

    #region GeneralHelpers
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
                throwOriginPosition = weaponSlot.position;
                throwDirection = (hit.point - weaponSlot.position).normalized;
            }
            else
            {
                throwOriginPosition = weaponSlot.position;
                var throwDestination = camera.position + camera.forward * 100f;
                throwDirection = (throwDestination - weaponSlot.position).normalized;
            }
        }
    }
    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("MedKit_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("MedKit_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("MedKit_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("MedKit_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}
