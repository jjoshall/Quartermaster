using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// This will fade the damage indicator off the screen
public class DamageIndicator : MonoBehaviour {
    public Transform damageImagePivot;
    private CanvasGroup _damageImageCanvasGroup;
    public float fadeStartTime, fadeTime = 1.5f;
    float maxFadeTime = 1.5f;

    private void Awake() {
        _damageImageCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update() {
        if (fadeStartTime > 0) {
            fadeStartTime -= Time.deltaTime;
        } else {
            fadeTime -= Time.deltaTime;
            _damageImageCanvasGroup.alpha = fadeTime / maxFadeTime;
            if (fadeTime <= 0) {
                Destroy(this.gameObject);
            }
        }
    }
}
