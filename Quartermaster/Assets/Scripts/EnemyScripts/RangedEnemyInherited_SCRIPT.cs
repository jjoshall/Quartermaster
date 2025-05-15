using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Services.Analytics;

public class RangedEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    #region Variables for GameManager
    protected override float GetAttackCooldown() => GameManager.instance.RangedEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.RangedEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.RangedEnemy_AttackDamage;
    protected override float GetAttackRadius() => 0f; // Ranged enemies don't use this, but it's required by the base class
    protected override bool GetUseGlobalTarget() => GameManager.instance.RangedEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.RangedEnemy_Health;
    #endregion

    [Header("Ranged Attack Settings")]
    [SerializeField] private float _maxAttackDistance = 10f;
    [SerializeField] private float _hoveredHeight = 4f; // Height above ground level
    [SerializeField] private Transform _firePoint; // Where projectiles originate from
    [SerializeField] private TrailRenderer _bulletTrailPrefab;
    [SerializeField] private float _trailDuration = 0.3f; // How long the trail effect lasts

    [Header("Flying Settings")]
    [SerializeField] private float _hoverAmplitude = 0.5f;
    [SerializeField] private float _hoverFrequency = 1f;
    private float _hoverOffset;

    [Header("Armature Settings")]
    [SerializeField] private Transform _leftGun;
    [SerializeField] private Transform _rightGun;

    // Used to control the animation update rate
    private int _frameCounter = 0;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        enemyType = EnemyType.Ranged;

        if (IsServer) {
            _hoverOffset = Random.Range(0f, 3f * Mathf.PI);     // makes the hovering look more natural

            // Create a fire point if one isn't assigned
            if (_firePoint == null) {
                GameObject firePointObj = new GameObject("FirePoint");
                firePointObj.transform.parent = transform;
                firePointObj.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                _firePoint = firePointObj.transform;
            }
        }
    }

    protected override void Update() {
        base.Update();

        if (!IsServer || !ShouldAnimate()) return;

        _frameCounter++;
        if (_frameCounter > 5000) {
            _frameCounter = 0; // Reset the frame counter to prevent overflow
        }

        ApplyHovering();
        UpdateAnimation();
        UpdateWeaponAngle();
    }

    // makes drone hover up and down
    private void ApplyHovering() {
        if (agent == null || !agent.enabled || !ShouldAnimate()) {
            return; // Don't apply hovering if the agent is disabled or not animating
        }

        float verticalOffset = _hoverAmplitude * Mathf.Sin(Time.time + _hoverOffset) * _hoverFrequency;
        agent.baseOffset = _hoveredHeight + verticalOffset;
    }

    private void UpdateAnimation() {
        if (animator == null || agent == null) return;

        if (!animator.enabled) {
            animator.enabled = true;
        }

        if (_frameCounter % 5 != 0) return; // Update animation every 5 frames

        float speed = agent.velocity.magnitude;
        animator.SetFloat("ForwardSpeed", speed);
    }

    private void UpdateWeaponAngle() {
        if (_leftGun == null || _rightGun == null) {
            return;
        }

        if (_frameCounter % 8 != 0) { return; }

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget) * Quaternion.Euler(90f, 0f, 0f);

        _leftGun.rotation = Quaternion.Slerp(_leftGun.rotation, lookRotation, Time.deltaTime * 5f);
        _rightGun.rotation = Quaternion.Slerp(_rightGun.rotation, lookRotation, Time.deltaTime * 5f);
    }

    protected override void Attack() {
        if (!IsServer) return;
        if (targetPosition == null) return;

        Collider[] playersAroundTargetPosition = Physics.OverlapSphere(targetPosition, 2f, LayerMask.GetMask("Player"));
        if (playersAroundTargetPosition.Length > 0) {
            // pick random one
            int randomIndex = Random.Range(0, playersAroundTargetPosition.Length);

            try {
                PlaySoundForEmitter("flying_shoot", transform.position);
            }
            catch (System.Exception e) {
                Debug.LogError("Error playing sound for emitter: " + e.Message);
            }

            // raycasts at given position
            FireBulletServerRpc(playersAroundTargetPosition[randomIndex].transform.position);
        }

        // Change to timer?
        // StartCoroutine(ResetAttackCooldown());
    }

    [ServerRpc(RequireOwnership = false)]
    private void FireBulletServerRpc(Vector3 targetPosition) {
        Vector3 targetDirection = (targetPosition - _firePoint.position).normalized;  // enemy won't miss if in range, perhaps change?

        // Masks to make sure enemies cant shoot through walls
        int playerLayerMask = LayerMask.GetMask("Player");
        int buildingLayerMask = LayerMask.GetMask("Building");
        int combinedLayerMask = playerLayerMask | buildingLayerMask;

        RaycastHit hit;
        if (Physics.Raycast(_firePoint.position, targetDirection, out hit, _maxAttackDistance, combinedLayerMask)) {
            // Just instantiates bullet trail effect
            CreateVisualEffectClientRpc(_firePoint.position, hit.point);

            if (hit.collider.gameObject.layer == buildingLayerMask) {
                return;
            }
            else if (hit.collider.CompareTag("Player")) {
                hit.collider.GetComponent<Damageable>().InflictDamage(damage, false, gameObject);
            }
        }
        else {
            Vector3 endPoint = _firePoint.position + targetDirection * _maxAttackDistance;
            CreateVisualEffectClientRpc(_firePoint.position, endPoint);
        }
    }

    [ClientRpc]
    private void CreateVisualEffectClientRpc(Vector3 startPoint, Vector3 endPoint) {
        if (_bulletTrailPrefab != null) {
            TrailRenderer trail = Instantiate(_bulletTrailPrefab, startPoint, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, startPoint, endPoint));
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 startPoint, Vector3 endPoint) {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1) {
            trail.transform.position = Vector3.Lerp(startPoint, endPoint, time);
            time += Time.deltaTime / (_trailDuration * 0.5f);
            yield return null;
        }

        trail.transform.position = endPoint;
        Destroy(trail.gameObject, trail.time);
    }

    protected override void OnDie() {
        if (AnalyticsManager_SCRIPT.Instance != null && AnalyticsManager_SCRIPT.Instance.IsAnalyticsReady()) {
            AnalyticsService.Instance.RecordEvent("RangedEnemyKilled");
        }

        try {
            PlaySoundForEmitter("flying_die", transform.position); 
        } catch (System.Exception e) {
            Debug.LogError("Error playing sound for emitter: " + e.Message);
        }

        // nate did this, change to timer?, this is so ranged enemy doesn't get destroyed from scene before sound finishes
        StartCoroutine(WaitOneSecond());
        base.OnDie();
    }

    private IEnumerator WaitOneSecond() {
        yield return new WaitForSeconds(1f);
    }
}
