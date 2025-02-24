// Code inspired from Unity6 FPS Template

using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System;

public class Health : NetworkBehaviour {
    [Tooltip("Maximum amount of health")] 
    public float MaxHealth = 10f;

    [Tooltip("Health to be considered \"at critical\"")]
    public float CriticalHealthRatio = 0.3f;

    public UnityAction<float, GameObject> OnDamaged;
    public UnityAction<float> OnHealed;
    public UnityAction OnDie;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>();
    public bool Invincible { get; set; }
    public bool CanPickup() => CurrentHealth.Value < MaxHealth;

    public float GetRatio() => CurrentHealth.Value / MaxHealth;
    public bool IsCritical() => GetRatio() <= CriticalHealthRatio;

    bool IsDead;

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        CurrentHealth.Value = MaxHealth;
    }

    public void Heal(float healAmount) {
        if (!IsServer) return;
        
        float healthBefore = CurrentHealth.Value;
        CurrentHealth.Value += healAmount;
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value, 0f, MaxHealth);

        // call OnHeal action
        float trueHealAmount = CurrentHealth.Value - healthBefore;
        if (trueHealAmount > 0f) {
            OnHealed?.Invoke(trueHealAmount);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, NetworkObjectReference damageSourceRef) {
        if (!IsServer || Invincible) return;

        float healthBefore = CurrentHealth.Value;   // healthBefore = 100
        Debug.Log(healthBefore);
        CurrentHealth.Value -= damage;  // Damage = 10, CurrentHealth = 100 - 10 = 90
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value, 0f, MaxHealth);
        GameObject damageSource = null;

        if (damageSourceRef.TryGet(out NetworkObject networkObject)) {
            damageSource = networkObject.gameObject;
        }

        // call OnDamage action
        float trueDamageAmount = healthBefore - CurrentHealth.Value;    // 100 - 90 = 10
        Debug.Log("True damage amount: " + trueDamageAmount);   // 10
        if (trueDamageAmount > 0f) {
            Debug.Log("Invoking OnDamaged");
            OnDamaged?.Invoke(trueDamageAmount, damageSource);  // 10, enemy

            Debug.Log("Calling UpdateClientHealthClientRpc with CurrentHealth: " + CurrentHealth.Value);
            UpdateClientHealthClientRpc(trueDamageAmount, damageSourceRef);    // 10, enemy
        }

        HandleDeath();
    }

    [ClientRpc]
    private void UpdateClientHealthClientRpc(float trueDamageAmount, NetworkObjectReference damageSourceRef) {
        if (IsServer) return;

        Debug.Log("Calling UpdateClientHealthClientRpc with true damage: " + trueDamageAmount);

        GameObject damageSource = null;
        if (damageSourceRef.TryGet(out NetworkObject networkObject)) {
            damageSource = networkObject.gameObject;
            Debug.Log("Damage source: " + damageSource);
        }

        Debug.Log("Invoking OnDamaged");
        OnDamaged?.Invoke(trueDamageAmount, damageSource);
    }

    public void Kill() {
        if (!IsServer) return;

        CurrentHealth.Value = 0f;

        OnDamaged?.Invoke(MaxHealth, null);

        HandleDeath();
    }

    void HandleDeath() {
        /*if (IsDead)
            return;*/

        if (CurrentHealth.Value <= 0f) {
            IsDead = true;
            OnDie?.Invoke();
        }
    }
}