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
    public float fadeStartTime, fadeTime;
    float maxFadeTime;

    public float RemainingFadeTime => fadeTime;

    private void Start() {
        maxFadeTime = fadeTime;
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

        if (fadeStartTime > 0) {
            fadeStartTime -= Time.deltaTime;
        }
        else {
            fadeTime -= Time.deltaTime;
            damageImageCanvas.alpha = fadeTime / maxFadeTime;
            if (fadeTime <= 0) {
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
        fadeTime = maxFadeTime;
        fadeStartTime = 1.5f; // Or whatever your default is
        damageImageCanvas.alpha = 1f;
        this.gameObject.SetActive(true);
    }

    private void ReturnToPool()
    {
        this.gameObject.SetActive(false);
        if (DI_Manager_SCRIPT.Instance != null) {
            DI_Manager_SCRIPT.Instance.ReturnIndicatorToPool(this);
        }
    }
}
