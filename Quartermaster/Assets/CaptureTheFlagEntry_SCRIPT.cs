using UnityEngine;
using System.Collections;

public class CaptureTheFlagEntry : MonoBehaviour {
    [SerializeField] private GameObject CaptureTheFlagObjective;

    [SerializeField] private Renderer subChildRenderer;
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private float fadeTime = 1;
    [SerializeField] private Color objectColor = Color.white;

    private Material subChildMaterial;
    private Material objectMaterial;
    private Coroutine fadeCoroutine;

    void Start() {
        if (subChildRenderer != null) {
            subChildMaterial = subChildRenderer.material;
            subChildMaterial.SetFloat("_Opacity", 0f);
            subChildMaterial.SetColor("_BaseColor", objectColor);
        } else {
            Debug.LogError("ObjectiveRingEntry: No Renderer assigned to subChildRenderer.");
        }

        if (objectRenderer != null) {
            objectMaterial = objectRenderer.material;
            objectMaterial.SetFloat("_Opacity", 0f);
            objectMaterial.SetColor("_BaseColor", objectColor);
        } else {
            Debug.LogError("ObjectiveRingEntry: No Renderer assigned to objectRenderer.");
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (subChildMaterial != null && objectMaterial != null) {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOpacity(1f, fadeTime));
        }
        CaptureTheFlagObjective.GetComponent<CaptureTheFlagObjective>().PublicTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other) {
        if (subChildMaterial != null && objectMaterial != null) {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOpacity(0f, fadeTime));
        }
        CaptureTheFlagObjective.GetComponent<CaptureTheFlagObjective>().PublicTriggerExit(other);
    }

    private IEnumerator FadeOpacity(float targetOpacity, float duration) {
        float startOpacity = subChildMaterial.GetFloat("_Opacity");
        float time = 0f;

        while (time < duration) {
            time += Time.deltaTime;
            float newOpacity = Mathf.Lerp(startOpacity, targetOpacity, time / duration);
            subChildMaterial.SetFloat("_Opacity", newOpacity);
            objectMaterial.SetFloat("_Opacity", newOpacity);
            yield return null;
        }

        subChildMaterial.SetFloat("_Opacity", targetOpacity);
        objectMaterial.SetFloat("_Opacity", targetOpacity);
    }
}
