using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyBindChanger : MonoBehaviour, IPointerClickHandler {
    [Header("UI References")]
    public TextMeshProUGUI keyDisplayText;  // The text field showing the current key binding.
    public RawImage keyDisplayBg;

    [Header("Settings")]
    public string currentKeyBinding = "None";

    public delegate void OnKeyBindChanged(string newKey);
    public event OnKeyBindChanged keyBindChangedEvent;

    private string lastSavedBinding = "None"; 

    private bool isWaitingForKey = false;

    private void Awake() {
        currentKeyBinding = keyDisplayText.text;
    }

    // Called when the UI element is clicked.
    public void OnPointerClick(PointerEventData eventData) {
        if (!isWaitingForKey) {
            Debug.Log("Key binding change initiated...");
            StartCoroutine(WaitForKeyPress());
        } else {
            Debug.Log("Already waiting for key press...");
        }
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && !isWaitingForKey) {
            //exit settings menu entirely and return to main menu
            GameObject settingsCanvas = GameObject.Find("SettingsCanvas");
            if (settingsCanvas != null) {
                settingsCanvas.SetActive(false);
            } else {
                Debug.LogWarning("SettingsCanvas not found.");
            }
        }
    }

    private IEnumerator WaitForKeyPress() {
        lastSavedBinding = currentKeyBinding;

        keyDisplayBg.color = new Color32(236, 191, 81, 255);
        isWaitingForKey = true;
        keyDisplayText.text = "...";
        Debug.Log("Waiting for key press...");

        // Wait until any key is pressed.
        while (isWaitingForKey) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Debug.Log("Escape key pressed, cancelling key binding change.");
                keyDisplayText.text = lastSavedBinding;
                isWaitingForKey = false;
                keyDisplayBg.color = Color.white;
                yield break;
            }
            if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape)) {
                Debug.Log("Key detected, determining which key was pressed...");

                // Loop through all keycodes and find which one was pressed.
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode))) {
                    if (Input.GetKeyDown(keyCode)) {
                        currentKeyBinding = keyCode.ToString();
                        keyDisplayText.text = currentKeyBinding;
                        isWaitingForKey = false;

                        keyDisplayBg.color = Color.white;

                        Debug.Log($"New key binding set: {currentKeyBinding}");

                        // Call your update method via an event or directly.
                        keyBindChangedEvent?.Invoke(currentKeyBinding);
                        yield break;
                    }
                }
            }
            yield return null;
        }
    }
}
