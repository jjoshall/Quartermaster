using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Analytics;


public class Pistol_MONO : Item
{

    #region Pistol Item Game Settings
    [Header("Pistol Settings")]
    [SerializeField] private float _pistolDamage = 17.0f; // originally 6.0f
    [SerializeField] private float _maxRange = 40.0f;

    // Make an effect string = "" to disable spawning an effect.
    [SerializeField] private string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    [SerializeField] private string _barrelFireEffect = "PistolBarrelFire"; // effect at player

    [SerializeField] private GameObject shotOrigin;

    [SerializeField] private int trailRenderID = 0; // REFACTOR THIS TO BE A DIRECT REFERENCE TO TRAILRENDERER OBJECT
                                                    // where is weaponEffects attached to?
                                                    // weaponEffects uses itemID to index an inspector assigned list of trailrenderer prefabs.
    #endregion




    public override void OnButtonUse(GameObject user)
    {
        if (AnalyticsManager_SCRIPT.Instance != null && AnalyticsManager_SCRIPT.Instance.IsAnalyticsReady())
        {
            AnalyticsService.Instance.RecordEvent("PistolUsed");
        }

        PlayerController pc = user.GetComponent<PlayerController>();
        if (user == null || pc == null)
        {
            Debug.LogError("Pistol_MONO: ButtonUse() NullChecks failed.");
            return;
        }
        if (GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time)
        {
            //Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            //Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }
        //Debug.Log(itemStr + " (" + itemID + ") used");

        SetLastUsed(Time.time);

        fire(user);
    }

    // Same as Use() for Pistol.
    public override void OnButtonHeld(GameObject user)
    {
        if (user == null || user.GetComponent<PlayerStatus>() == null)
        {
            Debug.LogError("Pistol_MONO: ButtonHeld() NullChecks failed.");
            return;
        }

        PlayerController pc = user.GetComponent<PlayerController>();
        if (user == null || pc == null)
        {
            Debug.LogError("Pistol_MONO: ButtonUse() NullChecks failed.");
            return;
        }
        if (GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time)
        {
            //Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            //Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s == null)
        {
            Debug.LogError("Flamethrower_MONO: ButtonHeld() NullChecks failed.");
            return;
        }
        bool autofire = CanAutoFire || s.stimActive;

        if (!autofire)
        {
            return;
        }

        SetLastUsed(Time.time);

        fire(user);
    }

    public void fire(GameObject user)
    {

        PlaySoundEmitter("pistol_shot");

        GameObject camera = user.transform.Find("Camera").gameObject;
        int enemyLayer = LayerMask.GetMask("Enemy");
        int buildingLayer = LayerMask.GetMask("Building");
        int combinedLayerMask = enemyLayer | buildingLayer;

        //Debug.DrawRay(camera.transform.position, camera.transform.forward * 100, Color.yellow, 2f);
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, _maxRange, combinedLayerMask, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("Pistol hit something: " + hit.collider.name + " on layer: " + hit.collider.gameObject.layer);

            // draw a ray from the shotOrigin to the hit point (for debug)
            //Debug.DrawRay(shotOrigin.transform.position, hit.point - shotOrigin.transform.position, Color.green, 2f);

            // ~---- SPAWN TRAIL RENDER FROM SHOT ORIGIN TO HIT POINT ----~
            WeaponEffects effects = GetComponent<WeaponEffects>(); // get from item instead of player obj.
            NetworkObject userNetObj = user.GetComponent<NetworkObject>();

            if (effects != null && userNetObj != null)
            {
                if (_barrelFireEffect != "")
                {
                    // spawn the pistol barrel fire in direction of camera look
                    //Quaternion attackRotation = Quaternion.LookRotation(hit.point - shotOrigin.transform.position);
                    //ParticleManager.instance.SpawnSelfThenAll(_barrelFireEffect, shotOrigin.transform.position, attackRotation);
                }

                Debug.Log("spawning trail");
                if (NetworkManager.Singleton.IsServer)
                {
                    // If the user (player) is the server, spawn the trail directly.
                    effects.SpawnBulletTrailClientRpc(shotOrigin.transform.position, hit.point, trailRenderID);
                }
                else
                {
                    // If the user is a client, request the server to spawn the trail.
                    effects.RequestSpawnBulletTrailServerRpc(shotOrigin.transform.position, hit.point, trailRenderID);
                }

            }

            // Check if the hit object is a building
            if (hit.collider.gameObject.layer == buildingLayer)
            {
                //Debug.Log("Hit building: " + hit.collider.name);
                return;
            }

            // Loop through parents in case enemies have child objs blocking raycast.
            Transform enemyRootObj = hit.transform;
            while (enemyRootObj.parent != null && !enemyRootObj.CompareTag("Enemy"))
            {
                enemyRootObj = enemyRootObj.parent;
                //Debug.Log("Enemy that was hit: " + enemyRootObj.name);
            }

            if (enemyRootObj.CompareTag("Enemy"))
            {
                // get the rotation based on surface normal of the hit on the enemy
                Vector3 hitNormal = hit.normal;
                Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

                if (_enemyHitEffect != "")
                {
                    ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, enemyRootObj.position, hitRotation);
                }
                Damageable damageable = enemyRootObj.GetComponent<Damageable>();
                if (damageable == null)
                {
                    Debug.LogError("Raycast hit enemy without damageable component.");
                }
                else
                {
                    // damageable?.InflictDamage(_pistolDamage, false, user);
                    DoDamage(damageable, false, user);
                }
            }
        }
    }


    // Does the actual damage to param target.
    private void DoDamage(Damageable d, bool isExplosiveDmgType, GameObject user)
    {
        float damage = _pistolDamage;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null)
        {
            float bonus = s.GetDmgBonus();
            damage = damage * (1 + bonus);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

    #region Turret
    #endregion

    public override void TurretItemLoopBehavior(GameObject turret, float lastUsed)
    {
        if (TurretNullChecksFailed(turret)) return;
        if (TurretCooldownCheckFailed(lastUsed)) return;

        SetLastUsed(Time.time);
        TurretFire(turret);

    }

    private void TurretFire(GameObject turret)
    {
        PlaySoundEmitter("pistol_shot");
        TurretController_SCRIPT tc = turret.GetComponent<TurretController_SCRIPT>();
        if (tc == null){
            Debug.LogError("Pistol: Turret Controller not found");
            return;
        }

        // this should also be shot origin?
        GameObject turretPointer = tc.BulletSpawnPoint.gameObject;  // replace this with whatever object is actually aligned with the turret's targeting

        int enemyLayer = LayerMask.GetMask(tc._TargetTag);
        int buildingLayer = LayerMask.GetMask("Building");
        //int combinedLayerMask = enemyLayer | buildingLayer;
        int combinedLayerMask = tc.BulletLayerMask;

        //Debug.DrawRay(camera.transform.position, camera.transform.forward * 100, Color.yellow, 2f);
        if (Physics.Raycast(turretPointer.transform.position, turretPointer.transform.forward, out RaycastHit hit, _maxRange, combinedLayerMask, QueryTriggerInteraction.Ignore))
        {
            WeaponEffects effects = GetComponent<WeaponEffects>();
            NetworkObject userNetObj = GetComponent<NetworkObject>();

            if (effects != null && userNetObj != null)
            {
                // Particle effect
                // if (_barrelFireEffect != "")
                // {
                        // spawn the pistol barrel fire in direction of camera look
                        //Quaternion attackRotation = Quaternion.LookRotation(hit.point - shotOrigin.transform.position);
                        //ParticleManager.instance.SpawnSelfThenAll(_barrelFireEffect, shotOrigin.transform.position, attackRotation);
                // }

                Debug.Log("spawning trail");
                if (NetworkManager.Singleton.IsServer)
                {
                    // If the user (player) is the server, spawn the trail directly.
                    effects.SpawnBulletTrailClientRpc(shotOrigin.transform.position, hit.point, trailRenderID);
                }
                else
                {
                    // If the user is a client, request the server to spawn the trail.
                    effects.RequestSpawnBulletTrailServerRpc(shotOrigin.transform.position, hit.point, trailRenderID);
                }

            }

            // Check if the hit object is a building
            if (hit.collider.gameObject.layer == buildingLayer)
            {
                //Debug.Log("Hit building: " + hit.collider.name);
                return;
            }

            // Loop through parents in case enemies have child objs blocking raycast.
            Transform enemyRootObj = hit.transform;
            while (enemyRootObj.parent != null && !enemyRootObj.CompareTag(tc._TargetTag))
            {
                enemyRootObj = enemyRootObj.parent;
                //Debug.Log("Enemy that was hit: " + enemyRootObj.name);
            }

            if (enemyRootObj.CompareTag(tc._TargetTag))
            {
                // get the rotation based on surface normal of the hit on the enemy
                Vector3 hitNormal = hit.normal;
                Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

                if (_enemyHitEffect != "")
                {
                    ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, enemyRootObj.position, hitRotation);
                }
                Damageable damageable = enemyRootObj.GetComponent<Damageable>();
                if (damageable == null)
                {
                    Debug.LogError("Raycast hit enemy without damageable component.");
                }
                else
                {
                    // damageable?.InflictDamage(_pistolDamage, false, user);
                    DoDamage(damageable, false, turret);
                }
            }
        }
    }



}
