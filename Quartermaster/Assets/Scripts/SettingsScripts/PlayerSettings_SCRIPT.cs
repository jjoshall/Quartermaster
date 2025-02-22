using System;

[Serializable]
public class PlayerSettings {
    // Movement Bindings
    public string moveForward = "W";
    public string moveBackward = "S";
    public string moveLeft = "A";
    public string moveRight = "D";

    // Action Bindings
    public string sprint = "LeftShift";
    public string crouch = "LeftControl";
    public string pickup = "E";
    public string drop = "G";
    public string use = "F";
    public string shoot = "Mouse0";

    // Inventory Bindings
    public string itemslot1 = "Alpha1";
    public string itemslot2 = "Alpha2";
    public string itemslot3 = "Alpha3";
    public string itemslot4 = "Alpha4";
    public string itemscroll = "Mouse ScrollWheel";

    // Additional Options
    public float mouseSensitivity = 1.0f;
    public float masterVolume = 1.0f;
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;
    public float fov = 60.0f;
}
