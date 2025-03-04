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

    Vector2 finalPos = new Vector2(-930, 510);
    Vector2 startPos = new Vector2(-1350, 510);

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public static void SendTooltip(string message, bool toAll = false, ulong targetClientId = 0) {
        if (Instance == null) {
            Debug.LogError("TooltipManager instance not found in the scene.");
            return;
        }

        if (!toAll && targetClientId == 0) {
            Instance.CreateTooltip(message);
            return;
        }

        if (NetworkManager.Singleton.IsServer) {
            if (targetClientId != 0) {
                Instance.ShowTooltipClientRpc(message, new ClientRpcParams {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
                });
            } else if (toAll) {
                Instance.ShowTooltipClientRpc(message);
            }
        } else {
            Instance.SendTooltipServerRpc(message, toAll, targetClientId);
        }
    }

    [ServerRpc]
    private void SendTooltipServerRpc(string message, bool toAll, ulong targetClientId, ServerRpcParams rpcParams = default) {
        if (targetClientId != 0) {
            ShowTooltipClientRpc(message, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            });
        } else if (toAll) {
            ShowTooltipClientRpc(message);
        } else {
            ShowTooltipClientRpc(message, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
            });
        }
    }

    [ClientRpc]
    private void ShowTooltipClientRpc(string message, ClientRpcParams clientRpcParams = default) {
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

        // Get the RectTransform and set initial position off-screen (to the left).
        RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
        if (rt != null) {
            rt.anchoredPosition = startPos;
        }

        TMPro.TextMeshProUGUI textComponent = tooltipInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComponent != null) {
            textComponent.text = message;
        } else {
            Debug.LogError("TooltipManager: tooltipPrefab is missing a Text component in its children.");
        }

        // Animate the tooltip (slide in, hold, slide out) and then destroy it.
        StartCoroutine(AnimateTooltip(tooltipInstance, 3f));
    }

    /// <summary>
    /// Animates the tooltip:
    ///   - Slides in from left (0.5 seconds, ease out)
    ///   - Holds for the remaining time (2 seconds)
    ///   - Slides out to the left (0.5 seconds, ease in)
    /// </summary>
    /// <param name="tooltipInstance">The tooltip GameObject.</param>
    /// <param name="totalDuration">Total time before the tooltip is destroyed.</param>
    private IEnumerator AnimateTooltip(GameObject tooltipInstance, float totalDuration) {
        RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
        if (rt == null) {
            Destroy(tooltipInstance);
            yield break;
        }

        // Define animation durations.
        float slideInDuration = 0.5f;
        float slideOutDuration = 0.5f;
        float holdDuration = totalDuration - slideInDuration - slideOutDuration;

        // Slide in: from startPos to finalPos with ease-out.
        float t = 0f;
        while (t < slideInDuration) {
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / slideInDuration);
            // Ease out: decelerating.
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
            // Ease in: accelerating.
            float ease = Mathf.Pow(normalizedTime, 2);
            rt.anchoredPosition = Vector2.Lerp(finalPos, startPos, ease);
            yield return null;
        }
        rt.anchoredPosition = startPos;

        Destroy(tooltipInstance);
    }
}
