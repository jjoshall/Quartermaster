using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] private RawImage selectedItemImage;

    [SerializeField] private GameObject settingsCanvas;

    public void SetSelectedItemTexture(Texture texture) {
        if (selectedItemImage != null)
        {
            selectedItemImage.texture = texture;
        }
    }

    public Texture GetSelectedItemTexture()
    {
        if (selectedItemImage != null)
        {
            return selectedItemImage.texture;
        }
        return null;
    }

    void Update() {
        // if esc pressed open settings menu
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (settingsCanvas != null) {
                settingsCanvas.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } else {
                Debug.LogWarning("SettingsCanvas not found.");
            }
        }
    }
}
