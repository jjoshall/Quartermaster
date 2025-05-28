using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuManager : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button guidebookButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject mainMenuCanvasPrefab;
    [SerializeField] private GameObject settingsCanvasPrefab;
    [SerializeField] private GameObject guidebookCanvasPrefab;
    [SerializeField] private GameObject tutorialCanvasPrefab;

    private void Start() {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        guidebookButton.onClick.AddListener(OnGuidebookButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnPlayButtonClicked() {
        SceneManager.LoadScene("Level Design_SCENE");
    }

    private void OnGuidebookButtonClicked() {
        guidebookCanvasPrefab.SetActive(true);
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
