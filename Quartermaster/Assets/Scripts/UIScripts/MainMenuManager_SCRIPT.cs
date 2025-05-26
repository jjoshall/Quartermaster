using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuManager : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsCanvasPrefab;
    [SerializeField] private GameObject tutorialCanvasPrefab;

    private void Start() {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnPlayButtonClicked() {
        SceneManager.LoadScene("Level Design_SCENE");
    }

    private void OnTutorialButtonClicked() {
        Debug.Log("going to tutorial");
        tutorialCanvasPrefab.SetActive(true);
    }

    private void OnSettingsButtonClicked() {
        Debug.Log("going to settings");
        settingsCanvasPrefab.SetActive(true);
    }

    private void OnQuitButtonClicked() {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
