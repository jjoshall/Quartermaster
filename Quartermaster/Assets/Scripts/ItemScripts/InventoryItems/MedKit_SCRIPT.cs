using UnityEngine;

public class MedKit : InventoryItem {
    // Backing fields
    // private int _id = 0;
    private int _medkitQuantity = 0;
    // private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 0.1f;
    private int _stackLimit = 1;
    private float _medKitBaseVelocity = 1f;
    private float _medKitMaxVelocity = 10f;
    private float _medKitMaxChargeTime = 1.0f;
    private float _medKitTapThreshold = 0.1f;

    // runtime
    private float _medKitChargeTime = 0f;
    private float _medKitVelocity = 0f;
    private float _medKitTapTime = 0.0f;
    private bool _medKitTapped = false;

    // pre-charge threshold.

    private float _healAmount = 0.0f;

    public override bool isHoldable { get; set; } = true;

    // Abstract overrides
    public override float cooldown {
        get => _itemCooldown;
        set => _itemCooldown = value;
    }
    public override int itemID {
        get => _id;
        set => _id = value;
    }

    public override int quantity {
        get => _medkitQuantity;
        set => _medkitQuantity = value;
    }

    public override void InitializeFromGameManager()
    {
        _itemCooldown = GameManager.instance.MedKit_Cooldown;
        _healAmount = GameManager.instance.MedKit_HealAmount;
        _stackLimit = GameManager.instance.MedKit_StackLimit;
        _medKitBaseVelocity = GameManager.instance.MedKit_MinVelocity;
        _medKitMaxVelocity = GameManager.instance.MedKit_MaxVelocity;
        _medKitMaxChargeTime = GameManager.instance.MedKit_ChargeTime;
        _medKitTapThreshold = GameManager.instance.MedKit_TapThreshold;

    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return true;
    }

    public override int StackLimit() {
        return GameManager.instance.MedKit_StackLimit;
    }

    public override bool IsWeapon() {
        return false;
    }


    public override void Use(GameObject user, bool isHeld) {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        // First, check healSpec level.
        int healSpec = user.GetComponent<PlayerStatus>().GetHealSpecLvl();


        if (lastUsed + cooldown > Time.time && healSpec == 0) { // No cooldown for healSpec
            Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }

        Debug.Log(itemStr + " (" + itemID + ") used");

        Debug.Log ("healspec lvl is: " + healSpec);
        if (healSpec == 0) { // if no healSpec, only use.
            ImmediateMedKitUsage(user);
        } else { // if healSpec, allow holding to charge throwable.
            Debug.Log("MedKit: Use: HealSpec level is " + healSpec);


            // If the tap threshold is reached, start charging
            if (isHeld == false) {
                StartCharging(user);
            } else {
                Charging(user);
            }
        }
    

    }


    
    private void StartCharging (GameObject user){
        // Initialize the charging state.
        _medKitVelocity = _medKitBaseVelocity;
        _medKitChargeTime = 0.0f;
        _medKitTapTime = Time.time;
        _medKitTapped = true;
        Debug.Log ("TapTime Set to " + _medKitTapTime);

        // Initialize the line renderer.
        // UpdateLineRenderer(user);
    }

    private void Charging(GameObject user)
    {
        if (!_medKitTapped){
            _medKitVelocity = _medKitBaseVelocity;
            _medKitChargeTime = 0.0f;
            _medKitTapTime = Time.time;
            _medKitTapped = true;
            Debug.Log ("TapTime Set to " + _medKitTapTime);
            return;
        }
        if (Time.time < _medKitTapTime + _medKitTapThreshold) {
            return;
        }
        // Increment the charge time
        _medKitChargeTime += Time.deltaTime;

        // Calculate the interpolation factor (clamped between 0 and 1)
        float t = Mathf.Clamp01(_medKitChargeTime / _medKitMaxChargeTime);

        // Linearly interpolate medKit velocity from base to max over the charge time
        _medKitVelocity = Mathf.Lerp(_medKitBaseVelocity, _medKitMaxVelocity, t);

        // Update the line renderer
        UpdateLineRenderer(user);
    }

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _medKitVelocity);
    }

    public override void Release (GameObject user){

        int healSpec = user.GetComponent<PlayerStatus>().GetHealSpecLvl();
        if (healSpec == 0){
            return;
        }

        // If the tap threshold is not reached, use the item instantly
        if (Time.time < _medKitTapTime + _medKitTapThreshold){ 
            ImmediateMedKitUsage(user);
            return;
        }

        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the medKit.
        ProjectileManager.instance.SpawnSelfThenAll("MedKit", 
                camera.transform.position + camera.right * 0.1f, 
                camera.transform.rotation, 
                direction, 
                _medKitVelocity, 
                user);
        _medkitQuantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer();
        _medKitVelocity = _medKitBaseVelocity;
        _medKitChargeTime = 0.0f;
        _medKitTapped = false;
    }

    private void ImmediateMedKitUsage (GameObject user){
        quantity--;

        lastUsed = Time.time;
        // user.GetComponent<PlayerHealth>().Heal(HEAL_AMOUNT);
        // What handles health now?
        // Generate a quaternion for the particle effect to have no rotation
        Health hp = user.GetComponent<Health>();
        if (hp == null) {
            Debug.LogError("MedKit: ItemEffect: No Health component found on user.");
            return;
        }

        int healSpec = user.GetComponent<PlayerStatus>().GetHealSpecLvl();
        float bonusPerSpec = GameManager.instance.HealSpec_MultiplierPer;
        float total = bonusPerSpec * healSpec + 1.0f;
        float totalHeal = _healAmount * total;

        hp.HealServerRpc(totalHeal);
        ParticleManager.instance.SpawnSelfThenAll("Healing", user.transform.position, Quaternion.Euler(-90, 0, 0));

        _medKitTapTime = 0.0f; // Reset the tap timer
        _medKitTapped = false;
    }

}
