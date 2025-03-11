using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TooltipManager : NetworkBehaviour {
    public static TooltipManager Instance;

    [Tooltip("Assign the Canvas that holds tooltip UI elements.")]
    public Canvas tooltipCanvas;

    [Tooltip("Assign the tooltip prefab (should contain a Panel with a TextMeshProUGUI component).")]
    public GameObject tooltipPrefab;

    // Tooltip animation positions.
    [SerializeField] private Vector2 finalPos = new Vector2(10, 55);
    [SerializeField] private Vector2 startPos = new Vector2(-700, 550);

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public static void SendTooltip(string message, bool toAll = false) {
        if (Instance == null) {
            Debug.LogError("TooltipManager instance not found in the scene.");
            return;
        }

        if (!toAll) {
            Instance.CreateTooltip(message);
            return;
        }

        if (NetworkManager.Singleton.IsServer) {
            Instance.ShowTooltipClientRpc(message);
        } else {
            Instance.SendTooltipServerRpc(message);
        }
    }

    [ServerRpc()]
    private void SendTooltipServerRpc(string message, ServerRpcParams rpcParams = default) {
        ShowTooltipClientRpc(message);
    }

    [ClientRpc]
    private void ShowTooltipClientRpc(string message) {
        CreateTooltip(message);
    }

    public void CreateTooltip(string message) {
        if (tooltipCanvas == null || tooltipPrefab == null) {
            Debug.LogError("TooltipManager: tooltipCanvas or tooltipPrefab is not assigned.");
            return;
        }

        // Instantiate the tooltip prefab as a child of the canvas.
        GameObject tooltipInstance = Instantiate(tooltipPrefab, tooltipCanvas.transform);
        tooltipInstance.SetActive(true);

        // Set initial position off-screen (to the left).
        RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
        if (rt != null) {
            rt.anchoredPosition = startPos;
        }

        // Set the text of the tooltip.
        TMPro.TextMeshProUGUI textComponent = tooltipInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComponent != null) {
            textComponent.text = message;
        } else {
            Debug.LogError("TooltipManager: tooltipPrefab is missing a Text component in its children.");
        }

        // Animate the tooltip (slide in, hold, slide out) then destroy it.
        StartCoroutine(AnimateTooltip(tooltipInstance, 3f));
    }

    private IEnumerator AnimateTooltip(GameObject tooltipInstance, float totalDuration) {
        RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
        if (rt == null) {
            Destroy(tooltipInstance);
            yield break;
        }

        // Animation durations.
        float slideInDuration = 0.5f;
        float slideOutDuration = 0.5f;
        float holdDuration = totalDuration - slideInDuration - slideOutDuration;

        // Slide in: from startPos to finalPos with ease-out.
        float t = 0f;
        while (t < slideInDuration) {
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / slideInDuration);
            float ease = 1 - Mathf.Pow(1 - normalizedTime, 2);
            rt.anchoredPosition = Vector2.Lerp(startPos, finalPos, ease);
            yield return null;
        }
        rt.anchoredPosition = finalPos;

        // Hold position.
        yield return new WaitForSeconds(holdDuration);

        // Slide out: from finalPos back to startPos with ease-in.
        t = 0f;
        while (t < slideOutDuration) {
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / slideOutDuration);
            float ease = Mathf.Pow(normalizedTime, 2);
            rt.anchoredPosition = Vector2.Lerp(finalPos, startPos, ease);
            yield return null;
        }
        rt.anchoredPosition = startPos;

        Destroy(tooltipInstance);
    }
}
