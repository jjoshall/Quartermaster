using UnityEngine;
using Unity.Services.Analytics;
using System.Collections.Generic;
using Unity.Netcode;

public class RailgunItem : Item {
    #region Railgun Item Game Settings
    [Header("Railgun Settings")]
    [SerializeField] private float _railgunDamage = 17.0f;
    [SerializeField] private float _explosionRadius = 10.0f;
    [SerializeField] private float _maxRange = 40.0f;
    [SerializeField] private float _slowPotency = 0.5f;
    [SerializeField] private float _slowDuration = 2.0f;

    [SerializeField] private string _enemyHitEffect = "Sample";
    [SerializeField] private string _explosionEffect = "RailgunExplosion";
    [SerializeField] private string _barrelLaserEffect = "";
    [SerializeField] private GameObject shotOrigin;
    [SerializeField] private int trailRenderID = 0;
    #endregion

    public override void OnButtonUse(GameObject user) {
        if (AnalyticsManager_SCRIPT.Instance?.IsAnalyticsReady() == true)
            AnalyticsService.Instance.RecordEvent("RailgunUsed");

        var pc = user.GetComponent<PlayerController>();
        if (pc == null || GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time)
            return;

        SetLastUsed(Time.time);
        Fire(user);
    }

    public override void OnButtonHeld(GameObject user) {
        var pc = user.GetComponent<PlayerController>();
        var s = user.GetComponent<PlayerStatus>();
        if (pc == null || s == null) return;
        if (!(CanAutoFire || s.stimActive)) return;
        if (GetLastUsed() + cooldown / pc.stimAspdMultiplier > Time.time) return;

        SetLastUsed(Time.time);
        Fire(user);
    }

    private void Fire(GameObject user) {
        // always play shot sound
        foreach (var emitter in user.GetComponents<SoundEmitter>())
            if (emitter.emitterID == "railgun_shot")
                emitter.PlayNetworkedSound(shotOrigin.transform.position);

        var cam = user.transform.Find("Camera").gameObject;
        int mask = LayerMask.GetMask("Enemy", "Building", "whatIsGround");
        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, _maxRange, mask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // spawn trail every time
        Vector3 endPoint = hits.Length > 0
            ? hits[0].point
            : cam.transform.position + cam.transform.forward * _maxRange;
        SpawnTrail(user, shotOrigin.transform.position, endPoint);

        // barrel effect on hit
        if (hits.Length > 0 && _barrelLaserEffect != "") {
            var rot = Quaternion.LookRotation(hits[0].point - shotOrigin.transform.position);
            ParticleManager.instance.SpawnSelfThenAll(_barrelLaserEffect, shotOrigin.transform.position, rot);
        }

        // only if we hit something do explosion & damage
        if (hits.Length > 0) {
            var targets = new List<Transform>();
            foreach (var hit in hits) {
                int layer = hit.collider.gameObject.layer;
                if (layer == LayerMask.NameToLayer("Building")
                    || layer == LayerMask.NameToLayer("whatIsGround")) {
                    ExplodeAt(hit.point, targets);
                    break;
                }
                if (!hit.collider.CompareTag("Enemy") && !hit.collider.CompareTag("Player")) {
                    ExplodeAt(hit.point, targets);
                    break;
                }
                // enemy logic
                var root = hit.transform;
                while (root.parent != null && !root.CompareTag("Enemy")) root = root.parent;
                if (root.CompareTag("Enemy") && !targets.Contains(root))
                    targets.Add(root);
            }
            DamageAndSlow(user, targets);
        }
    }

    private void SpawnTrail(GameObject user, Vector3 start, Vector3 end) {
        var effects = user.GetComponent<WeaponEffects>();
        var netObj = user.GetComponent<NetworkObject>();
        if (effects == null || netObj == null) return;

        if (NetworkManager.Singleton.IsServer)
            effects.SpawnBulletTrailClientRpc(start, end, trailRenderID);
        else
            effects.RequestSpawnBulletTrailServerRpc(start, end, trailRenderID);
    }

    private void ExplodeAt(Vector3 pos, List<Transform> targets) {
        if (_explosionEffect != "")
            ParticleManager.instance.SpawnSelfThenAll(_explosionEffect, pos, Quaternion.identity, _explosionRadius);

        foreach (var col in Physics.OverlapSphere(pos, _explosionRadius, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Collide)) {
            var root = col.transform;
            while (root.parent != null && !root.CompareTag("Enemy")) root = root.parent;
            if (root.CompareTag("Enemy") && !targets.Contains(root))
                targets.Add(root);
        }
    }

    private void DamageAndSlow(GameObject user, List<Transform> targets) {
        foreach (var t in targets) {
            var damageable = t.GetComponent<Damageable>();
            if (damageable != null) {
                float bonus = user.GetComponent<PlayerStatus>()?.GetDmgBonus() ?? 0f;
                damageable.InflictDamage(_railgunDamage * (1 + bonus), false, user);
            }

            if (_enemyHitEffect != "")
                ParticleManager.instance.SpawnSelfThenAll(_enemyHitEffect, t.position, Quaternion.identity);

            var enemy = t.GetComponent<BaseEnemyClass_SCRIPT>();
            if (enemy != null)
                enemy.ApplyTimedSlowServerRpc(_slowPotency, _slowDuration);
        }
    }
}
