using UnityEngine;
using System.Collections.Generic;

public class MedKit_MONO : MonoItem
{
    #region ItemSettings
    #endregion
    [Header("MedKit Settings")]
    [SerializeField] private float _medKitBaseVelocity = 1f; // originally 5f
    [SerializeField] private float _medKitMaxVelocity = 10f; // originally 30f
    [SerializeField] private float _medKitMaxChargeTime = 1.0f; // originally 1.0f
    [SerializeField] private float _medKitTapThreshold = 0.1f;
    [SerializeField] private float _healAmount = 6.0f; // originally 6.0f

    #region RuntimeVars
    #endregion 
    private float _medKitChargeTime = 0f; // time MedKit has been charged for.
    private float _medKitTapTime = 0f;
    private float _medKitVelocity = 0f; // velocity of MedKit.
    private bool _medKitTapped;

    public override void ButtonUse(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("MedKit_MONO: ButtonUse() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s.GetLastUsed(uniqueID) + cooldown > Time.time) return; // check cooldown.

        int healSpecLvl = s.GetHealSpecLvl();

        if (healSpecLvl == 0){
            ImmediateMedKitUsage(user);
        } else {
            Debug.Log ("MedKit_MONO: ButtonUse() HealSpecLvl > 0, charging throwable MedKit");

            // Start charging.
            _medKitVelocity = _medKitBaseVelocity; // reset velocity.
            _medKitChargeTime = 0f; // reset charge time.
            _medKitTapTime = Time.time;
            _medKitTapped = true;
            // _isCharging = true; // set charging to true.
        }
    }

    public override void ButtonHeld(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("MedKit_MONO: ButtonHeld() NullChecks failed.");
            return;
        }
        if (!_medKitTapped){
            _medKitVelocity = _medKitBaseVelocity;
            _medKitChargeTime = 0.0f;
            _medKitTapTime = Time.time;
            _medKitTapped = true;
            Debug.Log ("TapTime Set to " + _medKitTapTime);
            return;
        }
        if (Time.time < _medKitTapTime + _medKitTapThreshold) {
            return;
        }
        // Increment the charge time
        _medKitChargeTime += Time.deltaTime;
        float t = Mathf.Clamp01(_medKitChargeTime / _medKitMaxChargeTime);
        _medKitVelocity = Mathf.Lerp(_medKitBaseVelocity, _medKitMaxVelocity, t);

        // Update the line renderer
        UpdateLineRenderer(user);
    }

    public override void ButtonRelease(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("MedKit_MONO: ButtonRelease() NullChecks failed.");
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();

        int healSpec = s.GetHealSpecLvl();
        if (healSpec == 0){
            return;
        }

        if (Time.time < _medKitTapTime + _medKitTapThreshold){ 
            ImmediateMedKitUsage(user);
            return;
        }

        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the MedKit.
        ProjectileManager.instance.SpawnSelfThenAll("MedKit", 
                camera.transform.position + camera.right * 0.1f, 
                camera.transform.rotation, 
                direction, 
                _medKitVelocity, 
                user,
                _healAmount);

        quantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer();
        _medKitVelocity = _medKitBaseVelocity;
        _medKitChargeTime = 0.0f;
        _medKitTapped = false;
    }

    public override void SwapCancel(GameObject user){
        if (NullChecks(user)) {
            Debug.LogError("MedKit_MONO: SwapCancel() NullChecks failed.");
            return;
        }

    }


    #region MedKitHelpers
    #endregion

    
    private void ImmediateMedKitUsage (GameObject user){
        quantity--;

        lastUsed = Time.time;
        // user.GetComponent<PlayerHealth>().Heal(HEAL_AMOUNT);
        // What handles health now?
        // Generate a quaternion for the particle effect to have no rotation
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

        float healSpecBonus = 1.0f; // Get bonus from PlayerStatus.

        float bonus = 1.0f + healSpecBonus;
        float totalHeal = _healAmount * bonus;

        hp.HealServerRpc(totalHeal);
        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));

        _medKitTapTime = 0.0f; // Reset the tap timer
        _medKitTapped = false;
    }

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _medKitVelocity);
    }

    #region GeneralHelpers
    #endregion 

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
