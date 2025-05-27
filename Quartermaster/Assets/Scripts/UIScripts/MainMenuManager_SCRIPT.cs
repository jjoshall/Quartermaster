using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuManager : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button compendiumButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject mainMenuCanvasPrefab;
    [SerializeField] private GameObject settingsCanvasPrefab;
    [SerializeField] private GameObject compendiumCanvasPrefab;
    [SerializeField] private GameObject tutorialCanvasPrefab;

    private void Start() {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        compendiumButton.onClick.AddListener(OnCompendiumButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        tutorialButton.onClick.AddListener(OnTutorialButtonClicked);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnPlayButtonClicked() {
        SceneManager.LoadScene("Level Design_SCENE");
    }

    private void OnTutorialButtonClicked() {
        SceneManager.LoadScene("Tutorial_SCENE");
    }

    private void OnCompendiumButtonClicked() {
        compendiumCanvasPrefab.SetActive(true);
        mainMenuCanvasPrefab.SetActive(false);
    }

    private void OnSettingsButtonClicked() {
        settingsCanvasPrefab.SetActive(true);
        mainMenuCanvasPrefab.SetActive(false);
    }

    private void OnQuitButtonClicked() {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
