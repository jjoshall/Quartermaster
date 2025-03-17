using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PocketInventoryPortalKey : InventoryItem {
    [Header("Item Configuration")]
    private float _teleportRadius = 20.0f;


    [Header("Backing Fields")]
    // private int _id = 0;
    private int _pocketInventoryQuantity = 0;
    private float _lastUsedTime = float.MinValue;
    private static float _itemCooldown = 10f;

    [Header("Abstract Overrides")]
    public override float cooldown {
        get => _itemCooldown;
        set => _itemCooldown = value;
    }

    public override int itemID {
        get => _id;
        set => _id = value;
    }

    public override int quantity {
        get => _pocketInventoryQuantity;
        set => _pocketInventoryQuantity = value;
    }


    public override bool IsWeapon(){
        return false;
    }

    // Override methods (used as "static fields" for subclass)
    public override bool IsConsumable() {
        return false;
    }

    public override int StackLimit() {
        return 1;
    }

    public override void InitializeFromGameManager()
    {
        _teleportRadius = GameManager.instance.PortalKey_TeleportRadius;
        _itemCooldown = GameManager.instance.PortalKey_Cooldown;
    }

    public override void Use(GameObject user , bool isHeld) {
        NetworkObject n_user = user.GetComponent<NetworkObject>();

        if (isHeld) return; // TEMPORARY. disable if it is a held trigger. so we don't immediately re-teleport.
        
        if (PocketInventory.instance.PlayerIsInPocket(n_user)){
            PocketInventory.instance.ReturnAllPlayersServerRpc();
            return;
        }
        // PocketInventory.instance.ReturnIfInPocketServerRpc(n_user); // bypass cooldown. async.

        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (lastUsed + cooldown > Time.time) {
            Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }

        Debug.Log(itemStr + " (" + itemID + ") used");
    
        if (IsConsumable()) {
            quantity--;
        }

        lastUsed = Time.time;

        List<GameObject> nearbyPlayers;
        // use physics overlap sphere with radius _TELEPORT_RADIUS to grab nearby players
        nearbyPlayers = GetNearbyPlayers(user, _teleportRadius);
        Debug.Log ("Nearby players to teleport: " + nearbyPlayers.Count);
        foreach (GameObject player in nearbyPlayers) {
            TeleportPlayer(player);
        }
        // TeleportPlayer(user);

    }

    private List<GameObject> GetNearbyPlayers (GameObject user, float radius) {
        List<GameObject> nearbyPlayers = new List<GameObject>();
        Collider[] colliders = Physics.OverlapSphere(user.transform.position, radius);
        foreach (Collider col in colliders) {
            if (col.gameObject.tag == "Player") {
                Debug.Log ("Player detected: " + col.gameObject);
                nearbyPlayers.Add(col.gameObject);
            }
        }
        return nearbyPlayers;
    }

    private void TeleportPlayer(GameObject user) {
        PocketInventory.instance.TeleportToPocketServerRpc(user);
    }

}
