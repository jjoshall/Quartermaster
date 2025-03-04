using UnityEngine;

public class Grenade : InventoryItem {

    // Adjustable fields.
    private float _grenadeBaseVelocity = 5f;
    private float _grenadeMaxVelocity = 30f;
    private float _grenadeMaxChargeTime = 1.0f;

    // Runtime field. DONT CHANGE.
    private bool _grenadeIsCharging = false;
    private float _grenadeChargeTime = 0f;
    private float _grenadeVelocity = 0f;


    // Backing fields. DONT CHANGE.
    private int _id = 0;
    private int _grenadeQuantity = 0;
    private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 3f;
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
        get => _grenadeQuantity;
        set => _grenadeQuantity = value;
    }

    public override float lastUsed {
        get => _lastUsedTime;
        set => _lastUsedTime = value;
    }


    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return true;
    }

    public override int StackLimit() {
        return 5;
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

        if (isHeld == false){ // This is our KeyDown event.
            StartCharging(user);
        }
        else {
            Charging(user);
        }

    }

    private void StartCharging (GameObject user){
        Debug.Log ("Start charging grenade");
        // Initialize the charging state.
        _grenadeIsCharging = true;
        _grenadeVelocity = _grenadeBaseVelocity;
        // Initialize the line renderer.
        UpdateLineRenderer(user);
    }

    private void Charging(GameObject user)
    {
        Debug.Log("Charging grenade called");

        // Increment the charge time
        _grenadeChargeTime += Time.deltaTime;

        // Calculate the interpolation factor (clamped between 0 and 1)
        float t = Mathf.Clamp01(_grenadeChargeTime / _grenadeMaxChargeTime);

        // Linearly interpolate grenade velocity from base to max over the charge time
        _grenadeVelocity = Mathf.Lerp(_grenadeBaseVelocity, _grenadeMaxVelocity, t);

        // Update the line renderer
        UpdateLineRenderer(user);
    }

    private void UpdateLineRenderer (GameObject user){
        Transform camera = user.GetComponent<Inventory>().orientation;
        ProjectileManager.instance.UpdateLineRenderer(camera, _grenadeVelocity);
    }

    public override void Release (GameObject user){
        Debug.Log ("release grenade called");
        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the grenade.
        ProjectileManager.instance.SpawnSelfThenAll("Grenade", camera.transform.position + camera.right * 0.1f, camera.transform.rotation, direction, _grenadeVelocity, user);
        _grenadeQuantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer();
        _grenadeVelocity = _grenadeBaseVelocity;
        _grenadeIsCharging = false;
        _grenadeChargeTime = 0.0f;
    }

    // Note: Do collision 

}
