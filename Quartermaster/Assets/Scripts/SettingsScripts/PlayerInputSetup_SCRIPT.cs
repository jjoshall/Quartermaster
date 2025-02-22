using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputSetup : NetworkBehaviour
{
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        if (!IsOwner)
            return;

        PlayerSettings settings = SettingsManager.Instance.currentSettings;
        ApplySettings(settings);
    }

    public void ApplySettings(PlayerSettings settings)
    {
        InputActionAsset actions = playerInput.actions;

        // Note: the new binding string already includes the device type.
        RebindAction(actions, "moveForward", "<Keyboard>/" + settings.moveForward);
        RebindAction(actions, "moveBackward", "<Keyboard>/" + settings.moveBackward);
        RebindAction(actions, "moveLeft", "<Keyboard>/" + settings.moveLeft);
        RebindAction(actions, "moveRight", "<Keyboard>/" + settings.moveRight);

        RebindAction(actions, "sprint", "<Keyboard>/" + settings.sprint);
        RebindAction(actions, "crouch", "<Keyboard>/" + settings.crouch);
        RebindAction(actions, "pickup", "<Keyboard>/" + settings.pickup);
        RebindAction(actions, "drop", "<Keyboard>/" + settings.drop);
        RebindAction(actions, "use", "<Keyboard>/" + settings.use);
        RebindAction(actions, "shoot", "<Mouse>/" + settings.shoot);

        RebindAction(actions, "itemslot1", "<Keyboard>/" + settings.itemslot1);
        RebindAction(actions, "itemslot2", "<Keyboard>/" + settings.itemslot2);
        RebindAction(actions, "itemslot3", "<Keyboard>/" + settings.itemslot3);
        RebindAction(actions, "itemslot4", "<Keyboard>/" + settings.itemslot4);
        RebindAction(actions, "itemscroll", "<Mouse>/" + settings.itemscroll);

        Debug.Log("Applied settings: " +
            settings.moveForward + ", " +
            settings.moveBackward + ", " +
            settings.moveLeft + ", " +
            settings.moveRight + ", " +
            settings.sprint + ", " +
            settings.crouch + ", " +
            settings.pickup + ", " +
            settings.drop + ", " +
            settings.use + ", " +
            settings.shoot + ", " +
            settings.itemslot1 + ", " +
            settings.itemslot2 + ", " +
            settings.itemslot3 + ", " +
            settings.itemslot4 + ", " +
            settings.itemscroll);
    }

    /// <summary>
    /// Finds the binding(s) on the specified action that match the device type inferred from the new binding string,
    /// removes any existing override, and applies the new override.
    /// </summary>
    /// <param name="actions">The InputActionAsset containing the action.</param>
    /// <param name="actionName">The name of the action to rebind.</param>
    /// <param name="newBinding">The new binding string (for example, "<Keyboard>/w").</param>
    void RebindAction(InputActionAsset actions, string actionName, string newBinding)
    {
        InputAction action = actions.FindAction(actionName);
        if (action != null)
        {
            // Determine the device type from the new binding string.
            string deviceType = "";
            if (newBinding.StartsWith("<Keyboard>/"))
                deviceType = "<Keyboard>";
            else if (newBinding.StartsWith("<Mouse>/"))
                deviceType = "<Mouse>";
            else if (newBinding.StartsWith("<Gamepad>/"))
                deviceType = "<Gamepad>";
            else if (newBinding.StartsWith("<XRController>/"))
                deviceType = "<XRController>";

            bool bindingFound = false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                // If a device type was determined, update only the binding that starts with that type.
                if (!string.IsNullOrEmpty(deviceType))
                {
                    if (action.bindings[i].path.StartsWith(deviceType))
                    {
                        // Remove any existing override and apply the new binding.
                        action.RemoveBindingOverride(i);
                        action.ApplyBindingOverride(i, newBinding);
                        Debug.Log($"Rebinding {actionName} (binding index {i}) to {newBinding}");
                        bindingFound = true;
                    }
                }
                else
                {
                    // If no device type could be determined, simply override the first binding.
                    action.RemoveBindingOverride(i);
                    action.ApplyBindingOverride(i, newBinding);
                    Debug.Log($"Rebinding {actionName} (binding index {i}) to {newBinding}");
                    bindingFound = true;
                    break;
                }
            }
            if (!bindingFound)
            {
                Debug.LogWarning($"No binding matching device type {deviceType} found for action {actionName}.");
            }
        }
        else
        {
            Debug.LogWarning($"Action {actionName} not found in InputActionAsset.");
        }
    }
}
