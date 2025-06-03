using UnityEngine;
using Unity.Netcode;

public class CameraTooltipTrigger : NetworkBehaviour
{
    GameObject _playerObj;
    NetworkObject n_player;
    [Tooltip("Time in seconds required to hover over an object before showing the tooltip.")]
    public float hoverThreshold = 1.0f; 
    [Tooltip("Tooltip trigger raycast range")]
    public float raycastRange = 15f; // Raycast range for tooltip trigger
    public GameObject animatedTooltipPrefab;

    // Runtime
    private float hoverTime = 0f;
    private GameObject prevTarget;
    private GameObject prevTooltippable;
    private bool tooltipShown = false;

    void Start()
    {
        _playerObj = this.gameObject.transform.root.gameObject;
        n_player = _playerObj.GetComponent<NetworkObject>();
    }

    void Update()
    {
        RayCastCheck();
    }

    void RayCastCheck(){

        // Create a ray from the main camera through the mouse cursor position.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // RaycastHit hit;

        RaycastHit[] hits = Physics.RaycastAll(ray, raycastRange);
        if (hits.Length == 0){
            if (prevTarget && prevTooltippable) Destroy(prevTooltippable); // destroy the previous tooltip if it exists.
            prevTarget = null; // reset the previous target.
            hoverTime = 0f; // reset the hover time.
            tooltipShown = false; // reset the tooltip shown flag.
            return;
        }

        // int i = 1;
        RaycastHit closestHit = hits[0];
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue; // skip null colliders.
            if (hits[i].collider.gameObject == null) continue; // skip null game objects.
            if (hits[i].collider.gameObject.GetComponent<AnimatedTooltippable>() == null) continue; // skip objects without AnimatedTooltippable component.
            if (hits[i].collider.gameObject.GetComponent<Item>() != null && hits[i].collider.gameObject.GetComponent<Item>().IsPickedUp) continue; // skip items that are picked up.

            closestHit = hits[i]; // found a valid hit with a tooltippable component.
            break; // exit the loop early since we found a valid hit.
        }

        GameObject currGO = closestHit.collider.gameObject;

        if (closestHit.collider == null || currGO == null || currGO.GetComponent<AnimatedTooltippable>() == null)
        {
            if (prevTarget && prevTooltippable) Destroy(prevTooltippable);
            hoverTime = 0f;
            tooltipShown = false;
            return;
        } else if (currGO != prevTarget){
            // We hit something but it's not the previous object we tooltipped.
            if (prevTarget && prevTooltippable) Destroy(prevTooltippable);
            prevTarget = currGO;
            hoverTime = 0f;
            tooltipShown = false;
            return;
        }

        if (currGO == prevTarget){
            if (IncrementHover()){
                var at = currGO.GetComponent<AnimatedTooltippable>();
                if (at == null) return; // no tooltippable component on the object.

                // Spawn the tooltip at the hit point.
                GameObject tooltip = Instantiate(animatedTooltipPrefab, UIManager.instance.playerDrawCanvas.transform);
                tooltip.GetComponent<UITargetCircle>().Initialize(this.gameObject, currGO, at.tooltipHeaderText, at.headerFontSize, at.tooltipBodyText, at.bodyFontSize);

                if (prevTarget && prevTooltippable) Destroy(prevTooltippable); // destroy the previous tooltip if it exists.
                tooltipShown = true;
                prevTooltippable = tooltip; // store the tooltip for later destruction.
                prevTarget = currGO; // store the current target for hover time tracking.
            }
        }
    }

    bool IncrementHover(){
        hoverTime += Time.deltaTime;
        if (hoverTime >= hoverThreshold)
        {
            // Once we've exceeded the hover threshold and haven't yet shown the tooltip.
            if (prevTarget == null) return false; // no target to show tooltip for.

            if (!tooltipShown)
            {
                return true; // hover time exceed threshold and tooltip isn't already shown. return true to spawn a tooltip.
            }
        }
        return false;
    }


}
