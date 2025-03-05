using Unity.Netcode;
using System.Collections;
using UnityEngine;
using Unity.Services.Matchmaker.Models;

public class ExplosiveMeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    protected override float attackCooldown => 2f;
    protected override float attackRange => 3f;
    protected override int damage => 45;
    protected override float attackRadius => 3.5f;

    private bool _isExploding = false;

    #region Explosion Blinking Visualization
    private NetworkVariable<bool> isBlinking = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Color originalColor;
    [SerializeField] private float blinkSpeed = 5f;
    [SerializeField] private float normalSpeed = 5f;
    [SerializeField] private float blinkingSpeed = 8f;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        if (renderer != null) {
            originalColor = renderer.material.color;
        }

        if (agent != null) {
            agent.speed = normalSpeed;
        }

        isBlinking.OnValueChanged += OnBlinkingStateChanged;
    }

    // This function gets called whenever the value of isBlinking changes
    private void OnBlinkingStateChanged(bool oldValue, bool newValue) {
        if (newValue) {
            StartCoroutine(BlinkCoroutine());

            if (IsServer && agent != null) {
                agent.speed = blinkingSpeed;
            }
        }
    }

    private IEnumerator BlinkCoroutine() {
        while (gameObject.activeInHierarchy) {
            if (renderer != null) {
                renderer.material.color = renderer.material.color == Color.white ? originalColor : Color.white;
            }

            yield return new WaitForSeconds(1f / blinkSpeed);
        }
    }

    protected override void OnDamaged(float damage, GameObject damageSource)
    {
        base.OnDamaged(damage, damageSource);
        
        if (!isBlinking.Value && renderer != null)
        {
            originalColor = renderer.material.color;
        }
    }

    #endregion

    protected override void UpdateTarget() {
        if (enemySpawner == null || enemySpawner.playerList == null) return;

        GameObject closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject obj in enemySpawner.playerList) {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < closestDistance) {
                closestPlayer = obj;
                closestDistance = distance;
            }
        }

        target = closestPlayer != null ? closestPlayer.transform : null;
    }

    protected override IEnumerator DelayAttack() {
        isBlinking.Value = true;

        yield return new WaitForSeconds(attackCooldown);
        Attack();
    }

    protected override void Attack() {
        if (!IsServer || _isExploding) return;
        _isExploding = true;

        AttackServerRpc(true);
    }

    public override void OnNetworkDespawn()
    {
        isBlinking.OnValueChanged -= OnBlinkingStateChanged;

        StopAllCoroutines();

        base.OnNetworkDespawn();
    }
}
