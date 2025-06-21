// Code inspired from Unity6 FPS Template

using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class Health : NetworkBehaviour {
    [Tooltip("Maximum amount of health")] 
    public float MaxHealth = 10f;

    [Tooltip("Health to be considered \"at critical\"")]
    public float CriticalHealthRatio = 0.3f;

    [Header("Hovering Health Bar")]
    [SerializeField] private GameObject hoveringHealthBar;  // not sure what this is for - norman
    [SerializeField] private Image fillImage;               // hp bar canvas (floating above players heads)
    [SerializeField] private TextMeshProUGUI lives;
    public NetworkVariable<float> healthRatio = new NetworkVariable<float>(
    1.0f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner);

    public UnityAction<float, GameObject> OnDamaged;
    public UnityAction<float> OnHealed;
    public UnityAction OnDie;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>();
    public NetworkVariable<bool> Invincible = new NetworkVariable<bool>(false);
    public bool CanPickup() => CurrentHealth.Value < MaxHealth;

    public float GetRatio() => CurrentHealth.Value / MaxHealth;
    public bool IsCritical() => GetRatio() <= CriticalHealthRatio;

    private bool _wasCritical = false;
    //bool IsDead;

    public override void OnNetworkSpawn()
    {
        if (IsServer) {
            CurrentHealth.Value = MaxHealth;
        }
        
        if (IsLocalPlayer)
        {
            CheckCriticalState();
        }

        if (IsPlayer() && !IsLocalPlayer) {
            CurrentHealth.OnValueChanged += UpdateHealthBarFill;
        }
    }

    public bool IsPlayer()
    {
        return gameObject.CompareTag("Player") ? true : false;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();


        if (IsPlayer() && !IsLocalPlayer) {
            CurrentHealth.OnValueChanged -= UpdateHealthBarFill;
        }
    }

    private void UpdateHealthBarFill(float oldHealth, float newHealth)
    {
        if (fillImage == null)
        {
            Debug.LogError("FillImage is null. This object is: " + gameObject.name);
            return;
        }
        fillImage.fillAmount = GetRatio();
    }

    private void CheckCriticalState()
    {
        bool isCritical = IsCritical(); // health at 20 so true

        // If critical state changed
        if (isCritical != _wasCritical)
        {
            _wasCritical = isCritical;      // wascrit is now true
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetInvincibleServerRpc(bool state) {
        if (!IsServer) return;

        if (fillImage) {
            fillImage.color = state ? Color.black : Color.red;
        }
        Invincible.Value = state;
        SetInvincibleClientRpc(state);
    }
    
    [ClientRpc]
    public void SetInvincibleClientRpc(bool state) {
        if (IsServer) return;

        if (fillImage) {
            fillImage.color = state ? Color.black : Color.red;
        }
        Invincible.Value = state;
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
        if (!IsServer || Invincible.Value) return;

        float healthBefore = CurrentHealth.Value;
        CurrentHealth.Value -= damage;
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value, 0f, MaxHealth);
        GameObject damageSource = null;

        if (damageSourceRef.TryGet(out NetworkObject networkObject)) {
            damageSource = networkObject.gameObject;
        }

        // call OnDamage action
        float trueDamageAmount = healthBefore - CurrentHealth.Value;
        // Debug.Log ("TrueDamageAmount: " + trueDamageAmount + " DamageSource: " + damageSource);
        if (damage > 0f)
        {
            if (damage > 99999f)
            {
                Debug.Log ("damage: " + damage + " trueDamageAmount: " + trueDamageAmount + " damageSource: " + damageSource);
            }
            OnDamaged?.Invoke(damage, damageSource);
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
            CurrentHealth.Value = 2147483000; // DEPRECATED? no time to test taking itout.
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