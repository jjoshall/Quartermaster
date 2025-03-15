using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SettingsCanvasManager : MonoBehaviour
{
    [SerializeField] private Button returnToPrevBtn;
    
    [SerializeField] private bool isMainMenuContext = false;
    
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

    private void ReturnToPrevious()
    {
        if (isMainMenuContext)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene("MainMenu_SCENE");
        }
        else
        {
            if (pauseCanvas != null)
            {
                pauseCanvas.gameObject.SetActive(true);
            }
            gameObject.SetActive(false);
        }
    }

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

