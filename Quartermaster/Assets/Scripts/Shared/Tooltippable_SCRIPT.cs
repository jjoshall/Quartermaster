using UnityEngine;
using UnityEngine.Localization.Settings;

public class Tooltippable : MonoBehaviour
{
    // [SerializeField] private Vector2 startPos;
    // [SerializeField] private Vector2 finalPos;
    [SerializeField] private float holdDuration;
    [SerializeField] private string key;

    public void SendMyTooltipTo(ulong clientid){
        string message = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("Localization Table", key).Result;
        // get canvaswidth and height from window size
        int canvasWidth = Screen.width;
        int canvasHeight = Screen.height;

        int tooltipY = canvasHeight / 2 + canvasHeight / 10;
        int tooltipX = canvasWidth / 2 - canvasWidth / 10;

        Vector2 startPos = new Vector2(-300, tooltipY);
        Vector2 finalPos = new Vector2(tooltipX, tooltipY);
        TooltipManager.SendTooltipToClient(message, holdDuration, startPos, finalPos, clientid);
    }

    public void HideTooltip(){
        // we need to alert tooltipmanager to destroy the tooltip somehow. WIP.
    }


}
