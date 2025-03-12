using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class RangedEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    protected override float GetAttackCooldown() => GameManager.instance.RangedEnemy_AttackCooldown;
    protected override float GetAttackRange() => GameManager.instance.RangedEnemy_AttackRange;
    protected override int GetDamage() => GameManager.instance.RangedEnemy_AttackDamage;
    protected override float GetAttackRadius() => 0f; // Ranged enemies might not use this
    protected override bool GetUseGlobalTarget() => GameManager.instance.RangedEnemy_UseGlobalTarget;
    protected override float GetInitialHealth() => GameManager.instance.RangedEnemy_Health;
    protected override float GetSpeed() => GameManager.instance.RangedEnemy_Speed;

    [Header("Ranged Attack Settings")]
    [SerializeField] private float _maxAttackDistance = 10f;
    [SerializeField] private float _minAttackDistance = 4f; // Minimum distance to maintain from player
    [SerializeField] private float _hoveredHeight = 3f; // Height abov/e ground level
    [SerializeField] private Transform _firePoint; // Where projectiles originate from
    [SerializeField] private TrailRenderer _bulletTrailPrefab;
    [SerializeField] private float _trailDuration = 0.3f; // How long the trail effect lasts

    [Header("Flying Settings")]
    [SerializeField] private float _hoverAmplitude = 0.5f;
    [SerializeField] private float _hoverFrequency = 1f;
    private float _hoverOffset;

    private bool _canAttack = true;

    private Animator animator;
    private SoundEmitter[] soundEmitters;

    [Header("Armature Settings")]
    [SerializeField] private Transform _leftGun;
    [SerializeField] private Transform _rightGun;

    //protected override float attackCooldown => 3f;
    //protected override float attackRange => 10f;
    //protected override int damage => 8;
    //protected override bool useGlobalTarget => true;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        animator = GetComponentInChildren<Animator>();
        soundEmitters = GetComponents<SoundEmitter>();




        if (IsServer) {
            _hoverOffset = Random.Range(0f, 2f * Mathf.PI);

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

        if (!IsServer) return;

        Vector3 lookPosition = target.position;
        lookPosition.y = transform.position.y;
        transform.LookAt(lookPosition);

        ApplyHovering();
        UpdateAnimation();
        UpdateWeaponAngle();
    }

    private void ApplyHovering() {
        if (agent != null && agent.enabled) {
            float verticalOffset = _hoverAmplitude * Mathf.Sin(Time.time + _hoverOffset) * _hoverFrequency;
            agent.baseOffset = _hoveredHeight + verticalOffset;
        }
    }

    private void UpdateAnimation() {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat("ForwardSpeed", speed);
    }

    private void UpdateWeaponAngle() {
        if (_leftGun != null && _rightGun != null) {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Debug.DrawRay(_firePoint.position, directionToTarget * 10f, Color.red);
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget) * Quaternion.Euler(90f, 0f, 0f);

            _leftGun.rotation = Quaternion.Slerp(_leftGun.rotation, lookRotation, Time.deltaTime * 5f);
            _rightGun.rotation = Quaternion.Slerp(_rightGun.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    protected override void Attack() {
        if (!_canAttack) return;
        if (target == null) return;
        _canAttack = false;

        if (IsServer) {
            FireBulletServerRpc(target.GetComponent<NetworkObject>().NetworkObjectId);
        }

        StartCoroutine(ResetAttackCooldown());
    }

    [ServerRpc(RequireOwnership = false)]
    private void FireBulletServerRpc(ulong targetNetworkId) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetNetworkObject)) {
            Debug.LogWarning("Target not found.");
            return;
        }

        Transform targetTransform = targetNetworkObject.transform;
        Vector3 targetDirection = (targetTransform.position - _firePoint.position).normalized;
        int playerLayerMask = LayerMask.GetMask("Player");
        int buildingLayerMask = LayerMask.GetMask("Building");
        int combinedLayerMask = playerLayerMask | buildingLayerMask;

        RaycastHit hit;
        if (Physics.Raycast(_firePoint.position, targetDirection, out hit, _maxAttackDistance, combinedLayerMask)) {
            CreateVisualEffectClientRpc(_firePoint.position, hit.point);

            if (hit.collider.gameObject.layer == buildingLayerMask) {
                //Debug.Log("Ranged enemy hit building layer.");
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
        try {
            PlaySoundForEmitter("flying_die", transform.position); 
        } catch (System.Exception e) {
            Debug.LogError("Error playing sound for emitter: " + e.Message);
        }    
        
        StartCoroutine(WaitOneSecond());
        base.OnDie();

    }

    private IEnumerator ResetAttackCooldown() {
        yield return new WaitForSeconds(attackCooldown);
        _canAttack = true;
    }

    private IEnumerator WaitOneSecond() {
        yield return new WaitForSeconds(1f);
    }

    public void PlaySoundForEmitter(string emitterId, Vector3 position) {
        foreach (SoundEmitter emitter in soundEmitters) {
            if (emitter.emitterID == emitterId) {
                emitter.PlayNetworkedSound(position);
                return;
            }
        }
    }
}
