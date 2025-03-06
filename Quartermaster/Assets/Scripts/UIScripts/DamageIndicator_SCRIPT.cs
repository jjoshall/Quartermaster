using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

// This will fade the damage indicator off the screen
public class DamageIndicator : MonoBehaviour
{
    public Vector3 damageLocation;
    private Transform playerObj;
    public Transform damageImagePivot;
    public CanvasGroup damageImageCanvas;

    [Header("Fade Settings")]
    public float fadeStartTime = 1.5f;
    public float fadeDuration = 1.5f;

    private float currentFadeStartTime;
    private float currentFadeTime;

    public float RemainingFadeTime => currentFadeTime;

    private void Start() {
        StartCoroutine(FindLocalPlayer());
    }

    private IEnumerator FindLocalPlayer() {
        // Wait until NetworkManager is ready
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);

        // More reliable way to find the local player
        while (playerObj == null) {
            // Find all players
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject player in players) {
                NetworkObject netObj = player.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsLocalPlayer) {
                    playerObj = player.transform;
                    break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Update() {
        if (playerObj == null) {
            return;
        }

        if (currentFadeStartTime > 0) {
            currentFadeStartTime -= Time.deltaTime;
            if (damageImageCanvas != null) {
                damageImageCanvas.alpha = 1.0f;
            }
        }
        else {
            currentFadeTime -= Time.deltaTime;

            if (damageImageCanvas != null) {
                float alphaValue = Mathf.Clamp01(currentFadeTime / fadeDuration);
                damageImageCanvas.alpha = alphaValue;
            }
            else {
                Debug.LogError("Damage Image Canvas is null");
            }

            if (currentFadeTime <= 0) {
                ReturnToPool();
            }
        }
        damageLocation.y = playerObj.position.y; // Keep the damage indicator at the same height as the player
        Vector3 directionToDamage = (damageLocation - playerObj.position).normalized;
        float angle = Vector3.SignedAngle(directionToDamage, playerObj.forward, Vector3.up);
        damageImagePivot.localEulerAngles = new Vector3(0, 0, angle);
    }

    public void Initialize(Vector3 damagePos) {
        damageLocation = damagePos;
        currentFadeStartTime = fadeStartTime;
        currentFadeTime = fadeDuration;

        if (damageImageCanvas != null) { 
            damageImageCanvas.alpha = 1f;
        }
        else
        {
            damageImageCanvas = GetComponent<CanvasGroup>();
            if (damageImageCanvas == null)
            {
                Debug.LogError("Failed to find CanvasGroup on DamageIndicator!");
            }
        }

        gameObject.SetActive(true);
    }

    private void ReturnToPool()
    {
        this.gameObject.SetActive(false);
        if (DI_Manager_SCRIPT.Instance != null) {
            DI_Manager_SCRIPT.Instance.ReturnIndicatorToPool(this);
        }
    }

    private void OnEnable()
    {
        // Ensure CanvasGroup is properly referenced
        if (damageImageCanvas == null)
        {
            damageImageCanvas = GetComponent<CanvasGroup>();
            if (damageImageCanvas == null)
            {
                Debug.LogError("DamageIndicator is missing CanvasGroup reference!");
            }
        }
    }
}
