using UnityEngine;
using Unity.Netcode;

public class ItemChest_SCRIPT : NetworkBehaviour
{
    // Hold a list of all chest prefabs
    //[SerializeField] private GameObject[] chestPrefabs;

    private PlayerInputHandler _inputHandler;

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            return;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer) {
            return;
        }
        
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
    }

    private void OnTriggerExit(Collider other) {
        if (!IsServer) {
            return;
        }

        if (_inputHandler != null && other.GetComponent<PlayerInputHandler>() == _inputHandler) {
            Debug.Log("Player exited chest trigger zone.");
            _inputHandler.OnInteract -= OpenChest;
            _inputHandler = null;
        }
    }

    private void OpenChest() {
        Debug.Log("Chest opened.");
    }
}
