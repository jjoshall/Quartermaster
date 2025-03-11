using UnityEngine;

public class Tooltippable : MonoBehaviour
{
    [SerializeField] private Vector2 startPos;
    [SerializeField] private Vector2 finalPos;
    [SerializeField] private float holdDuration;
    [SerializeField] private string message;

    public void SendMyTooltipTo(ulong clientid){
        TooltipManager.SendTooltipToClient(message, holdDuration, startPos, finalPos, clientid);
    }

    public void HideTooltip(){
        // we need to alert tooltipmanager to destroy the tooltip somehow. WIP.
    }


}
