using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseMenuToggler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas pauseCanvas;      // Your PauseMenu Canvas
    [SerializeField] private Canvas playerUICanvas;     // The main Player UI Canvas
    [SerializeField] private Canvas settingsCanvas;     // The Settings Canvas

    private bool isPauseCanvasActive = false;
    public static bool IsPaused { get; private set; } = false;

    private void Start()
    {
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerUICanvas != null && playerUICanvas.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
                if (settingsCanvas) {
                    settingsCanvas.gameObject.SetActive(false);
                }
            }
        }
    }

    private void TogglePauseMenu()
    {
        isPauseCanvasActive = !isPauseCanvasActive;
        IsPaused = isPauseCanvasActive;

        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(isPauseCanvasActive);

        if (isPauseCanvasActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // No longer resetting movement here.
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ReturnToGame()
    {
        isPauseCanvasActive = false;
        IsPaused = false;
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoToMainMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("NetworkManager shut down.");
        }

        isPauseCanvasActive = false;
        IsPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu_SCENE");
    }

    public void OpenSettingsCanvas()
    {
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(true);

        IsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
