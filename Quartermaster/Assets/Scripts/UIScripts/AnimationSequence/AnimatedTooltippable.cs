using UnityEngine;

public class AnimatedTooltippable : MonoBehaviour
{
    public string tooltipHeaderText = "Default Tooltip Text";
    public string tooltipBodyText = "Default Tooltip Body Text";
    public int headerFontSize = 30;
    public int bodyFontSize = 18;

    public void UpdateTooltipHeader(string newHeaderText)
    {
        tooltipHeaderText = newHeaderText;
        // Update the UI element that displays the header text
        // Example: tooltipHeaderTextUI.text = tooltipHeaderText;
    }

    public void UpdateTooltipBody(string newBodyText)
    {
        tooltipBodyText = newBodyText;
        // Update the UI element that displays the body text
        // Example: tooltipBodyTextUI.text = tooltipBodyText;
    }
}
