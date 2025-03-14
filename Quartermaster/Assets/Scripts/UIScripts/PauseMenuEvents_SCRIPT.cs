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
    // Expose a global flag that other scripts can check.
    public static bool IsPaused { get; private set; } = false;

    private void Start()
    {
        // Ensure the pause canvas is hidden at startup.
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Only allow toggling the pause menu if the player UI canvas is active.
        if (playerUICanvas != null && playerUICanvas.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePauseMenu();
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
            // When pause is active, unlock and show the cursor.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // When resuming, lock and hide the cursor.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Called by the "Return" button to close the pause menu and resume gameplay.
    /// </summary>
    public void ReturnToGame()
    {
        isPauseCanvasActive = false;
        IsPaused = false;
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Called by the "Main Menu" button to load the main menu scene.
    /// </summary>
    public void GoToMainMenu()
    {
        // Shutdown the network instance if it's running.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("NetworkManager shut down.");
        }

        // Unpause the game explicitly.
        isPauseCanvasActive = false;
        IsPaused = false;

        // Reset cursor to the desired state (for main menu usually unlocked).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load the main menu scene.
        SceneManager.LoadScene("MainMenu_SCENE");
    }

    /// <summary>
    /// Called by the "Settings" button in the pause menu to open the settings canvas.
    /// Hides the pause menu and shows the settings canvas.
    /// </summary>
    public void OpenSettingsCanvas()
    {
        // Hide the pause menu.
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        // Show the settings canvas.
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(true);

        // Keep the game paused and the cursor unlocked.
        IsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}






