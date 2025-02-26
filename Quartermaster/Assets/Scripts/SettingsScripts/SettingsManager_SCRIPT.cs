using UnityEngine;
using System.IO;

public class SettingsManager : MonoBehaviour {
    public static SettingsManager Instance;

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

    public void UpdateKeyBinding(string actionName, string newBinding) {
        // Use reflection or a switch to update the correct binding.
        // For simplicity, here's an example using a switch statement.
        switch (actionName) {
            case "moveForward":
                currentSettings.moveForward = newBinding;
                break;
            case "moveBackward":
                currentSettings.moveBackward = newBinding;
                break;
            // Add cases for all your actions...
            case "shoot":
                currentSettings.shoot = newBinding;
                break;
            // etc.
        }
        SaveSettings();
    }
}
