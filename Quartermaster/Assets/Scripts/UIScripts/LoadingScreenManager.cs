using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour {
    public static LoadingScreenManager Instance { get; private set; }

    [Header("Loading Panel References")]
    [SerializeField] private GameObject loadingPanel;       // The panel containing your loading UI
    [SerializeField] private CanvasGroup loadingCanvasGroup;  // The CanvasGroup used for fading

    [Header("UI Elements")]
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private Canvas lobbyMenuCanvas;

    [Header("Timing Settings")]
    public float fadeDuration = 1f;
    public float waitTimeBeforeProceeding = 3000f; // in milliseconds, e.g. 3000ms = 3 seconds
    public float delayedUIEnableWait = 0.5f;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
        }
    }

    /// <summary>
    /// Shows the loading screen by:
    /// 1. Activating the loading panel,
    /// 2. Fading it in,
    /// 3. Disabling the lobby menu,
    /// 4. Waiting a set time,
    /// 5. Fading it out,
    /// 6. And finally deactivating the panel.
    /// Returns the fade-out Task so it can be awaited.
    /// </summary>
    public async Task<Task> ShowLoadingScreenAsync() {
        // Activate the loading panel right before starting the transition.
        if (loadingPanel != null) {
            loadingPanel.SetActive(true);
            //Debug.Log("Loading panel activated.");
        } else {
            Debug.LogWarning("Loading Panel reference is missing!");
        }

        // Fade in the loading panel.
        if (loadingCanvasGroup != null) {
            loadingCanvasGroup.alpha = 0f;
            //Debug.Log("Fading in LoadingPanel");
            await FadeCanvasGroup(loadingCanvasGroup, 0f, 1f, fadeDuration);
            Debug.Log("Finished fade in");
        } else {
            Debug.LogWarning("Loading CanvasGroup is missing!");
        }

        // Disable lobby menu canvas so it doesn't interfere.
        if (lobbyMenuCanvas != null) {
            lobbyMenuCanvas.enabled = false;
            //Debug.Log("LobbyMenuCanvas hidden.");
        } else {
            Debug.LogWarning("LobbyMenuCanvas reference is missing!");
        }

        // Wait for the specified time (in milliseconds).
        //Debug.Log($"Waiting {waitTimeBeforeProceeding / 1000f} seconds before proceeding...");
        await Task.Delay((int)waitTimeBeforeProceeding);

        // Fade out the loading panel.
        Task fadeOutTask = null;
        if (loadingPanel != null && loadingCanvasGroup != null) {
            //Debug.Log("Starting fade out of LoadingPanel");
            fadeOutTask = FadeCanvasGroup(loadingCanvasGroup, 1f, 0f, fadeDuration);
            await fadeOutTask;
            //Debug.Log("Finished fade out");
            // Fully deactivate the loading panel after fade out.
            loadingPanel.SetActive(false);
            //Debug.Log("LoadingPanel deactivated after fade out");
        }
        return fadeOutTask;
    }

    /// <summary>
    /// Coroutine to update UI elements after relay creation.
    /// Activates the player UI canvas, sets the join code text,
    /// and then deactivates the loading panel.
    /// </summary>
    public IEnumerator HandleDelayedUIEnable(string joinCode) {
        yield return new WaitForSeconds(delayedUIEnableWait);

        //Debug.Log("Activating playerUICanvas and updating joinCodeText");
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

        // Ensure the loading panel is deactivated.
        if (loadingPanel != null) {
            //Debug.Log("Disabling LoadingPanel after UI update");
            loadingPanel.SetActive(false);
            //Debug.Log("LoadingPanel active state after SetActive(false): " + loadingPanel.activeSelf);
        } else {
            Debug.LogWarning("Loading Panel reference is missing during UI update!");
        }

        if (lobbyMenuCanvas != null) {
            lobbyMenuCanvas.enabled = false;
            //Debug.Log("LobbyMenuCanvas hidden.");
        } else {
            Debug.LogWarning("LobbyMenuCanvas reference is missing!");
        }
    }

    /// <summary>
    /// In case of an error, simply disable the loading panel.
    /// </summary>
    public void DisableLoadingPanel() {
        if (loadingPanel != null) {
            //Debug.Log("Disabling LoadingPanel due to error");
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Asynchronously fades a CanvasGroup’s alpha from startAlpha to endAlpha over the specified duration.
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

