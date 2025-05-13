using System.Threading.Tasks;
using UnityEngine;

public class LoadingPanelManager : MonoBehaviour {
    [SerializeField] private GameObject loadingPanel;  // Reference to the Loading Panel
    [SerializeField] private float fadeDuration = 1f;

    private CanvasGroup canvasGroup;

    private void Awake() {
        if (loadingPanel != null) {
            canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                Debug.LogWarning("CanvasGroup component is missing on the loading panel. Adding one.");
                canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
            }
            //Debug.Log("LoadingPanelManager Awake: Found loading panel and CanvasGroup.");
        } else {
            Debug.LogWarning("LoadingPanelManager Awake: Loading panel is not assigned!");
        }
    }

    public async Task ShowLoadingPanelAsync() {
        if (loadingPanel == null || canvasGroup == null) {
            Debug.LogWarning("ShowLoadingPanelAsync: Loading panel or CanvasGroup is not set!");
            return;
        }
        //Debug.Log("ShowLoadingPanelAsync: Activating loading panel.");
        loadingPanel.SetActive(true);
        //Debug.Log("ShowLoadingPanelAsync: Starting fade in.");
        await FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration);
        //Debug.Log("ShowLoadingPanelAsync: Fade in complete.");
    }

    public async Task HideLoadingPanelAsync() {
        if (loadingPanel == null || canvasGroup == null) {
            Debug.LogWarning("HideLoadingPanelAsync: Loading panel or CanvasGroup is not set!");
            return;
        }
        //Debug.Log("HideLoadingPanelAsync: Starting fade out.");
        await FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration);
        loadingPanel.SetActive(false);
        //Debug.Log("HideLoadingPanelAsync: Loading panel deactivated.");
    }

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

