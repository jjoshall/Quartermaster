using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect_NoTextStretch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector2 originalSize;
    public float widthIncrease = 20f;    // How much wider the button becomes on hover
    public float animationSpeed = 10f;

    private Vector2 targetSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;
        targetSize = originalSize;
    }

    private void Update()
    {
        rectTransform.sizeDelta = Vector2.Lerp(rectTransform.sizeDelta, targetSize, Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetSize = new Vector2(originalSize.x + widthIncrease, originalSize.y);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetSize = originalSize;
    }
}

