using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class Railgun : IWeapon
{
    #region DesignSettings
    // Change these to adjust weapon stats.
    
    private static float _itemCooldown = 1.0f;
    private static float _railgunDamage = 10.0f;
    private static float _explosionRadius = 10.0f;
    public override bool isHoldable { get; set; } = false;

    // Make an effect string = "" to disable spawning an effect.
    private static string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    private static string _explosionEffect = ""; // effect spawned at environment explosion contact pt
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
        int enemyLayer = LayerMask.GetMask("Enemy");
        int buildingLayer = LayerMask.GetMask("Building");
        int combinedLayerMask = enemyLayer | buildingLayer;

        // particle on player
        Quaternion attackRotation = Quaternion.LookRotation(camera.transform.forward);
        if (_barrelLaserEffect != ""){
            ParticleManager.instance.SpawnSelfThenAll(_barrelLaserEffect, camera.transform.position, attackRotation);
        }


        // piercing raycast
        List<Transform> targetsHit = new List<Transform>();

        LineAoe(user, camera, targetsHit, combinedLayerMask); // calls explosion if environment hit.
        DamageTargets(user, targetsHit);
        
    }
    #endregion

    #region LineAoE()

    private void LineAoe(GameObject user, GameObject camera, List<Transform> targetsHit, int combinedLayerMask){
        GameObject p_weaponSlot = user.transform.Find("WeaponSlot").gameObject;
        GameObject p_heldWeapon = p_weaponSlot.transform.GetChild(0).gameObject;
        GameObject shotOrigin = p_heldWeapon.transform.Find("ShotOrigin").gameObject;

        
        RaycastHit[] hits;
        hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, 100.0f, combinedLayerMask);
        // hits order undefined, so we sort.
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        if (hits.Length > 0){

            // draw a ray from the shotOrigin to the hit point
            Debug.DrawRay(shotOrigin.transform.position, hits[0].point - shotOrigin.transform.position, Color.blue, 2f);



            foreach (RaycastHit hit in hits){
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Building") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("whatIsGround"))
                {
                    Debug.Log ("railgun hit building/whatisground");
                    SpawnExplosion(hit.point, _explosionRadius, targetsHit);
                    break;
                }

                if (!hit.collider.CompareTag("Enemy") && !hit.collider.CompareTag("Player")){
                    
                    Debug.Log ("railgun hit non-enemy && non-player");
                    SpawnExplosion(hit.point, _explosionRadius, targetsHit);
                    break;
                }
                Transform enemyRootObj = hit.transform;
                while (enemyRootObj.parent != null && !enemyRootObj.CompareTag("Enemy")){
                    enemyRootObj = enemyRootObj.parent;
                }

                if (enemyRootObj.CompareTag("Enemy")){
                    if (targetsHit.Contains(enemyRootObj)){
                        continue;
                    }
                    // get the rotation based on surface normal of the hit on the enemy
                    Vector3 hitNormal = hit.normal;
                    Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

                    targetsHit.Add(enemyRootObj);
                }
            }
        }
    }
    #endregion

    #region Explosion()

    private void SpawnExplosion(Vector3 position, float aoeRadius, List<Transform> targetsHit){
        if (_explosionEffect != ""){
            ParticleManager.instance.SpawnSelfThenAll(_explosionEffect, position, Quaternion.Euler(0, 0, 0));
        }
        
        Collider[] collisions = Physics.OverlapSphere(position, aoeRadius, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Collide);
        
        foreach (Collider hit in collisions){
            Transform enemyRootObj = hit.transform;
            while (enemyRootObj.parent != null && !enemyRootObj.CompareTag("Enemy")){
                enemyRootObj = enemyRootObj.parent;
            }

            if (enemyRootObj.CompareTag("Enemy")){
                    if (targetsHit.Contains(enemyRootObj)){
                        continue;
                    }
                // get the rotation based on surface normal of the hit on the enemy
                Vector3 hitNormal = hit.transform.position - position;
                Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

                targetsHit.Add(enemyRootObj);
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
                    damageable?.InflictDamage(_railgunDamage, false, user);
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
