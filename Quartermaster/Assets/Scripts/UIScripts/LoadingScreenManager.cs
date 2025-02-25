using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour {
    public static LoadingScreenManager Instance { get; private set; }

    [Header("Loading Panel References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    
    [Header("UI Elements")]
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private Canvas lobbyMenuCanvas;

    [Header("Timing Settings")]
    public float fadeDuration = 1f;
    public float waitTimeBeforeProceeding = 3f;
    public float delayedUIEnableWait = 0.5f;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
        }
    }

    /// <summary>
    /// Shows the loading screen by fading it in, disabling lobby UI, waiting a set time,
    /// and then starting the fade out concurrently.
    /// Returns the fade-out Task so that it can be awaited later.
    /// </summary>
    public async Task<Task> ShowLoadingScreenAsync() {
        if (loadingPanel != null) {
            loadingPanel.SetActive(true);
            if (loadingCanvasGroup != null) {
                loadingCanvasGroup.alpha = 0f;
                Debug.Log("Fading in LoadingPanel");
                await FadeCanvasGroup(loadingCanvasGroup, 0f, 1f, fadeDuration);
                Debug.Log("Finished fade in");
            } else {
                Debug.LogWarning("Loading CanvasGroup is missing!");
            }
        } else {
            Debug.LogWarning("Loading Panel reference is missing!");
        }

        // Disable lobby menu canvas if available.
        if (lobbyMenuCanvas != null) {
            lobbyMenuCanvas.enabled = false;
            Debug.Log("LobbyMenuCanvas hidden.");
        } else {
            Debug.LogWarning("LobbyMenuCanvas reference is missing!");
        }

        // Wait to ensure the loading panel is visible.
        Debug.Log($"Waiting {waitTimeBeforeProceeding} seconds before proceeding...");
        await Task.Delay((int)(waitTimeBeforeProceeding * 1000));

        // Start fade out concurrently.
        Task fadeOutTask = null;
        if (loadingPanel != null && loadingCanvasGroup != null) {
            Debug.Log("Starting fade out concurrently with scene initialization");
            fadeOutTask = FadeCanvasGroup(loadingCanvasGroup, 1f, 0f, fadeDuration);
        }
        return fadeOutTask;
    }

    /// <summary>
    /// Coroutine to update UI elements after relay creation.
    /// Activates the player UI canvas, shows and sets the join code text,
    /// disables the loading panel, and hides the lobby UI.
    /// </summary>
    public IEnumerator HandleDelayedUIEnable(string joinCode) {
        yield return new WaitForSeconds(delayedUIEnableWait);

        Debug.Log("Activating playerUICanvas and joinCodeText");
        if (playerUICanvas != null) {
            playerUICanvas.gameObject.SetActive(true);
        } else {
            Debug.LogWarning("Player UI Canvas reference is missing!");
        }

        if (joinCodeText != null) {
            joinCodeText.gameObject.SetActive(true);
            joinCodeText.text = joinCode;
            Debug.Log("JoinCode set to: " + joinCode);
        } else {
            Debug.LogWarning("JoinCode Text reference is missing during UI update!");
        }

        if (loadingPanel != null) {
            Debug.Log("Disabling LoadingPanel after UI update");
            loadingPanel.SetActive(false);
            Debug.Log("LoadingPanel active state after SetActive(false): " + loadingPanel.activeSelf);
        } else {
            Debug.LogWarning("Loading Panel reference is missing during UI update!");
        }

        if (lobbyMenuCanvas != null) {
            lobbyMenuCanvas.enabled = false;
            Debug.Log("LobbyMenuCanvas hidden.");
        } else {
            Debug.LogWarning("LobbyMenuCanvas reference is missing!");
        }
    }

    /// <summary>
    /// In case of an error, simply disable the loading panel.
    /// </summary>
    public void DisableLoadingPanel() {
        if (loadingPanel != null) {
            Debug.Log("Disabling LoadingPanel due to error");
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Asynchronously fades a CanvasGroup’s alpha from startAlpha to endAlpha over duration.
    /// </summary>
    private async Task FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration) {
        float elapsed = 0f;
        cg.alpha = startAlpha;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            await Task.Yield();
        }
        cg.alpha = endAlpha;
    }
}

