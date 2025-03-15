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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
}
