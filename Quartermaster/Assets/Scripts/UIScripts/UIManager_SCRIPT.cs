using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] private RawImage[] inventorySlotImages; // Assign 4 RawImage objects in the Inspector (index 0 = slot 1, index 1 = slot 2, etc.)
    [SerializeField] private float highlightScale = 1.5f;      // Scale multiplier for the highlighted slot
    [SerializeField] private Canvas settingsCanvas;



    public Slider musicSlider;
    public Slider sfxSlider;

    public Slider masterVolSlider;


    private void Start() {
        musicSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetMusicVolume(value));
        sfxSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetSFXVolume(value));

        masterVolSlider.onValueChanged.AddListener((value) => AudioManager.Instance.SetMasterVolume(value));
    }

    // Updates the texture of a specific inventory slot.
    public void SetInventorySlotTexture(int slot, Texture texture) {
        if (inventorySlotImages != null && slot >= 0 && slot < inventorySlotImages.Length) {
            inventorySlotImages[slot].texture = texture;
        }
    }

    // Highlights the specified slot by resetting all scales and enlarging the chosen one.
    public void HighlightSlot(int selectedSlot) {
        if (inventorySlotImages == null) return;
        for (int i = 0; i < inventorySlotImages.Length; i++) {
            inventorySlotImages[i].rectTransform.localScale = Vector3.one;
        }
        if (selectedSlot >= 0 && selectedSlot < inventorySlotImages.Length) {
            inventorySlotImages[selectedSlot].rectTransform.localScale = Vector3.one * highlightScale;
        }
    }

    public void OpenSettingsCanvas() {
        if (!settingsCanvas.gameObject.activeSelf) {
            settingsCanvas.gameObject.SetActive(true);
        }
    }


    void Update() {
    #if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.Escape)) {
        Debug.Log("Escape pressed, opening settings");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OpenSettingsCanvas();
    }
    #endif
}
}






