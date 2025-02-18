using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenuEvents : MonoBehaviour {
     private UIDocument _document;
     private Button _resumeButton;
     private Button _mainMenuButton;
     private bool _isPaused = false;

     private void Awake() {
          // Make sure the scene is not paused when the game starts
          Time.timeScale = 1;

          _document = GetComponent<UIDocument>();

          _resumeButton = _document.rootVisualElement.Q("ResumeButton") as Button;
          _resumeButton.RegisterCallback<ClickEvent>(OnResumeClick);

          _mainMenuButton = _document.rootVisualElement.Q("MainMenuButton") as Button;
          _mainMenuButton.RegisterCallback<ClickEvent>(evt => SceneManager.LoadScene("MainMenu"));

          // Initially hide the pause menu
          _document.rootVisualElement.style.display = DisplayStyle.None;

          // Find the camera movement script

          // Start listening for input
          StartCoroutine(CheckForPauseInput());
     }


     private void OnDisable() {
          _resumeButton.UnregisterCallback<ClickEvent>(OnResumeClick);
     }

     private void OnResumeClick(ClickEvent evt) {
          TogglePauseMenu();
     }

     private void TogglePauseMenu() {
          _isPaused = !_isPaused;
          _document.rootVisualElement.style.display = _isPaused ? DisplayStyle.Flex : DisplayStyle.None;
          Time.timeScale = _isPaused ? 0 : 1;

          // Show/hide cursor
          UnityEngine.Cursor.visible = _isPaused;
          UnityEngine.Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;

          // Enable/disable camera movement
     }

     private System.Collections.IEnumerator CheckForPauseInput() {
          while (true) {
               if (Input.GetKeyDown(KeyCode.P)) {
                    TogglePauseMenu();
               }

               yield return null;
          }
     }
}
