using UnityEngine;
using Unity.Netcode;

public class Pistol : IWeapon
{
    // Backing fields
    private int _id;
    
    private int _quantity = 1;
    private int _ammo = 0;
    private float lastUsedTime = float.MinValue;
    private float lastFiredTime = float.MinValue;
    private static float itemCooldown = 0.5f;

    // Abstract overrides

    public override bool isHoldable { get; set; } = true;
    public override float cooldown
    {
        get => itemCooldown;
        set => itemCooldown = value;
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

    public override void fire(GameObject user){
        // raycast in direction camera is facing

        // int layerMask = layerMask.GetMask("Enemy", "Wall");

        // camera is a child, we do not know which one
        GameObject camera = user.transform.Find("Camera").gameObject;
        // spawn the pistol barrel fire in direction of camera look
        Quaternion attackRotation = Quaternion.LookRotation(camera.transform.forward);
        ParticleManager.instance.SpawnSelfThenAll("PistolBarrelFire", user.transform.position, attackRotation);
        
        //Debug.DrawRay(camera.transform.position, camera.transform.forward * 100, Color.yellow, 2f);
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 100f, -1, QueryTriggerInteraction.Ignore)){

            // Loop through parents in case enemies have child objs blocking raycast.
            Transform enemyRootObj = hit.transform;
            while (enemyRootObj.parent != null && !enemyRootObj.CompareTag("Enemy")){
                enemyRootObj = enemyRootObj.parent;
            }

            if (enemyRootObj.CompareTag("Enemy")){
                // get the rotation based on surface normal of the hit on the enemy
                Vector3 hitNormal = hit.normal;
                Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

                ParticleManager.instance.SpawnSelfThenAll("Sample", enemyRootObj.position, hitRotation);
                
                Damageable damageable = enemyRootObj.GetComponent<Damageable>();
                if (damageable == null){
                    Debug.LogError ("Raycast hit enemy without damageable component.");
                } else {
                    damageable?.InflictDamage(10, false, user);
                }
            }
        }
        
    }

}
