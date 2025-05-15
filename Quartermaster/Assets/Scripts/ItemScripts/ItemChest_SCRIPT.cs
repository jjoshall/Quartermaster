using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ItemChest_SCRIPT : NetworkBehaviour
{
    private PlayerInputHandler _inputHandler;

    private bool _isOpen = false;
    // list of struct for items + quantity
    // private List<ItemStruct> _items = new List<ItemStruct>();
    // Serializeable

    [Serializable]
    public struct itemStruct {
        public GameObject itemPrefab;
        public int quantity;
    }

    [SerializeField] private List<itemStruct> _dropTable = new List<itemStruct>();
    
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
    }

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) {
            return;
        }

        _inputHandler = other.GetComponent<PlayerInputHandler>();
        if (_inputHandler == null) {
            Debug.LogError("PlayerInputHandler not found on player object.");
            return;
        }

        Debug.Log("Player entered chest trigger zone.");

        _inputHandler.OnInteract += OpenChest;

        // Change color of object locally for feedback
        this.GetComponent<Renderer>().material.color = Color.red;

    }

    private void OnTriggerExit(Collider other) {
        if (_inputHandler != null && other.GetComponent<PlayerInputHandler>() == _inputHandler) {
            Debug.Log("Player exited chest trigger zone.");
            _inputHandler.OnInteract -= OpenChest;
            _inputHandler = null;

            // Change color of object back to original
            this.GetComponent<Renderer>().material.color = Color.white;
        }
    }

    private void OpenChest() {
        OpenChestServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenChestServerRpc() {
        Debug.Log("OpenChestServerRpc called.");
        if (_isOpen == false){
            foreach (itemStruct drop in _dropTable) {
                Debug.Log("Dropping item: " + drop.itemPrefab.name + " with quantity: " + drop.quantity);
                ItemManager.instance.DropGivenItemPrefab(drop.itemPrefab, drop.quantity, transform.position);
            }
            _isOpen = true;
        }
        // ItemManager.instance.RollDropTable(transform.position);
        OpenChestClientRpc();
    }

    [ClientRpc]
    private void OpenChestClientRpc() {
        _inputHandler.OnInteract -= OpenChest;
        // Set this gameobject to inactive
        gameObject.SetActive(false);
    }
}
