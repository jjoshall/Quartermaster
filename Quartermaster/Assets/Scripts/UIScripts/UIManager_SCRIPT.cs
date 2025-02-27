using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] private RawImage selectedItemImage;
    private bool isPaused = false;

    [SerializeField] private Canvas settingsCanvas;

    public void SetSelectedItemTexture(Texture texture) {
        if (selectedItemImage != null) {
            selectedItemImage.texture = texture;
        }
    }

    public Texture GetSelectedItemTexture() {
        if (selectedItemImage != null) {
            return selectedItemImage.texture;
        }

        return null;
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
