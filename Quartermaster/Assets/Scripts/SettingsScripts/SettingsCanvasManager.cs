using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SettingsCanvasManager : MonoBehaviour {
    [SerializeField] private Button returnToPrevBtn;

    //mark false in game, true in menu
    [SerializeField] private bool inMainMenu = false;

    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private GameObject mainMenuCanvasPrefab;
    [SerializeField] private Canvas settingsCanvas;

    public TMP_Dropdown resolutionDropdown;
    Resolution[] resolutions;

    private void Awake() {
        if (returnToPrevBtn != null) {

            returnToPrevBtn.onClick.AddListener(ReturnToPrevious);
        }
        else {
            // Debug.LogError("Return button is not assigned in SettingsCanvasManager.");
        }
    }

    void Start() {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> resolutionOptions = new List<string>();

        int currResIndex = 0;
        for (int i = 0; i < resolutions.Length; i++) {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height) {
                currResIndex = i;
            }
        }


        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currResIndex;
        resolutionDropdown.RefreshShownValue();


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

    public void SetQuality(int qualityIndex) {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen) {
        Screen.fullScreen = isFullscreen;
    }

    public void SetResolution(int resolutionIndex) {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

}

