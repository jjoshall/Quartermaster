using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SettingsCanvasManager : MonoBehaviour
{
    [SerializeField] private Button returnToPrevBtn;
    
    // Set this flag to true if the settings were opened from the main menu,
    // or false if they were opened in-game (from the pause menu).
    [SerializeField] private bool isMainMenuContext = false;
    
    // For in-game context, assign the PauseCanvas (the one that opened settings).
    [SerializeField] private Canvas pauseCanvas;

    private void Awake()
    {
        if (returnToPrevBtn != null)
        {
            returnToPrevBtn.onClick.AddListener(ReturnToPrevious);
        }
        else
        {
            Debug.LogError("Return button is not assigned in SettingsCanvasManager.");
        }
    }

    /// <summary>
    /// Called when the Return button is clicked.
    /// Depending on the context, either loads the Main Menu scene or
    /// hides the settings canvas and re-displays the pause canvas.
    /// </summary>
    private void ReturnToPrevious()
    {
        if (isMainMenuContext)
        {
            // For main menu context, return to the main menu scene.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene("MainMenu_SCENE");
        }
        else
        {
            // For in-game context, hide the settings canvas and show the pause canvas.
            if (pauseCanvas != null)
            {
                pauseCanvas.gameObject.SetActive(true);
            }
            gameObject.SetActive(false);
        }
    }

    // Optional: A helper method to add hover effects to a button's text.
    private void AddHoverEffect(Button button)
    {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText == null)
        {
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

    private void ChangeTextColor(TextMeshProUGUI text, Color color)
    {
        text.color = color;
    }
}

