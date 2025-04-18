using UnityEngine;
using System.Collections.Generic;

public class Flamethrower_MONO : Item
{

    #region Flamethrower Item Game Settings
    [Header("Flamethrower Settings")]
    [SerializeField] private float _flamethrowerDamage = 6.0f; // originally 6.0f
    [SerializeField] private float _capsuleRadius = 4.0f; // 4.0f
    [SerializeField] private float _maxRange = 10.0f; // 10.0f

    // Make an effect string = "" to disable spawning an effect.
    [SerializeField] private string _enemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    [SerializeField] private string _barrelLaserEffect = "PistolBarrelFire"; // effect at player

    [SerializeField] private ParticleSystem flameParticle; // particle system for flamethrower.
    [SerializeField] private GameObject shotOrigin;
    #endregion


    #region InternalVars
    private bool _isFireStarted = false;
    #endregion

    public override void ButtonUse(GameObject user) {
        if (lastUsed + cooldown > Time.time) {
            return;
        }

        if (!_isFireStarted){
            StartFireEffect(user); // continuous fire effect.
            _isFireStarted = true;
        } else {
            fire(user); // does damage. 
        }
        lastUsed = Time.time;
    }

    // Same as Use() for flamethrower.
    public override void ButtonHeld(GameObject user)
    {
        if (lastUsed + cooldown > Time.time) {
            return;
        }

        if (!CanAutoFire){
            return;
        }

        if (!_isFireStarted){
            StartFireEffect(user); // continuous fire effect.
            _isFireStarted = true;
        } else {
            fire(user); // does damage. 
        }
        lastUsed = Time.time;
    }

    public override void ButtonRelease(GameObject user){
        if (_isFireStarted){
            _isFireStarted = false; 
            StopFireEffect(user); // stop continuous fire effect.
        }
        lastUsed = float.MinValue;
    }

    public override void Drop(GameObject user)
    {
        if (_isFireStarted){
            _isFireStarted = false; 
            StopFireEffect(user); // stop continuous fire effect.
        }
    }

    public override void SwapCancel(GameObject user)
    {
        if (_isFireStarted){
            _isFireStarted = false; 
            StopFireEffect(user); // stop continuous fire effect.
        }
    }


    #region Effect
    private void StartFireEffect(GameObject user) {
        if (flameParticle != null && !flameParticle.isEmitting) {
            flameParticle.Play();
        }    

    }

    private void StopFireEffect(GameObject user) {
        if (flameParticle != null && flameParticle.isPlaying) {
            Debug.Log ("stopping flamethrowerPS play");
            flameParticle.Stop();
        }

    }

    #endregion 
    #region HELPERS

    // Fire(). Main damage function.
    public void fire(GameObject user){
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

    // Capsule AOE. Stores list of all targets hit in param targetsHit.
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

    // Iterates over hit targets in param, calls DoDamage on each target.
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

    // Does the actual damage to param target.
    private void DoDamage (Damageable d, bool isExplosiveDmgType, GameObject user){
        float damage = _flamethrowerDamage;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonus = s.GetDmgBonus();
            damage = damage * (1 + bonus);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }
    #endregion
}
