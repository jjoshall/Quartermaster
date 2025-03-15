using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TabHoverColor_TMP : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI tabText;
    [SerializeField] private CustomTabButton customTabButton;

    [Header("Hover Settings")]
    [SerializeField] private Color hoverColor = Color.red;

    private Color originalColor;

    private void Awake()
    {
        if (tabText == null)
        {
            tabText = GetComponent<TextMeshProUGUI>();
        }
        if (customTabButton == null)
        {
            customTabButton = GetComponentInParent<CustomTabButton>();
        }
        if (tabText != null)
        {
            originalColor = tabText.color; // assume black by default
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only change color if this tab isn't selected.
        if (customTabButton != null && customTabButton.tabGroup != null &&
            customTabButton.tabGroup.selectedTabRef == customTabButton)
        {
            return;
        }
        tabText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Only reset color if this tab isn't selected.
        if (customTabButton != null && customTabButton.tabGroup != null &&
            customTabButton.tabGroup.selectedTabRef == customTabButton)
        {
            return;
        }
        tabText.color = originalColor;
    }

    // Public method to reset the text color to its default.
    public void ResetTextColor() {
        if (tabText != null) {
            tabText.color = originalColor;  // originalColor should be set to black (or your desired default)
        }
    }
}


