using UnityEngine;
using Unity.Netcode;
using Unity.Services.Analytics;

public class Pistol_MONO : Item {
    [Header("Pistol Settings")]
    [SerializeField] private float _pistolDamage = 17f;
    [SerializeField] private float _maxRange = 40f;
    [SerializeField] private string _enemyHitEffect = "Sample";
    [SerializeField] private string _barrelFireEffect = "PistolBarrelFire";
    [SerializeField] private GameObject shotOrigin;
    [SerializeField] private int trailRenderID = 0;

    public override void OnButtonUse(GameObject user) {
        if (AnalyticsManager_SCRIPT.Instance?.IsAnalyticsReady() == true)
            AnalyticsService.Instance.RecordEvent("PistolUsed");

        var pc = user.GetComponent<PlayerController>();
        if (pc == null || GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time) return;

        SetLastUsed(Time.time);
        Fire(user);
    }

    public override void OnButtonHeld(GameObject user) {
        var pc = user.GetComponent<PlayerController>();
        var s = user.GetComponent<PlayerStatus>();
        if (pc == null || s == null || (!CanAutoFire && !s.stimActive)) return;
        if (GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time) return;

        SetLastUsed(Time.time);
        Fire(user);
    }

    private void Fire(GameObject user) {
        // always play shot sound
        foreach (var em in user.GetComponents<SoundEmitter>())
            if (em.emitterID == "pistol_shot")
                em.PlayNetworkedSound(shotOrigin.transform.position);

        var cam = user.transform.Find("Camera").gameObject;
        int mask = LayerMask.GetMask("Enemy", "Building");
        Vector3 startPoint = shotOrigin.transform.position;
        Vector3 endPoint = cam.transform.position + cam.transform.forward * _maxRange;

        RaycastHit hit;
        bool didHit = Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, _maxRange, mask, QueryTriggerInteraction.Ignore);
        if (didHit) {
            endPoint = hit.point;
        }

        // always spawn trail
        var effects = GetComponent<WeaponEffects>();
        var netObj = user.GetComponent<NetworkObject>();
        if (effects != null && netObj != null) {
            if (NetworkManager.Singleton.IsServer)
                effects.SpawnBulletTrailClientRpc(startPoint, endPoint, trailRenderID);
            else
                effects.RequestSpawnBulletTrailServerRpc(startPoint, endPoint, trailRenderID);
        }

        // only apply damage/effects on an enemy hit
        if (didHit && hit.collider.CompareTag("Enemy")) {
            if (_barrelFireEffect != "")
                ParticleManager.instance.SpawnSelfThenAll(_barrelFireEffect, startPoint,
                    Quaternion.LookRotation(endPoint - startPoint));
            var dmgable = hit.collider.transform;
            while (dmgable.parent != null && !dmgable.CompareTag("Enemy")) dmgable = dmgable.parent;
            var d = dmgable.GetComponent<Damageable>();
            if (d != null)
                d.InflictDamage(_pistolDamage * (1 + (user.GetComponent<PlayerStatus>()?.GetDmgBonus() ?? 0f)), false, user);
            if (_enemyHitEffect != "")
                ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, dmgable.position, Quaternion.identity);
        }
    }

    public override void TurretItemLoopBehavior(GameObject turret, float lastUsed) {
        if (TurretNullChecksFailed(turret)) return;
        if (GetLastUsedTurret(turret) + turretCooldown > Time.time) return;
        SetLastUsedTurret(turret, Time.time);
    }
}
