using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SettingsCanvasManager : MonoBehaviour {
    [SerializeField] private Button returnToPrevBtn;

    //mark false in game, true in menu
    [SerializeField] private bool inMainMenu = false;

    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private GameObject mainMenuCanvasPrefab;
    [SerializeField] private Canvas settingsCanvas;

    private void Awake() {
        if (returnToPrevBtn != null) {

            returnToPrevBtn.onClick.AddListener(ReturnToPrevious);
        }
        else {
            // Debug.LogError("Return button is not assigned in SettingsCanvasManager.");
        }
    }

    private void ReturnToPrevious() {
        if (inMainMenu) {
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
            settingsCanvas.gameObject.SetActive(false);
            mainMenuCanvasPrefab.SetActive(true);
            if (pauseCanvas != null) {
                pauseCanvas.gameObject.SetActive(true);
            }
        }
        else {
            if (pauseCanvas != null) {
                settingsCanvas.gameObject.SetActive(false);
                pauseCanvas.gameObject.SetActive(true);
            }
        }
    }

    private void AddHoverEffect(Button button) {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText == null) {
            Debug.LogWarning($"No TextMeshProUGUI found in {button.name}'s children.");
            return;
        }

        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { ChangeTextColor(buttonText, Color.black); });

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { ChangeTextColor(buttonText, Color.white); });

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    private void ChangeTextColor(TextMeshProUGUI text, Color color) {
        text.color = color;
    }

}

