using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class SettingsCanvasManager : MonoBehaviour {

    [SerializeField] private Button returnToPrevBtn;

    private void Awake() {
        //when button clicked disable canvas
        returnToPrevBtn.onClick.AddListener(() => {
            gameObject.SetActive(false);
            //if local scene is not main menu, re-enable cursor
            if (SceneManager.GetActiveScene().name != "MainMenu_SCENE") {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            PlayerController localPlayer = FindLocalPlayer();
            if (localPlayer != null) {
                localPlayer.ReloadInputActions();
            }
        });
    } 

    private PlayerController FindLocalPlayer() {
        // Finds the first PlayerController where IsOwner is true.
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach(var player in players) {
            if (player.IsOwner) {
                return player;
            }
        }
        return null;
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
