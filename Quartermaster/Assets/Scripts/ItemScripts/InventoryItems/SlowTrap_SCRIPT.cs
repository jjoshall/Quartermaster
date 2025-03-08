using UnityEngine;

public class SlowTrap : InventoryItem {
    // Adjustable fields. INITIAL VALUES DEPRECATED
    private float _slowTrapBaseVelocity = 5f;
    private float _slowTrapMaxVelocity = 10f;
    private float _slowTrapMaxChargeTime = 0.5f;

    // Runtime field. DONT CHANGE.
    private float _slowTrapChargeTime = 0f;
    private float _slowTrapVelocity = 0f;
    
    private float _slowPercentage = 0.0f; // enemy movespeed *= 1 - slowPercentage
                                          // spawned slowtrap prefab should inherit this value
    public override void InitializeFromGameManager()
    {
        _itemCooldown = GameManager.instance.SlowTrap_Cooldown;
        _slowPercentage = GameManager.instance.SlowTrap_SlowByPct;
        _slowTrapBaseVelocity = GameManager.instance.SlowTrap_MinVelocity;
        _slowTrapMaxVelocity = GameManager.instance.SlowTrap_MaxVelocity;
        _slowTrapMaxChargeTime = GameManager.instance.SlowTrap_ChargeTime;
    }


    private int _slowTrapQuantity = 0;
    private static float _itemCooldown = 10f;
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
        get => _slowTrapQuantity;
        set => _slowTrapQuantity = value;
    }


    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return true;
    }

    public override int StackLimit() {
        return GameManager.instance.SlowTrap_StackLimit;
    }

    public override bool IsWeapon() {
        return false;
    }


    public override void Use(GameObject user, bool isHeld) {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;

        if (lastUsed + cooldown > Time.time) {
            Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }

        if (isHeld == false){
            StartCharging(user);
        } else {
            Charging(user);
        }

    }

    private void StartCharging (GameObject user){
        Debug.Log ("Start charging slowtrap");
        // Initialize the charging state.
        _slowTrapVelocity = _slowTrapBaseVelocity;
        // Initialize the line renderer.
        UpdateLineRenderer(user);
    }

        private void Charging(GameObject user)
    {
        Debug.Log("Charging slowtrap called");

        // Increment the charge time
        _slowTrapChargeTime += Time.deltaTime;

        // Calculate the interpolation factor (clamped between 0 and 1)
        float t = Mathf.Clamp01(_slowTrapChargeTime / _slowTrapMaxChargeTime);

        // Linearly interpolate grenade velocity from base to max over the charge time
        _slowTrapVelocity = Mathf.Lerp(_slowTrapBaseVelocity, _slowTrapMaxVelocity, t);

        // Update the line renderer
        UpdateLineRenderer(user);
    }

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _slowTrapVelocity);
    }

    public override void Release (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the grenade.
        ProjectileManager.instance.SpawnSelfThenAll("SlowTrap", 
                camera.transform.position + camera.right * 0.1f, 
                camera.transform.rotation, direction, 
                _slowTrapVelocity, 
                user);
        _slowTrapQuantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer();
        _slowTrapVelocity = _slowTrapBaseVelocity;
        _slowTrapChargeTime = 0.0f;
    }

}
