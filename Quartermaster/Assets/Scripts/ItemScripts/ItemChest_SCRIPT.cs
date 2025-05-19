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

    // [Serializable]
    // public struct itemStruct
    // {
    //     public GameObject itemPrefab;
    //     public int quantity;
    // }

    [SerializeField] private List<itemStruct> _dropTable = new List<itemStruct>();      // should be server side only for run-time spawned chests.

    public void InitDropTable(List<itemStruct> dropTable)
    {
        if (!IsServer) {
            Debug.LogError("InitDropTable: This function should only be called on the server.");
            return;
        }
        _dropTable = dropTable;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _inputHandler = other.GetComponent<PlayerInputHandler>();
        if (_inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler not found on player object.");
            return;
        }

        Debug.Log("Player entered chest trigger zone.");

        _inputHandler.OnInteract += OpenChest;

        // Change color of object locally for feedback
        this.GetComponent<Renderer>().material.color = Color.red;

    }

    private void OnTriggerExit(Collider other)
    {
        if (_inputHandler != null && other.GetComponent<PlayerInputHandler>() == _inputHandler)
        {
            Debug.Log("Player exited chest trigger zone.");
            _inputHandler.OnInteract -= OpenChest;
            _inputHandler = null;

            // Change color of object back to original
            this.GetComponent<Renderer>().material.color = Color.white;
        }
    }

    private void OpenChest()
    {
        OpenChestServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenChestServerRpc()
    {
        Debug.Log("OpenChestServerRpc called.");
        if (_isOpen == false)
        {
            foreach (itemStruct drop in _dropTable)
            {
                Debug.Log("Dropping item: " + drop.itemPrefab.name + " with quantity: " + drop.quantity);
                DropGivenItemPrefab(drop.itemPrefab, drop.quantity, transform.position); 
            }
            _isOpen = true;
        }
        // ItemManager.instance.RollDropTable(transform.position);
        OpenChestClientRpc();
    }

    [ClientRpc]
    private void OpenChestClientRpc()
    {
        _inputHandler.OnInteract -= OpenChest;
        // Set this gameobject to inactive
        gameObject.SetActive(false);
    }
    
    public void DropGivenItemPrefab(GameObject itemPrefab, int quantity, Vector3 position){
        if (!IsServer){
            Debug.LogError("DropGivenItemPrefab: This function should only be called on the server.");
            return;
        }
        Vector3 spawnLoc = new Vector3(position.x, position.y + 1.0f, position.z);
        
        GameObject newItem = Instantiate(itemPrefab, spawnLoc, Quaternion.identity);
        if (newItem.GetComponent<Item>() != null){
            if (newItem.GetComponent<Item>().uniqueID == "portalkey"){
                // find all portal keys in scene
                int portalKeyCount = 0;
                foreach (var item in GameObject.FindGameObjectsWithTag("Item")){
                    if (item.GetComponent<Item>().uniqueID == "portalkey"){
                        portalKeyCount++;
                        if (portalKeyCount > 1){
                            Destroy(newItem);       // don't make additional portal keys.
                            return;
                        }
                    }
                }
            }
        }
        newItem.GetComponent<Item>().quantity = quantity; // set quantity to the item stack size.
        newItem.GetComponent<Item>().userRef = null; // set user ref to the enemy.
        newItem.GetComponent<Item>().IsPickedUp = false; // set IsPickedUp to false.
        
        NetworkObject n_newItem = newItem.GetComponent<NetworkObject>();
        if (n_newItem == null) {
            Debug.LogError("DropItemServerRpc: The spawned object is missing a NetworkObject component!");
            Destroy(newItem);  // Prevent stray objects in the scene
            return;
        }

        n_newItem.transform.position = spawnLoc;
        Vector3 randomDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
        randomDirection.Normalize();
        n_newItem.GetComponent<Rigidbody>().linearVelocity = randomDirection;
        n_newItem.Spawn(true); // Spawn the object on the network
        Debug.Log("Spawned network object with ID: " + n_newItem.NetworkObjectId);

        // newItem.transform.SetParent(this.gameObject.transform); // Set the parent to this object.
        newItem.GetComponent<Item>().OnSpawn();
    }
}
