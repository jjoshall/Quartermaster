using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuManager : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsCanvasPrefab;

    private void Start() {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);

        AddHoverEffect(playButton);
        AddHoverEffect(settingsButton);
        AddHoverEffect(quitButton);
    }

    private void OnPlayButtonClicked() {
        SceneManager.LoadScene("Level Design_SCENE");
    }

    private void OnSettingsButtonClicked() {
        settingsCanvasPrefab.SetActive(true);
    }

    private void OnQuitButtonClicked() {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
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
        entryEnter.callback.AddListener((data) => { ChangeTextColor(buttonText, Color.white); });

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { ChangeTextColor(buttonText, Color.black); });

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    private void ChangeTextColor(TextMeshProUGUI text, Color color) {
        text.color = color;
    }
}
