using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI text;
    public Color normalColor = Color.black;
    public Color hoverColor = Color.white;
    private bool isHovered = false;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        text.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        text.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        text.color = normalColor;
    }

    // Method to force reset the color.
    public void ResetColor()
    {
        isHovered = false;
        text.color = normalColor;
    }

    void Update()
    {
        // Check if the mouse is really over this element.
        if (!RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, Input.mousePosition, null))
        {
            // If not hovered, ensure the color is reset.
            if (isHovered)
            {
                isHovered = false;
                text.color = normalColor;
            }
        }
    }
}
