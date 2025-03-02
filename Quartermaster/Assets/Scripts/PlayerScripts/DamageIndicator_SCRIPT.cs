using UnityEngine;
using UnityEngine.UI;

// This will rotate the damage indicator on the screen towards the direction of damage
public class DamageIndicator : MonoBehaviour {
    public Vector3 damageLocation;
    public Transform playerObj;
    public Transform damageImagePivot;

    public CanvasGroup damageImageCanvasGroup;
    public float fadeStartTime, fadeTime;
    float maxFadeTime;

    private void Start() {
        maxFadeTime = fadeTime;
    }

    private void Update() {
        if (playerObj == null) return;

        if (fadeStartTime > 0) {
            fadeStartTime -= Time.deltaTime;
        } else {
            fadeTime -= Time.deltaTime;
            damageImageCanvasGroup.alpha = fadeTime / maxFadeTime;
            if (fadeTime <= 0) {
                Destroy(this.gameObject);
            }
        }

        damageLocation.y = playerObj.position.y;
        Vector3 direction = (damageLocation - playerObj.position).normalized;
        float angle = (Vector3.SignedAngle(direction, playerObj.forward, Vector3.up));
        damageImagePivot.transform.localEulerAngles = new Vector3(0, 0, angle);
    }
}
