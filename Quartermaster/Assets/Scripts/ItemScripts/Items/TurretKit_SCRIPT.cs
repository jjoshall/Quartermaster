using UnityEngine;
using System.Collections.Generic;

public class TurretKit_MONO : Item
{
    #region ItemSettings
    #endregion
    [Header("TurretKit Settings")]
    [SerializeField] private float _TurretKitBaseVelocity = 5f; // originally 5f
    [SerializeField] private float _TurretKitMaxVelocity = 30f; // originally 30f
    [SerializeField] private float _TurretKitMaxChargeTime = 1.0f; // originally 1.0f

    [SerializeField] private float _TurretKitDamage = 6.0f; // originally 6.0f
    [SerializeField] private float _capsuleRadius = 4.0f; // 4.0f

    #region RuntimeVars
    #endregion 
    private float _TurretKitChargeTime = 0f; // time TurretKit has been charged for.
    private float _TurretKitVelocity = 0f; // velocity of TurretKit.
    private bool _isCharging = false; 

    public override void OnButtonUse(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("TurretKit_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        _TurretKitVelocity = _TurretKitBaseVelocity; // reset velocity.
        _TurretKitChargeTime = 0f; // reset charge time.
        _isCharging = true; // set charging to true.

        UpdateLineRenderer(user); // update line renderer.
    }

    public override void OnButtonHeld(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("TurretKit_MONO: ButtonHeld() NullChecks failed.");
            return;
        }

        if (!_isCharging) return;

        _TurretKitChargeTime += Time.deltaTime; // increment charge time.
        float t = Mathf.Clamp01(_TurretKitChargeTime / _TurretKitMaxChargeTime); // calculate interpolation factor.
        _TurretKitVelocity = Mathf.Lerp(_TurretKitBaseVelocity, _TurretKitMaxVelocity, t); // linearly interpolate TurretKit velocity from base to max over the charge time.

        UpdateLineRenderer(user); // update line renderer.

    }

    public override void OnButtonRelease(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("TurretKit_MONO: ButtonRelease() NullChecks failed.");
            return;
        }

        if (!_isCharging) return; // check if charging
        // .

        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the TurretKit.
        ProjectileManager.instance.SpawnSelfThenAll("TurretKit", 
                camera.transform.position + camera.right * 0.1f, 
                camera.transform.rotation, 
                direction, 
                _TurretKitVelocity, 
                user,
                _TurretKitDamage);

        quantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer(); // clear arc and deactivate local line renderer
        _TurretKitVelocity = _TurretKitBaseVelocity;
        _TurretKitChargeTime = 0.0f;
        _isCharging = false; // set charging to false.
    }

    public override void OnDrop(GameObject user)
    {
        ProjectileManager.instance.DestroyLineRenderer();
        _TurretKitVelocity = _TurretKitBaseVelocity; // reset velocity.
        _TurretKitChargeTime = 0f; // reset charge time.
        _isCharging = false; // set charging to false.
    }

    public override void OnSwapOut(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("TurretKit_MONO: SwapCancel() NullChecks failed.");
            return;
        }
        ProjectileManager.instance.DestroyLineRenderer();
        _TurretKitVelocity = _TurretKitBaseVelocity; // reset velocity.
        _TurretKitChargeTime = 0f; // reset charge time.
        _isCharging = false; // set charging to false.
    }


    #region TurretKitHelpers
    #endregion

    // Does the actual damage to param target.
    private void DoDamage (Damageable d, bool isExplosiveDmgType, GameObject user){
        float damage = _TurretKitDamage;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonus = s.GetDmgBonus();
            damage = damage * (1 + bonus);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _TurretKitVelocity);
    }

    #region GeneralHelpers
    #endregion 

    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("TurretKit_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("TurretKit_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("TurretKit_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("TurretKit_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}
