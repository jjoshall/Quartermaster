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

    #region Send1Client
    public static void SendTooltipToClient(string message, float duration, Vector2 start, Vector2 final, ulong clientid){
        Debug.Log ("SendTooltipToClient()");
        if (Instance == null) {
            Debug.LogError("TooltipManager instance not found in the scene.");
            return;
        }
        TooltipManager.Instance.SendTooltipToClientServerRpc(message, duration, start, final, clientid);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendTooltipToClientServerRpc(string message, float duration, Vector2 start, Vector2 final, ulong clientid){
        // Debug.Log ("ServerRPC: SendTooltipToClientServerRpc()");
        ClientRpcParams crp = new ClientRpcParams{
            Send = new ClientRpcSendParams{
                TargetClientIds = new ulong[]{clientid}
            }
        };
        TooltipManager.Instance.ShowTooltipToClientClientRpc(message, duration, start, final, crp);
    }

    [ClientRpc]
    private void ShowTooltipToClientClientRpc(string message, float duration, Vector2 start, Vector2 final, ClientRpcParams clientRpcParams = default){
        // Debug.Log ("ClientRPC: ShowTooltipToClientClientRpc()");
        Instance.CreateTooltip(message, duration, start, final);
    }

    #endregion

    #region SendLocalOrAll
    public static void SendTooltip(string message, bool toAll = false) {
        // Debug.Log ("SendTooltip()");
        if (Instance == null) {
            Debug.LogError("TooltipManager instance not found in the scene.");
            return;
        }

        if (!toAll) {
            // Debug.Log ("Create local tooltip");
            Instance.CreateTooltip(message);
            return;
        }

        Instance.SendTooltipAllServerRpc(message);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendTooltipAllServerRpc(string message, ServerRpcParams rpcParams = default) {
        // Debug.Log ("ServerRPC: SendTooltipAllServerRpc()");
        ShowTooltipClientRpc(message);
    }


    [ClientRpc]
    private void ShowTooltipClientRpc(string message) {
        // Debug.Log ("ClientRPC: ShowTooltipClientRpc()");
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
    #endregion

    #region Overloads
    public static void SendTooltip(string message, float duration, Vector2 start, Vector2 final, bool toAll = false) {
         Debug.Log ("SendTooltip()");
        if (Instance == null) {
            Debug.LogError("TooltipManager instance not found in the scene.");
            return;
        }

        if (!toAll) {
            Debug.Log ("Create local tooltip");
            Instance.CreateTooltip(message, duration, start, final);
            return;
        }

        Instance.SendTooltipAllServerRpc(message, duration, start, final);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendTooltipAllServerRpc(string message, float duration, Vector2 start, Vector2 final, ServerRpcParams rpcParams = default) {
        Debug.Log ("ServerRPC: SendTooltipAllServerRpc()");
        ShowTooltipClientRpc(message, duration, start, final);
    }

    [ClientRpc]
    private void ShowTooltipClientRpc(string message, float duration, Vector2 start, Vector2 final) {
        Debug.Log ("ClientRPC: ShowTooltipClientRpc()");
        CreateTooltip(message, duration, start, final);
    }

    
    public void CreateTooltip(string message, float duration, Vector2 start, Vector2 final) {
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
        StartCoroutine(AnimateTooltip(tooltipInstance, duration, start, final));
    }

    
    private IEnumerator AnimateTooltip(GameObject tooltipInstance, float holdDuration, Vector2 startPos, Vector2 finalPos) {
        RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
        if (rt == null) {
            Destroy(tooltipInstance);
            yield break;
        }

        // Animation durations.
        float slideInDuration = 0.5f;
        float slideOutDuration = 0.5f;
        // float holdDuration = totalDuration - slideInDuration - slideOutDuration;

        // Slide in: from startPos to finalPos with ease-out.
        float t = 0f;
        while (t < slideInDuration) {
            if (rt == null) {
                if (tooltipInstance != null) Destroy(tooltipInstance);
                yield break;
            }
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / slideInDuration);
            float ease = 1 - Mathf.Pow(1 - normalizedTime, 2);
            rt.anchoredPosition = Vector2.Lerp(startPos, finalPos, ease);
            yield return null;
        }
        if (rt == null) {
            if (tooltipInstance != null) Destroy(tooltipInstance);
            yield break;
        }
        rt.anchoredPosition = finalPos;

        // Hold position.
        yield return new WaitForSeconds(holdDuration);

        // Slide out: from finalPos back to startPos with ease-in.
        t = 0f;
        while (t < slideOutDuration) {
            if (rt == null) {
                if (tooltipInstance != null) Destroy(tooltipInstance);
                yield break;
            }
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / slideOutDuration);
            float ease = Mathf.Pow(normalizedTime, 2);
            rt.anchoredPosition = Vector2.Lerp(finalPos, startPos, ease);
            yield return null;
        }
        if (rt == null) {
            if (tooltipInstance != null) Destroy(tooltipInstance);
            yield break;
        }
        rt.anchoredPosition = startPos;

        Destroy(tooltipInstance);
    }




    #endregion
}
