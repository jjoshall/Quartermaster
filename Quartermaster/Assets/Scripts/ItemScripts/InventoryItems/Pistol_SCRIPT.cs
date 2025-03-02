using UnityEngine;
using Unity.Netcode;

public class Pistol : IWeapon
{
    #region DesignSettings
    private static float _itemCooldown = 0.5f;
    private static float _pistolDamage = 10.0f;
    public override bool isHoldable { get; set; } = true;

    // ParticleManager spawned prefabs. Make an effect string = "" to disable spawning an effect.
    private static string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    private static string _barrelFireEffect = "PistolBarrelFire"; // effect at player
    
    #endregion
    #region Variables
    private int _id;
    // Backing fields
    private int _quantity = 1;
    private int _ammo = 0;
    private float lastUsedTime = float.MinValue;
    private float lastFiredTime = float.MinValue;

    #endregion
    #region Basics
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

    // Deprecated. Use IsHoldable instead.
    public override bool CanAutoFire(){
        return false;
    }

    public override void Use(GameObject user, bool isHeld)
    {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (lastUsed + cooldown > Time.time){
            //Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            //Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
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
    #region PistolFire()
    public override void fire(GameObject user){
        GameObject camera = user.transform.Find("Camera").gameObject;
        // spawn the pistol barrel fire in direction of camera look
        Quaternion attackRotation = Quaternion.LookRotation(camera.transform.forward);
        if (_barrelFireEffect != ""){
            ParticleManager.instance.SpawnSelfThenAll(_barrelFireEffect, user.transform.position, attackRotation);
        }
        //Debug.DrawRay(camera.transform.position, camera.transform.forward * 100, Color.yellow, 2f);
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 100f, -1, QueryTriggerInteraction.Ignore)){
            Debug.Log("Pistol hit something: " + hit.collider.name);
            // Loop through parents in case enemies have child objs blocking raycast.
            Transform enemyRootObj = hit.transform;
            while (enemyRootObj.parent != null && !enemyRootObj.CompareTag("Enemy")){
                enemyRootObj = enemyRootObj.parent;
                Debug.Log("Enemy that was hit: " + enemyRootObj.name);
            }

            if (enemyRootObj.CompareTag("Enemy")){
                // get the rotation based on surface normal of the hit on the enemy
                Vector3 hitNormal = hit.normal;
                Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

                if (_enemyHitEffect != ""){
                    ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, enemyRootObj.position, hitRotation);
                }
                Damageable damageable = enemyRootObj.GetComponent<Damageable>();
                if (damageable == null){
                    Debug.LogError ("Raycast hit enemy without damageable component.");
                } else {
                    damageable?.InflictDamage(_pistolDamage, false, user);
                }
            }
        }
        
    }
    #endregion

}
