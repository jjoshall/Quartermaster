using UnityEngine;
using Unity.Netcode;

public class CameraTooltipTrigger : NetworkBehaviour
{
    GameObject _playerObj;
    NetworkObject n_player;
    ulong thisObjOwnerId; 
    [Tooltip("Time in seconds required to hover over an object before showing the tooltip.")]
    public float hoverThreshold = 1.0f; 

    private float hoverTime = 0f;
    private GameObject currentTarget;
    private Tooltippable currentTooltippable;
    private bool tooltipShown = false;
    void Update()
    {
        RayCastCheck();
    }

    void RayCastCheck(){

        // Create a ray from the main camera through the mouse cursor position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            // Try to get the Tooltippable component on the hit object.
            Tooltippable tooltip = hitObject.GetComponent<Tooltippable>();

            if (tooltip != null)
            {
                // If we're still hovering the same object.
                if (currentTarget == hitObject)
                {
                    hoverTime += Time.deltaTime;
                    // Once we've exceeded the hover threshold and haven't yet shown the tooltip.
                    if (!tooltipShown && hoverTime >= hoverThreshold)
                    {
                        if (!_playerObj) FindAndSetPlayer(); // saves netobj and ownerid too

                        tooltip.SendMyTooltipTo(thisObjOwnerId);
                        tooltipShown = true;
                    }
                }
                else
                {
                    // Hovered over a new object. Reset the hover timer and tooltip flag.
                    ResetTooltip();
                    currentTarget = hitObject;
                    currentTooltippable = tooltip;
                }
            }
            else
            {
                // If the hit object doesn't have a Tooltippable, reset any tooltip.
                ResetTooltip();
            }
        }
        else
        {
            // Nothing hit, so reset any tooltip data.
            ResetTooltip();
        }
    }

        // Helper method to reset hover data and hide the tooltip if needed.
    void ResetTooltip()
    {
        if (currentTooltippable != null && tooltipShown)
        {
            currentTooltippable.HideTooltip();
        }
        currentTarget = null;
        currentTooltippable = null;
        hoverTime = 0f;
        tooltipShown = false;
    }

    void FindAndSetPlayer(){
        if (_playerObj == null){
            _playerObj = this.gameObject.transform.root.gameObject;

            // if root isn't the player, then search for the player in child objs.
            if (!_playerObj.CompareTag("Player")){
                GameObject childPlayer = FindChildWithTag(this.gameObject, "Player");
                if (childPlayer != null){
                    _playerObj = childPlayer;
                    return;
                }
            }
        }
        if (_playerObj == null){
            Debug.LogError("Player object not found in CameraTooltipTrigger");
            return;
        }

        n_player = _playerObj.GetComponent<NetworkObject>();
        thisObjOwnerId = n_player.OwnerClientId;
    }

    GameObject FindChildWithTag(GameObject parent, string tag)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
                return child.gameObject;

            // Recursively search the child's children.
            GameObject result = FindChildWithTag(child.gameObject, tag);
            if (result != null)
                return result;
        }
        return null;
    }
}
