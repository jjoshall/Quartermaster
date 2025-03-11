using UnityEngine;
using System.Collections.Generic;

public class Flamethrower : IWeapon
{
    #region DesignSettings
    // Change these to adjust weapon stats.
    
    private static float _itemCooldown = 0f; // originally 0.2f
    private static float _flamethrowerDamage = 6.0f; // originally 6.0f
    private static float _capsuleRadius = 4.0f; // 4.0f
    private static float _maxRange = 10.0f; // 10.0f
    public override bool isHoldable { get; set; } = true;

    // Make an effect string = "" to disable spawning an effect.
    private static string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    private static string _barrelLaserEffect = "PistolBarrelFire"; // effect at player

    #endregion
    #region Variables
    // Backing fields. Don't touch these.
    // private int _id;
    
    private int _quantity = 1;
    // private int _ammo = 0;
    // private float lastUsedTime = float.MinValue;
    // private float lastFiredTime = float.MinValue;

    private bool _isFireStarted = false;

    private ParticleSystem _flamethrowerPS;

    #endregion
    #region Basic Overrides
    // Abstract overrides

    public override float cooldown
    {
        get => _itemCooldown;
        set => _itemCooldown = value;
    }
    public override int itemID {
        get => _id;
        set => _id = value;
    }

    public override int quantity {
        get => _quantity;
        set => _quantity = value;
    }

    public override void InitializeFromGameManager(){
        _itemCooldown = GameManager.instance.Flame_Cooldown; // originally 0.2f
        _flamethrowerDamage = GameManager.instance.Flame_Damage; // originally 6.0f
        _capsuleRadius = GameManager.instance.Flame_EndRadius; // 4.0f
        _maxRange = GameManager.instance.Flame_Range; // 10.0f
        _enemyHitEffect = GameManager.instance.Flame_EnemyHitEffect; // effect spawned on center of every enemy hit.
        _barrelLaserEffect = GameManager.instance.Flame_BarrelEffect; // effect at player
    }

    public override bool CanAutoFire(){
        return true;
    }
    public override void Use(GameObject user, bool isHeld) {
        if (lastUsed + cooldown > Time.time) {
            return;
        }

        if (!_isFireStarted){
            StartFire(user);
            _isFireStarted = true;
        } else {
            fire(user);
        }
        lastUsed = Time.time;
    }

    public override void Release(GameObject user){
        if (_isFireStarted){
            _isFireStarted = false;
            StopFire(user);
        }
        lastUsed = float.MinValue;
    }


    #endregion

    private void StartFire(GameObject user) {
        if (_flamethrowerPS == null) {
            GameObject p_weaponSlot = user.transform.Find("WeaponSlot").gameObject;
            GameObject p_heldWeapon = p_weaponSlot.transform.GetChild(0).gameObject;
            GameObject shotOrigin = p_heldWeapon.transform.Find("ShotOrigin").gameObject;
            _flamethrowerPS = shotOrigin.GetComponent<ParticleSystem>();
        }

        if (_flamethrowerPS != null && !_flamethrowerPS.isEmitting) {
            _flamethrowerPS.Play();
        }        
    }


    private void StopFire(GameObject user) {
        if (_flamethrowerPS == null) {
            GameObject p_weaponSlot = user.transform.Find("WeaponSlot").gameObject;
            GameObject p_heldWeapon = p_weaponSlot.transform.GetChild(0).gameObject;
            GameObject shotOrigin = p_heldWeapon.transform.Find("ShotOrigin").gameObject;
            _flamethrowerPS = shotOrigin.GetComponent<ParticleSystem>();
        }

        if (_flamethrowerPS != null && _flamethrowerPS.isPlaying) {
            Debug.Log ("stopping flamethrowerPS play");
            _flamethrowerPS.Stop();
        }
    }


    #region Fire()
    public override void fire(GameObject user){
        GameObject p_weaponSlot = user.transform.Find("WeaponSlot").gameObject;
        GameObject p_heldWeapon = p_weaponSlot.transform.GetChild(0).gameObject;
        GameObject shotOrigin = p_heldWeapon.transform.Find("ShotOrigin").gameObject;

        GameObject camera = user.transform.Find("Camera").gameObject;

        // Get player camera forward + max range + capsule radius
        Vector3 shotEnd = camera.transform.position + camera.transform.forward * (_maxRange + _capsuleRadius);

        // Particle on player
        Quaternion attackRotation = Quaternion.LookRotation(camera.transform.forward);
        if (_barrelLaserEffect != ""){
            // ParticleManager.instance.SpawnSelfThenAll(_barrelLaserEffect, user.transform.position, attackRotation);
        }

        // Capsulecast.
        List<Transform> targetsHit = new List<Transform>();

        Debug.DrawRay(shotOrigin.transform.position, shotEnd, Color.red, 2f);

        CapsuleAoe(user, camera, targetsHit);
        DamageTargets(user, targetsHit);
    }
    #endregion
    #region CapsuleAoE()
    private void CapsuleAoe(GameObject user, GameObject camera, List<Transform> targetsHit){
        Vector3 direction = camera.transform.forward;
        Vector3 origin = camera.transform.position;

        Vector3 sphereOneCenter = origin + direction * _capsuleRadius;
        Vector3 sphereTwoCenter = origin + direction * _maxRange - direction * _capsuleRadius;
        Collider[] hits = Physics.OverlapCapsule(sphereOneCenter, sphereTwoCenter, _capsuleRadius, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits){
            if (hit.transform.gameObject == user){
                continue;
            }
            if (hit.transform.CompareTag("Enemy")){
                targetsHit.Add(hit.transform);
            }
        }
    }
    #endregion
    #region Damage()
    private void DamageTargets (GameObject user, List<Transform> targetsHit){
        foreach (Transform target in targetsHit){
            if (target != null){                 
                Damageable damageable = target.GetComponent<Damageable>();
                if (damageable == null){
                    Debug.LogError ("Raycast hit enemy without damageable component.");
                } else {
                    DoDamage(damageable, false, user);
                }

                // Enemy effect
                if (_enemyHitEffect != ""){
                    ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, target.position, Quaternion.Euler(0, 0, 0));
                }
            }
        }
    }

    private void DoDamage (Damageable d, bool isExplosiveDmgType, GameObject user){
        float damage = _flamethrowerDamage;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonusPerSpec = GameManager.instance.DmgSpec_MultiplierPer;
            int dmgSpecLvl = s.GetDmgSpecLvl();
            damage = damage * (1 + bonusPerSpec * dmgSpecLvl);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }
    #endregion
}
