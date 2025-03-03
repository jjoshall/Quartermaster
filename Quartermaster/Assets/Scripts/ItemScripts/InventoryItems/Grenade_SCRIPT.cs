using UnityEngine;

public class Grenade : InventoryItem {
    // Backing fields
    private int _id = 0;
    private int _grenadeQuantity = 0;
    private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 2f;

    public override bool isHoldable { get; set; } = true;

    private GameObject _lineRenderer;

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

    // Runtime field
    private bool _grenadeIsCharging = false;
    public float grenadeBaseVelocity = 20f;
    public float grenadeMaxVelocity = 40f;
    public float grenadeMaxChargeTime = 2.5f;
    private float _grenadeVelocity = 0f;

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
        // Initialize the charging state.
        _grenadeIsCharging = true;
        _grenadeVelocity = grenadeBaseVelocity;
        // Initialize the line renderer.
        Vector3 position = user.transform.position;
        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        ProjectileManager.instance.SpawnLineRenderer(camera, _grenadeVelocity);
    }

    private void Charging (GameObject user){
        Debug.Log ("charging grenade called");
        // Continue charging the grenade.
        // Update the line renderer.
        // Update the velocity if not maxed.
        _grenadeVelocity = Mathf.Min(_grenadeVelocity + Time.deltaTime * 10, grenadeMaxVelocity);
        UpdateLineRenderer(user);
    }

    private void UpdateLineRenderer (GameObject user){
        Vector3 position = user.transform.position;
        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        ProjectileManager.instance.UpdateLineRenderer(camera, _grenadeVelocity);
    }

    public override void Release (GameObject user){
        Debug.Log ("release grenade called");
        Transform camera = user.GetComponent<Inventory>().orientation;
        Vector3 direction = camera.forward;
        // Throw the grenade.
        ProjectileManager.instance.SpawnSelfThenAll("Grenade", user.transform.position, user.transform.rotation, direction, _grenadeVelocity);
        quantity--;
        lastUsed = Time.time;
        ProjectileManager.instance.DestroyLineRenderer();
        _grenadeVelocity = grenadeBaseVelocity;
        _grenadeIsCharging = false;
    }

    // Note: Do collision 

}
