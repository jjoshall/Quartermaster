using UnityEngine;
using System.Collections.Generic;

public class Grenade_MONO : Item
{
    #region ItemSettings
    #endregion
    [Header("Grenade Settings")]
    [SerializeField] private float _grenadeBaseVelocity = 5f; // originally 5f
    [SerializeField] private float _grenadeMaxVelocity = 30f; // originally 30f
    [SerializeField] private float _grenadeMaxChargeTime = 1.0f; // originally 1.0f

    [SerializeField] private float _grenadeDamage = 6.0f; // originally 6.0f
    [SerializeField] private float _capsuleRadius = 4.0f; // 4.0f

    #region RuntimeVars
    #endregion 
    private float _grenadeChargeTime = 0f; // time grenade has been charged for.
    private float _grenadeVelocity = 0f; // velocity of grenade.
    private bool _isCharging = false; 

    public override void ButtonUse(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("Grenade_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        _grenadeVelocity = _grenadeBaseVelocity; // reset velocity.
        _grenadeChargeTime = 0f; // reset charge time.
        _isCharging = true; // set charging to true.

        UpdateLineRenderer(user); // update line renderer.
    }

    public override void ButtonHeld(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("Grenade_MONO: ButtonHeld() NullChecks failed.");
            return;
        }

        if (!_isCharging) return;

        _grenadeChargeTime += Time.deltaTime; // increment charge time.
        float t = Mathf.Clamp01(_grenadeChargeTime / _grenadeMaxChargeTime); // calculate interpolation factor.
        _grenadeVelocity = Mathf.Lerp(_grenadeBaseVelocity, _grenadeMaxVelocity, t); // linearly interpolate grenade velocity from base to max over the charge time.

        UpdateLineRenderer(user); // update line renderer.

    }

    public override void ButtonRelease(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("Grenade_MONO: ButtonRelease() NullChecks failed.");
            return;
        }

        if (!_isCharging) return; // check if charging
        // .

        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the grenade.
        ProjectileManager.instance.SpawnSelfThenAll("Grenade", 
                camera.transform.position + camera.right * 0.1f, 
                camera.transform.rotation, 
                direction, 
                _grenadeVelocity, 
                user,
                _grenadeDamage, 
                _capsuleRadius);

        quantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer(); // clear arc and deactivate local line renderer
        _grenadeVelocity = _grenadeBaseVelocity;
        _grenadeChargeTime = 0.0f;
        _isCharging = false; // set charging to false.
    }

    public override void Drop(GameObject user)
    {
        ProjectileManager.instance.DestroyLineRenderer();
        _grenadeVelocity = _grenadeBaseVelocity; // reset velocity.
        _grenadeChargeTime = 0f; // reset charge time.
        _isCharging = false; // set charging to false.
    }

    public override void SwapCancel(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("Grenade_MONO: SwapCancel() NullChecks failed.");
            return;
        }
        ProjectileManager.instance.DestroyLineRenderer();
        _grenadeVelocity = _grenadeBaseVelocity; // reset velocity.
        _grenadeChargeTime = 0f; // reset charge time.
        _isCharging = false; // set charging to false.

    }


    #region GrenadeHelpers
    #endregion

    // Does the actual damage to param target.
    private void DoDamage (Damageable d, bool isExplosiveDmgType, GameObject user){
        float damage = _grenadeDamage;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonus = s.GetDmgBonus();
            damage = damage * (1 + bonus);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _grenadeVelocity);
    }

    #region GeneralHelpers
    #endregion 

    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("Grenade_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("Grenade_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("Grenade_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("Grenade_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}
