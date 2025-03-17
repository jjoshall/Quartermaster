using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class ButtonHoverEffect_WithShakeGlitch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // === Button resizing variables ===
    private RectTransform rectTransform;
    private Vector2 originalSize;
    public float widthIncrease = 20f;         // How much wider the button becomes on hover
    public float animationSpeed = 10f;        // Lerp speed for button resizing
    private Vector2 targetSize;

    // === Text scaling variables ===
    public TextMeshProUGUI text1;             // Reference in Inspector
    public TextMeshProUGUI text2;             // Reference in Inspector
    private Vector3 text1OriginalScale;
    private Vector3 text2OriginalScale;
    public float textScaleMultiplier = 1.1f;  // How much bigger text scales on hover
    private Vector3 text1TargetScale;
    private Vector3 text2TargetScale;

    // === Shake glitch variables ===
    [Header("Shake Glitch Settings")]
    public float glitchDuration = 0.3f;       // How long the shake glitch lasts
    public float glitchInterval = 0.05f;      // How often to randomize positions during the glitch
    public float shakeMagnitude = 5f;         // Maximum distance (in pixels) to shake text

    private Vector3 text1OriginalPosition;
    private Vector3 text2OriginalPosition;
    
    private Coroutine glitchCoroutine;

    private void Awake()
    {
        // Grab the RectTransform (the button itself)
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;
        targetSize = originalSize;

        // Store original text scales
        if (text1 != null)
        {
            text1OriginalScale = text1.transform.localScale;
            text1TargetScale = text1OriginalScale;
            text1OriginalPosition = text1.transform.localPosition;
        }
        if (text2 != null)
        {
            text2OriginalScale = text2.transform.localScale;
            text2TargetScale = text2OriginalScale;
            text2OriginalPosition = text2.transform.localPosition;
        }
    }

    private void Update()
    {
        // Smoothly resize the button
        rectTransform.sizeDelta = Vector2.Lerp(rectTransform.sizeDelta, targetSize, Time.deltaTime * animationSpeed);

        // Smoothly scale the text (so it grows/shrinks)
        if (text1 != null)
        {
            text1.transform.localScale =
                Vector3.Lerp(text1.transform.localScale, text1TargetScale, Time.deltaTime * animationSpeed);
        }
        if (text2 != null)
        {
            text2.transform.localScale =
                Vector3.Lerp(text2.transform.localScale, text2TargetScale, Time.deltaTime * animationSpeed);
        }
    }

    // === Mouse enters the button area ===
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Enlarge the button
        targetSize = new Vector2(originalSize.x + widthIncrease, originalSize.y);

        // Scale up the text
        if (text1 != null)
            text1TargetScale = text1OriginalScale * textScaleMultiplier;
        if (text2 != null)
            text2TargetScale = text2OriginalScale * textScaleMultiplier;

        // Start the shake glitch effect
        if (glitchCoroutine != null) StopCoroutine(glitchCoroutine);  // Stop any ongoing shake
        glitchCoroutine = StartCoroutine(ShakeGlitchRoutine(glitchDuration, glitchInterval));
    }

    // === Mouse leaves the button area ===
    public void OnPointerExit(PointerEventData eventData)
    {
        // Restore original button size
        targetSize = originalSize;

        // Restore text scale
        if (text1 != null)
            text1TargetScale = text1OriginalScale;
        if (text2 != null)
            text2TargetScale = text2OriginalScale;

        // End the shake glitch effect
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }
        ResetTextPositions();
    }

    // === Coroutine to "shake" the text for a short duration ===
    private IEnumerator ShakeGlitchRoutine(float duration, float interval)
    {
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            if (text1 != null)
            {
                text1.transform.localPosition = text1OriginalPosition +
                    (Vector3)Random.insideUnitCircle * shakeMagnitude;
            }
            if (text2 != null)
            {
                text2.transform.localPosition = text2OriginalPosition +
                    (Vector3)Random.insideUnitCircle * shakeMagnitude;
            }

            yield return new WaitForSeconds(interval);
        }

        ResetTextPositions();
    }

    // === Helper to restore text positions to original ===
    private void ResetTextPositions()
    {
        if (text1 != null)
            text1.transform.localPosition = text1OriginalPosition;
        if (text2 != null)
            text2.transform.localPosition = text2OriginalPosition;
    }
}
