using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class Flamethrower : IWeapon
{
    #region DesignSettings
    // Change these to adjust weapon stats.
    
    private static float _itemCooldown = 0.2f;
    private static float _flamethrowerDamage = 6.0f;
    private static float _capsuleRadius = 4.0f;
    private static float _maxRange = 10.0f;
    public override bool isHoldable { get; set; } = true;

    // Make an effect string = "" to disable spawning an effect.
    private static string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    private static string _barrelLaserEffect = "PistolBarrelFire"; // effect at player

    #endregion
    #region Variables
    // Backing fields. Don't touch these.
    private int _id;
    
    private int _quantity = 1;
    private int _ammo = 0;
    private float lastUsedTime = float.MinValue;
    private float lastFiredTime = float.MinValue;

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

    public override float lastUsed {
        get => lastUsedTime;
        set => lastUsedTime = value;
    }

    public override bool CanAutoFire(){
        return false;
    }

    public override void Use(GameObject user, bool isHeld)
    {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (lastUsed + cooldown > Time.time){
            return;
        }
        //Debug.Log(itemStr + " (" + itemID + ") used");
    
        if (IsConsumable()){
            quantity--;
        }
        lastUsed = Time.time;

        ItemEffect(user);

    }

    private void ItemEffect(GameObject user){
        // Do some kind of alternate attack, or reload.
        fire(user);
    }
    #endregion

    public override float GetCooldownRemaining() {
        return Mathf.Max(0, (lastUsed + _itemCooldown) - Time.time);
    }

    public override float GetMaxCooldown() {
        return _itemCooldown;
    }


    #region Fire()
    public override void fire(GameObject user){
        GameObject camera = user.transform.Find("Camera").gameObject;
        
        // particle on player
        Quaternion attackRotation = Quaternion.LookRotation(camera.transform.forward);
        if (_barrelLaserEffect != ""){
            ParticleManager.instance.SpawnSelfThenAll(_barrelLaserEffect, user.transform.position, attackRotation);
        }
        // piercing raycast
        List<Transform> targetsHit = new List<Transform>();

        CapsuleAoe(user, camera, targetsHit); // calls explosion if environment hit.
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
                    damageable?.InflictDamage(_flamethrowerDamage, false, user);
                }
                
                // enemy effect
                if (_enemyHitEffect != ""){
                    ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, target.position, Quaternion.Euler(0, 0, 0));
                }
            }
        }
    }

    #endregion

}
