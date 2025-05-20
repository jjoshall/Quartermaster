using UnityEngine;
using System.Collections.Generic;

public class SlowTrapItem : Item
{
    #region ItemSettings
    #endregion
    [Header("SlowTrap Settings")]
    [SerializeField] private float _SlowTrapBaseVelocity = 5f; // originally 5f
    [SerializeField] private float _SlowTrapMaxVelocity = 10f; // originally 30f
    [SerializeField] private float _SlowTrapMaxChargeTime = 0.5f; // originally 1.0f
    [SerializeField] private float _SlowTrapScale = 5.0f;   // particle aoe is set separately in the inspector/ParticleManager/
    [SerializeField] private float _SlowTrapSlowAmount = 0.5f;

    #region RuntimeVars
    #endregion 
    private float _SlowTrapChargeTime = 0f; // time SlowTrap has been charged for.
    private float _SlowTrapVelocity = 0f; // velocity of SlowTrap.
    private bool _isCharging = false; 

    public override void OnButtonUse(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("SlowTrap_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        _SlowTrapVelocity = _SlowTrapBaseVelocity; // reset velocity.
        _SlowTrapChargeTime = 0f; // reset charge time.
        _isCharging = true; // set charging to true.

        UpdateLineRenderer(user); // update line renderer.
    }

    public override void OnButtonHeld(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("SlowTrap_MONO: ButtonHeld() NullChecks failed.");
            return;
        }

        if (!_isCharging) return;

        _SlowTrapChargeTime += Time.deltaTime; // increment charge time.
        float t = Mathf.Clamp01(_SlowTrapChargeTime / _SlowTrapMaxChargeTime); // calculate interpolation factor.
        _SlowTrapVelocity = Mathf.Lerp(_SlowTrapBaseVelocity, _SlowTrapMaxVelocity, t); // linearly interpolate SlowTrap velocity from base to max over the charge time.

        UpdateLineRenderer(user); // update line renderer.

    }

    public override void OnButtonRelease(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("SlowTrap_MONO: ButtonRelease() NullChecks failed.");
            return;
        }

        if (!_isCharging) return; // check if charging
        // .

        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the SlowTrap.
        ProjectileManager.instance.SpawnSelfThenAll("SlowTrap", 
                camera.transform.position + camera.right * 0.1f, 
                camera.transform.rotation, 
                direction, 
                _SlowTrapVelocity, 
                user, 
                _SlowTrapScale,
                _SlowTrapSlowAmount); // spawn SlowTrap with scale and slow amount.

        quantity--;
        SetLastUsed(Time.time);
        ProjectileManager.instance.DestroyLineRenderer(); // clear arc and deactivate local line renderer
        _SlowTrapVelocity = _SlowTrapBaseVelocity;
        _SlowTrapChargeTime = 0.0f;
        _isCharging = false; // set charging to false.
    }

    public override void OnDrop(GameObject user)
    {
        ProjectileManager.instance.DestroyLineRenderer();
        _SlowTrapVelocity = _SlowTrapBaseVelocity; // reset velocity.
        _SlowTrapChargeTime = 0f; // reset charge time.
        _isCharging = false; // set charging to false.
    }

    public override void OnSwapOut(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("SlowTrap_MONO: SwapCancel() NullChecks failed.");
            return;
        }
        ProjectileManager.instance.DestroyLineRenderer();
        _SlowTrapVelocity = _SlowTrapBaseVelocity; // reset velocity.
        _SlowTrapChargeTime = 0f; // reset charge time.
        _isCharging = false; // set charging to false.

    }


    #region SlowTrapHelpers
    #endregion

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _SlowTrapVelocity);
    }

    #region GeneralHelpers
    #endregion 

    private bool NullChecks(GameObject user){
        if (user == null) {
            Debug.LogError ("SlowTrap_MONO: NullChecks() user is null.");
            return true;
        }
        if (user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError ("SlowTrap_MONO: NullChecks() user has no PlayerStatus component.");
            return true;
        }
        if (user.GetComponent<Inventory>() == null){
            Debug.LogError ("SlowTrap_MONO: NullChecks() user has no Inventory component.");
            return true;
        }
        if (user.GetComponent<Inventory>().orientation == null){
            Debug.LogError ("SlowTrap_MONO: NullChecks() user has no orientation component.");
            return true;
        }
        return false;

    }
}
