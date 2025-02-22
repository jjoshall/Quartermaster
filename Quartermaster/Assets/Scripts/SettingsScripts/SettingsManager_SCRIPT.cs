using UnityEngine;
using System.IO;

public class SettingsManager : MonoBehaviour {
    public static SettingsManager Instance { get; private set; }

    public PlayerSettings currentSettings;

    private string filePath;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            filePath = Path.Combine(Application.persistentDataPath, "playerSettings.json");
            LoadSettings();
        } else {
            Destroy(gameObject);
        }
    }

    public void LoadSettings() {
        if (File.Exists(filePath)) {
            string json = File.ReadAllText(filePath);
            currentSettings = JsonUtility.FromJson<PlayerSettings>(json);
        } else {
            // Use default settings if no saved file exists.
            currentSettings = new PlayerSettings();
            SaveSettings();
        }
    }

    public void SaveSettings() {
        string json = JsonUtility.ToJson(currentSettings, true);
        File.WriteAllText(filePath, json);
    }

    // A helper method to update a binding by its name
    public void UpdateKeyBinding(string bindingName, string newKey) {
        switch(bindingName) {
            case "moveForward": currentSettings.moveForward = newKey; break;
            case "moveBackward": currentSettings.moveBackward = newKey; break;
            case "moveLeft": currentSettings.moveLeft = newKey; break;
            case "moveRight": currentSettings.moveRight = newKey; break;
            case "sprint": currentSettings.sprint = newKey; break;
            case "crouch": currentSettings.crouch = newKey; break;
            case "pickup": currentSettings.pickup = newKey; break;
            case "drop": currentSettings.drop = newKey; break;
            case "use": currentSettings.use = newKey; break;
            case "shoot": currentSettings.shoot = newKey; break;
            case "itemslot1": currentSettings.itemslot1 = newKey; break;
            case "itemslot2": currentSettings.itemslot2 = newKey; break;
            case "itemslot3": currentSettings.itemslot3 = newKey; break;
            case "itemslot4": currentSettings.itemslot4 = newKey; break;
            case "itemscroll": currentSettings.itemscroll = newKey; break;
            default:
                Debug.LogWarning("Unknown binding: " + bindingName);
                break;
        }
        SaveSettings();
    }
}
