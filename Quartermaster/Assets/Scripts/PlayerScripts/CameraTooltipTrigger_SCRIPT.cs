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

        int i = 1;
        RaycastHit closestHit = hits[0];
        while ((
                closestHit.collider == null ||
                closestHit.collider.gameObject.GetComponent<AnimatedTooltippable>() == null ||
                (closestHit.collider.gameObject.GetComponent<Item>() != null && closestHit.collider.gameObject.GetComponent<Item>().IsPickedUp)
                ) 
                    && i < hits.Length)
        {
            
            closestHit = hits[i];
            i++;
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

//         // Helper method to reset hover data and hide the tooltip if needed.
//     void ResetTooltip()
//     {
//         if (currentTooltippable != null && tooltipShown)
//         {
//             currentTooltippable.HideTooltip();
//         }
//         currentTarget = null;
//         currentTooltippable = null;
//         hoverTime = 0f;
//         tooltipShown = false;
//     }

//     void FindAndSetPlayer(){
//         if (_playerObj == null){
//             _playerObj = this.gameObject.transform.root.gameObject;

//             // if root isn't the player, then search for the player in child objs.
//             if (!_playerObj.CompareTag("Player")){
//                 GameObject childPlayer = FindChildWithTag(this.gameObject, "Player");
//                 if (childPlayer != null){
//                     _playerObj = childPlayer;
//                     return;
//                 }
//             }
//         }
//         if (_playerObj == null){
//             Debug.LogError("Player object not found in CameraTooltipTrigger");
//             return;
//         }

//         n_player = _playerObj.GetComponent<NetworkObject>();
//         thisObjOwnerId = n_player.OwnerClientId;
//     }

//     GameObject FindChildWithTag(GameObject parent, string tag)
//     {
//         foreach (Transform child in parent.transform)
//         {
//             if (child.CompareTag(tag))
//                 return child.gameObject;

//             // Recursively search the child's children.
//             GameObject result = FindChildWithTag(child.gameObject, tag);
//             if (result != null)
//                 return result;
//         }
//         return null;
//     }
// }


//!SECTION
// using UnityEngine;
// using Unity.Netcode;
// using UnityEngine.Events;

// public class CameraTooltipTrigger : NetworkBehaviour
// {
//     GameObject _playerObj;
//     NetworkObject n_player;
//     ulong thisObjOwnerId; 
//     [Tooltip("Time in seconds required to hover over an object before showing the tooltip.")]
//     public float hoverThreshold = 1.0f; 

//     private float hoverTime = 0f;
//     private GameObject currentTarget;

//     [SerializeField] private GameObject tooltipCirclePrefab;
  
//     GameObject _activeTooltipCircle;


//     private bool tooltipShown = false;

//     void Start()
//     {
//     }


//     void Update()
//     {
//         RayCastCheck();
//     }

//     void RayCastCheck(){

//         // Create a ray from the main camera through the mouse cursor position.
//         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//         RaycastHit hit;
//         int layerMask = ~(1 << LayerMask.NameToLayer("HeldItem")); // make a layer mask to ignore HeldItem layer

//         if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
//         {
//             GameObject hitObject = hit.collider.gameObject;
//             if (hitObject == null || hitObject.GetComponent<AnimatedTooltippable>() == null)
//             {
//                 // If the raycast hit nothing, reset any tooltip.
//                 DestroyPreviousTooltip();
//                 ResetCurrHover();
//                 return;
//             } else if (currentTarget != hitObject)
//             {
//                 // We hit something but it's not the previous object we tooltipped.
//                 DestroyPreviousTooltip();
//             }
//             // Try to get the Tooltippable component on the hit object.
//             AnimatedTooltippable data = hitObject.GetComponent<AnimatedTooltippable>();

//             // If we're still hovering the same object.
//             if (currentTarget == hitObject)
//             {
//                 hoverTime += Time.deltaTime;
//                 // Once we've exceeded the hover threshold and haven't yet shown the tooltip.
//                 if (!tooltipShown && hoverTime >= hoverThreshold)
//                 {
//                     if (!_playerObj) FindAndSetPlayer(); // saves netobj and ownerid too

//                     tooltipShown = true;

//                     SpawnTooltip(hitObject, data.tooltipText, data.fontSize);
//                 }
//             }
//             else
//             {
//                 // Hovered over a new object. Reset the hover timer and tooltip flag.
//                 ResetCurrHover();
//                 currentTarget = hitObject;
//             }

//         }
//         else
//         {
//             // Nothing hit, so reset any tooltip data.
//             DestroyPreviousTooltip();
//         }
//     }

//     void SpawnTooltip(GameObject hitObject, string text, int fontSize){
        
//         _activeTooltipCircle = Instantiate(tooltipCirclePrefab, UIManager.instance.playerDrawCanvas.transform);
//         Debug.Log ("newTooltip instantiated");
//         if (_activeTooltipCircle == null) {
//             Debug.LogError("Tooltip circle prefab is null. Check the prefab assignment.");
//             return;
//         }
//         if (_activeTooltipCircle.GetComponent<UITargetCircle>() == null) {
//             Debug.LogError("Tooltip circle prefab does not have TooltippableAnimated component. Check the prefab assignment.");
//             return;
//         }
//         _activeTooltipCircle.GetComponent<UITargetCircle>().Initialize(this.gameObject, hitObject, text, fontSize);

//         currentTarget = hitObject;
//     }

//     void DestroyPreviousTooltip(){
//         if (_activeTooltipCircle != null){
//             Destroy(_activeTooltipCircle);
//             _activeTooltipCircle = null;
//         }
//     }

//         // Helper method to reset hover data and hide the tooltip if needed.
//     void ResetCurrHover()
//     {
//         currentTarget = null;
//         hoverTime = 0f;
//         tooltipShown = false;
//     }

//     void FindAndSetPlayer(){
//         if (_playerObj == null){
//             _playerObj = this.gameObject.transform.root.gameObject;

//             // if root isn't the player, then search for the player in child objs.
//             if (!_playerObj.CompareTag("Player")){
//                 GameObject childPlayer = FindChildWithTag(this.gameObject, "Player");
//                 if (childPlayer != null){
//                     _playerObj = childPlayer;
//                     return;
//                 }
//             }
//         }
//         if (_playerObj == null){
//             Debug.LogError("Player object not found in CameraTooltipTrigger");
//             return;
//         }

//         n_player = _playerObj.GetComponent<NetworkObject>();
//         thisObjOwnerId = n_player.OwnerClientId;
//     }

//     GameObject FindChildWithTag(GameObject parent, string tag)
//     {
//         foreach (Transform child in parent.transform)
//         {
//             if (child.CompareTag(tag))
//                 return child.gameObject;

//             // Recursively search the child's children.
//             GameObject result = FindChildWithTag(child.gameObject, tag);
//             if (result != null)
//                 return result;
//         }
//         return null;
//     }
// }
