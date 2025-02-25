using UnityEngine;

public class PlayerInputSetup : MonoBehaviour {
    void Start() {
        // Ensure this is the local player (if you’re using Netcode for GameObjects, you may need to check IsOwner).
        if (!IsLocalPlayer()) return;
        
        // Get settings from the persistent SettingsManager.
        PlayerSettings settings = SettingsManager.Instance.currentSettings;
        ApplySettings(settings);
    }

    bool IsLocalPlayer() {
        // Replace this with your network ownership check, e.g., using NetworkObject.IsOwner.
        return true;
    }

    void ApplySettings(PlayerSettings settings) {
        // Here, update your input actions, character controller, etc.
        // For example, if you have an InputActions component, set the key bindings:
        // myInputActions.SetBinding("moveForward", settings.moveForward);
        // myInputActions.SetBinding("shoot", settings.shoot);
        // Similarly, apply sensitivity, FOV, and volume options.
        Debug.Log("Applying settings: " + settings.moveForward + ", " + settings.shoot);
    }
}
