using UnityEngine;
using Unity.Netcode;

public class ItemChest_SCRIPT : NetworkBehaviour
{
    // Hold a list of all chest prefabs
    //[SerializeField] private GameObject[] chestPrefabs;

    private PlayerInputHandler _inputHandler;

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
        OpenChestClientRpc();
    }

    [ClientRpc]
    private void OpenChestClientRpc() {
        this.GetComponent<Renderer>().material.color = Color.green;
    }
}
