using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyBindChanger : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public TextMeshProUGUI keyDisplayText;  // The text field showing the current key binding.
    public RawImage keyDisplayBg;

    [Header("Settings")]
    public string currentKeyBinding = "None";

    public delegate void OnKeyBindChanged(string newKey);
    public event OnKeyBindChanged keyBindChangedEvent;

    private string lastSavedBinding = "None"; 
    private bool isWaitingForKey = false;

    private void Awake()
    {
        // Initialize our binding text from the TMP text
        currentKeyBinding = keyDisplayText.text;
    }

    // Called when the UI element is clicked.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isWaitingForKey)
        {
            Debug.Log("Key binding change initiated...");
            StartCoroutine(WaitForKeyPress());
        }
        else
        {
            Debug.Log("Already waiting for key press...");
        }
    }

    private IEnumerator WaitForKeyPress()
    {
        lastSavedBinding = currentKeyBinding;
        keyDisplayBg.color = new Color32(236, 191, 81, 255);
        isWaitingForKey = true;
        keyDisplayText.text = "...";
        Debug.Log("Waiting for key press...");

        // Wait until any key is pressed or Esc is pressed to cancel
        while (isWaitingForKey)
        {
            // If we detect Esc, revert to the previous binding and exit
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("Escape key pressed, cancelling key binding change.");
                keyDisplayText.text = lastSavedBinding;
                isWaitingForKey = false;
                keyDisplayBg.color = Color.white;
                yield break;
            }

            // If any other key is pressed, record the new binding
            if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        currentKeyBinding = keyCode.ToString();
                        keyDisplayText.text = currentKeyBinding;
                        isWaitingForKey = false;

                        keyDisplayBg.color = Color.white;
                        Debug.Log($"New key binding set: {currentKeyBinding}");

                        // Fire the event if anything else needs to know
                        keyBindChangedEvent?.Invoke(currentKeyBinding);
                        yield break;
                    }
                }
            }
            yield return null;
        }
    }
}
