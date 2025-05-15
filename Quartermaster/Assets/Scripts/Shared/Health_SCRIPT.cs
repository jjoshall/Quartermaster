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

    private bool _wasCritical = false;
    [SerializeField] private FullScreenTestController _damageEffect;
    //bool IsDead;

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        CurrentHealth.Value = MaxHealth;

        if (IsLocalPlayer) {
            CurrentHealth.OnValueChanged += OnHealthChanged;
            CheckCriticalState();
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsLocalPlayer) {
            CurrentHealth.OnValueChanged -= OnHealthChanged;

            if (_damageEffect != null) {
                _damageEffect.SetCriticalState(false);
            }
        }
    }

    private void OnHealthChanged(float oldHealth, float newHealth) {
        if (IsCritical() && _damageEffect != null) {
            StartCoroutine(_damageEffect.Hurt());
        }
    }

    private void CheckCriticalState() {
        bool isCritical = IsCritical(); // health at 20 so true

        // If critical state changed
        if (isCritical != _wasCritical && _damageEffect != null) {
            _wasCritical = isCritical;      // wascrit is now true
            _damageEffect.SetCriticalState(isCritical);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float healAmount) {
        if (!IsServer) return;

        float healthBefore = CurrentHealth.Value;
        CurrentHealth.Value += healAmount;
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value, 0f, MaxHealth);

        // call OnHeal action
        float trueHealAmount = CurrentHealth.Value - healthBefore;
        if (trueHealAmount > 0f) {
            OnHealed?.Invoke(trueHealAmount);
            HealClientRpc(trueHealAmount);
        }
    }

    [ClientRpc]
    private void HealClientRpc(float trueHealAmount) {
        if (IsServer) return;
        OnHealed?.Invoke(trueHealAmount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, NetworkObjectReference damageSourceRef) {
        if (!IsServer || Invincible) return;

        float healthBefore = CurrentHealth.Value;
        CurrentHealth.Value -= damage;
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value, 0f, MaxHealth);
        GameObject damageSource = null;

        if (damageSourceRef.TryGet(out NetworkObject networkObject)) {
            damageSource = networkObject.gameObject;
        }

        // call OnDamage action
        float trueDamageAmount = healthBefore - CurrentHealth.Value;
        if (trueDamageAmount > 0f) {
            OnDamaged?.Invoke(trueDamageAmount, damageSource);
            UpdateClientHealthClientRpc(trueDamageAmount, damageSourceRef);
        }

        HandleDeath();
    }

    [ClientRpc]
    private void UpdateClientHealthClientRpc(float trueDamageAmount, NetworkObjectReference damageSourceRef) {
        if (IsServer) return;

        GameObject damageSource = null;
        if (damageSourceRef.TryGet(out NetworkObject networkObject)) {
            damageSource = networkObject.gameObject;
        }

        OnDamaged?.Invoke(trueDamageAmount, damageSource);
    }

    public void Kill() {
        if (!IsServer) return;

        CurrentHealth.Value = 0f;

        OnDamaged?.Invoke(MaxHealth, null);

        HandleDeath();
    }

    void HandleDeath() {
        if (CurrentHealth.Value <= 0f) {
            CurrentHealth.Value = 2147483000; // added hp buffer for multiplayer
            //IsDead = true;
            OnDie?.Invoke();
            NotifyDeathClientRpc();
        }        
    }

    [ClientRpc]
    private void NotifyDeathClientRpc() {
        if (IsServer) return;
        OnDie?.Invoke();
    }
}