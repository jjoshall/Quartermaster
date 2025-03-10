using UnityEngine;
using Unity.Netcode;
using UnityEditor;

public class Pistol : IWeapon
{
    #region DesignSettings
    // Deprecated. Use values in gamemanager instead.
    private static float _itemCooldown = 0.0f;
    private static float _pistolDamage = 0.0f;

    public override bool isHoldable { get; set; } = false;

    // ParticleManager spawned prefabs. Make an effect string = "" to disable spawning an effect.
    private static string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    private static string _barrelFireEffect = "PistolBarrelFire"; // effect at player
    
    #endregion
    #region Variables
    // private int _id;
    // Backing fields
    private int _quantity = 1;
    // private int _ammo = 0;
    // private float lastUsedTime = float.MinValue;
    // private float lastFiredTime = float.MinValue;

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


    public override void InitializeFromGameManager()
    {
        _pistolDamage = GameManager.instance.Pistol_Damage;
        _itemCooldown = GameManager.instance.Pistol_Cooldown;
    }

    // Deprecated. Use IsHoldable instead.
    public override bool CanAutoFire(){
        return false;
    }

    public override bool IsWeapon()
    {
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
        GameObject p_weaponSlot = user.transform.Find("WeaponSlot").gameObject;
        GameObject p_heldWeapon = p_weaponSlot.transform.GetChild(0).gameObject;
        GameObject shotOrigin = p_heldWeapon.transform.Find("ShotOrigin").gameObject;

        GameObject camera = user.transform.Find("Camera").gameObject;
        int enemyLayer = LayerMask.GetMask("Enemy");
        int buildingLayer = LayerMask.GetMask("Building");
        int combinedLayerMask = enemyLayer | buildingLayer;

        //Debug.DrawRay(camera.transform.position, camera.transform.forward * 100, Color.yellow, 2f);
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 100f, combinedLayerMask, QueryTriggerInteraction.Ignore)){
            Debug.Log("Pistol hit something: " + hit.collider.name + " on layer: " + hit.collider.gameObject.layer);

            // draw a ray from the shotOrigin to the hit point (for debug)
            Debug.DrawRay(shotOrigin.transform.position, hit.point - shotOrigin.transform.position, Color.green, 2f);

            // ~---- SPAWN TRAIL RENDER FROM SHOT ORIGIN TO HIT POINT ----~
            WeaponEffects effects = user.GetComponent<WeaponEffects>();
            NetworkObject userNetObj = user.GetComponent<NetworkObject>();

            if (effects != null && userNetObj != null) {
                if (_barrelFireEffect != ""){
                    // spawn the pistol barrel fire in direction of camera look
                    Quaternion attackRotation = Quaternion.LookRotation(hit.point - shotOrigin.transform.position);
                    ParticleManager.instance.SpawnSelfThenAll(_barrelFireEffect, shotOrigin.transform.position, attackRotation);
                }
                if (NetworkManager.Singleton.IsServer) {
                    // If the user (player) is the server, spawn the trail directly.
                    effects.SpawnBulletTrailClientRpc(shotOrigin.transform.position, hit.point, itemID);
                }
                else {
                    // If the user is a client, request the server to spawn the trail.
                    effects.RequestSpawnBulletTrailServerRpc(shotOrigin.transform.position, hit.point, itemID);
                }
            }

            // Check if the hit object is a building
            if (hit.collider.gameObject.layer == buildingLayer) {
                Debug.Log("Hit building: " + hit.collider.name);
                return;
            }

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
                    // damageable?.InflictDamage(_pistolDamage, false, user);
                    DoDamage(damageable, false, user);
                }
            }
        }
    }

    
    private void DoDamage (Damageable d, bool isExplosiveDmgType, GameObject user){
        float damage = _pistolDamage;
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
