using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.SocialPlatforms;

public class PauseMenuToggler : NetworkBehaviour {
    [Header("UI References")]
    [SerializeField] private Canvas pauseCanvas;      // Your PauseMenu Canvas
    [SerializeField] private Canvas playerUICanvas;     // The main Player UI Canvas
    [SerializeField] private Canvas settingsCanvas;     // The Settings Canvas
    [SerializeField] private Canvas gameOverCanvas;     // The gameOver Canvas

    [SerializeField] private GameObject PlayerUI;     // The Player UI

    [SerializeField] private GameObject AdditionalInfoPanel;     // Additional Info Panel

    [Header("Player")]
    [Tooltip("Player controller for movement restrictions")]
    private PlayerController localPlayerController;

    private bool isPauseCanvasActive = false;
    public static bool IsPaused { get; set; } = false;

    private void Start() {
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(false);
        if (gameOverCanvas != null)
            gameOverCanvas.gameObject.SetActive(false);
    }

    private void Update() {
        if (playerUICanvas != null && playerUICanvas.gameObject.activeSelf && PlayerUI != null && PlayerUI.gameObject.activeSelf) {

            if (Input.GetKeyDown(KeyCode.Escape)) {
                TogglePauseMenu();
                if (settingsCanvas) {
                    settingsCanvas.gameObject.SetActive(false);
                }
            }
            if (HealthBarUI.instance.livesRemaining <= 0) {
                OpenGameOverCanvas();

            }
            if (AdditionalInfoPanel != null) {
                AdditionalInfoPanel.SetActive(Input.GetKey(KeyCode.Tab));
            }
        }
        else if (PlayerUI != null && !PlayerUI.gameObject.activeSelf) {
            settingsCanvas.gameObject.SetActive(false);
            pauseCanvas.gameObject.SetActive(false);
            if (isPauseCanvasActive) {
                isPauseCanvasActive = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public override void OnNetworkSpawn() {
        if (!IsOwner) {
            enabled = false;
            return;
        }

        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in all) {
            if (pc.IsOwner) {
                localPlayerController = pc;
                break;
            }
        }

        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.gameObject.SetActive(false);
    }

    private void TogglePauseMenu() {
        isPauseCanvasActive = !isPauseCanvasActive;
        IsPaused = isPauseCanvasActive;

        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(isPauseCanvasActive);

        if (localPlayerController != null) {
            localPlayerController.movementRestricted = isPauseCanvasActive;
        }

        if (isPauseCanvasActive) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // No longer resetting movement here.
        }
        else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ReturnToGame() {
        isPauseCanvasActive = false;
        IsPaused = false;
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);

        if (localPlayerController != null) {
            localPlayerController.movementRestricted = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoToMainMenu() {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("NetworkManager shut down.");
        }

        isPauseCanvasActive = false;
        IsPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu_SCENE");
    }

    public void OpenSettingsCanvas() {
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(true);

        if (localPlayerController != null) {
            localPlayerController.movementRestricted = true;
        }


        IsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void OpenGameOverCanvas() {
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(false);
        if (playerUICanvas != null)
            playerUICanvas.gameObject.SetActive(false);
        if (gameOverCanvas != null)
            gameOverCanvas.gameObject.SetActive(true);

        if (localPlayerController != null) {
            localPlayerController.movementRestricted = true;
        }
        IsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }
}
