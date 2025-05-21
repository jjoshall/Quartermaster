using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;
using Unity.Netcode;

public class RailgunItem : Item
{

    #region Railgun Item Game Settings
    [Header("Railgun Settings")]
    [SerializeField] private float _railgunDamage = 17.0f; // originally 6.0f
    [SerializeField] private float _explosionRadius = 10.0f;
    [SerializeField] private float _maxRange = 40.0f;


    // Make an effect string = "" to disable spawning an effect.
    [SerializeField] private string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    [SerializeField] private string _explosionEffect = "RailgunExplosion";
    [SerializeField] private string _barrelLaserEffect = ""; // effect at player

    [SerializeField] private GameObject shotOrigin;

    [SerializeField] private int trailRenderID = 0; // REFACTOR THIS TO BE A DIRECT REFERENCE TO TRAILRENDERER OBJECT
                                             // where is weaponEffects attached to?
                                             // weaponEffects uses itemID to index an inspector assigned list of trailrenderer prefabs.
    #endregion




    public override void OnButtonUse(GameObject user) {
        if (AnalyticsManager_SCRIPT.Instance != null && AnalyticsManager_SCRIPT.Instance.IsAnalyticsReady()) {
            AnalyticsService.Instance.RecordEvent("RailgunUsed");
        }
        PlayerController pc = user.GetComponent<PlayerController>();
        if (user == null || pc == null) {
            Debug.LogError("Pistol_MONO: ButtonUse() NullChecks failed.");
            return;
        }
        if (GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time){
            //Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            //Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }
        //Debug.Log(itemStr + " (" + itemID + ") used");

        SetLastUsed(Time.time);

        fire(user);
    }

    // Same as Use() for Railgun.
    public override void OnButtonHeld(GameObject user)
    {
        if (user == null || user.GetComponent<PlayerStatus>() == null) {
            Debug.LogError("Railgun_MONO: ButtonHeld() NullChecks failed.");
            return;
        }

        PlayerController pc = user.GetComponent<PlayerController>();
        if (user == null || pc == null) {
            Debug.LogError("Pistol_MONO: ButtonUse() NullChecks failed.");
            return;
        }
        if (GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time){
            //Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            //Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }

        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s == null) {
            Debug.LogError("Flamethrower_MONO: ButtonHeld() NullChecks failed.");
            return;
        }
        bool autofire = CanAutoFire || s.stimActive;
        
        if (!autofire){
            return;
        }

        SetLastUsed(Time.time);

        fire (user);
    }




    public void fire(GameObject user){
        GameObject camera = user.transform.Find("Camera").gameObject;
        int enemyLayer = LayerMask.GetMask("Enemy");
        int buildingLayer = LayerMask.GetMask("Building");
        int combinedLayerMask = enemyLayer | buildingLayer;
        int groundLayer = LayerMask.GetMask("whatIsGround");
        combinedLayerMask = combinedLayerMask | groundLayer;

        // piercing raycast
        List<Transform> targetsHit = new List<Transform>();

        soundEmitters = user.GetComponents<SoundEmitter>();
        string emitterId = "railgun_shot";

        foreach (SoundEmitter emitter in soundEmitters) {
            if (emitter.emitterID == emitterId) {
                emitter.PlayNetworkedSound(shotOrigin.transform.position);
            }
        }


        LineAoe(user, camera, targetsHit, combinedLayerMask); // calls explosion if environment hit.
        DamageTargets(user, targetsHit);
        
    }

    #region LineAoE()

    private void LineAoe(GameObject user, GameObject camera, List<Transform> targetsHit, int combinedLayerMask){
        
        RaycastHit[] hits;
        hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, _maxRange, combinedLayerMask);
        // hits order undefined, so we sort.
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        if (hits.Length > 0){
            // particle on player
            Quaternion attackRotation = Quaternion.LookRotation(hits[0].point - shotOrigin.transform.position);
            if (_barrelLaserEffect != ""){
                ParticleManager.instance.SpawnSelfThenAll(_barrelLaserEffect, shotOrigin.transform.position, attackRotation);
            }
            // draw a ray from the shotOrigin to the hit point
            //Debug.DrawRay(shotOrigin.transform.position, hits[0].point - shotOrigin.transform.position, Color.blue, 2f);

            // ~---- SPAWN TRAIL RENDER FROM SHOT ORIGIN TO HIT POINT ----~
            WeaponEffects effects = user.GetComponent<WeaponEffects>();
            NetworkObject userNetObj = user.GetComponent<NetworkObject>();

            if (effects != null && userNetObj != null) {
                //Debug.Log ("spawning trail");
                if (NetworkManager.Singleton.IsServer) {
                    // If the user (player) is the server, spawn the trail directly.
                    effects.SpawnBulletTrailClientRpc(shotOrigin.transform.position, hits[0].point, trailRenderID);
                }
                else {
                    // If the user is a client, request the server to spawn the trail.
                    effects.RequestSpawnBulletTrailServerRpc(shotOrigin.transform.position, hits[0].point, trailRenderID);
                }
            }



            foreach (RaycastHit hit in hits){
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Building") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("whatIsGround") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    //Debug.Log ("railgun hit building/whatisground");
                    SpawnExplosion(hit.point, _explosionRadius, targetsHit);
                    break;
                }

                if (!hit.collider.CompareTag("Enemy") && !hit.collider.CompareTag("Player")){
                    
                    //Debug.Log ("railgun hit non-enemy && non-player");
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
            ParticleManager.instance.SpawnSelfThenAll(_explosionEffect, position, Quaternion.Euler(0, 0, 0), _explosionRadius);
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
                    // damageable?.InflictDamage(_railgunDamage, false, user);
                    DoDamage(damageable, false, user);
                }
                
                // enemy effect
                if (_enemyHitEffect != ""){
                    ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, target.position, Quaternion.Euler(0, 0, 0));
                }
            }
        }
    }

    
    private void DoDamage (Damageable d, bool isExplosiveDmgType, GameObject user){
        float damage = _railgunDamage;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonus = s.GetDmgBonus();
            damage = damage * (1 + bonus);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

    #endregion
}
