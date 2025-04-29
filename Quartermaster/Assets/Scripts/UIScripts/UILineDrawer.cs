using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UILineDrawer : MonoBehaviour
{
    private GameObject _camRef;
    private GameObject _highlightObjectRef;

    public Vector2 origin2d;
    public Vector2 dest2d;

    public float thickness = 2f;
    public float currScale = 0f; // 0-1f
    public float drawDuration = 1f;

    void Start()
    {
        // Initialize the line with default values
        this.origin2d = Vector2.zero;
        this.dest2d = Vector2.zero;
        this.transform.localScale = new Vector3(0f, 0f, 0f);
    }

    public void Initialize(GameObject cam, GameObject highlightObj, Vector2 origin, Vector2 destination)
    {
        _camRef = cam;
        _highlightObjectRef = highlightObj;

        this.origin2d = origin;
        this.dest2d = destination;
        // set x scale to thickness, y scale to 0
        this.transform.localScale = new Vector3(0f, thickness, 1f);
        // align rotation
        Vector3 direction = destination - origin;
        float angle; // angle needs to be the angle between the origin and destination points
        if (direction.x == 0f) {
            angle = 0f; // no rotation needed
        } else {
            angle = Mathf.Atan2(direction.y, direction.x);
            Debug.Log ("angle: " + angle);  
        }
        this.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
        

        AnimateDrawLine();

    }

    public void AnimateDrawLine(){
        LeanTween.value(gameObject, 0f, 1f, drawDuration) 
            .setEase(LeanTweenType.easeInOutCubic)
            .setOnUpdate((float val) =>
            {
                currScale = val;
            })
            .setOnComplete(() =>
            {
                // Step 2: Line draw animation
                Debug.Log("Line draw complete.");
            });
    }

        // Update is called once per frame
    void LateUpdate()
    {
        if (_camRef != null && _highlightObjectRef != null){
            UpdateLocalCanvasPosition();
            UpdateLineDraw(currScale);
        }
    }


    void UpdateLocalCanvasPosition(){
        if (_camRef.TryGetComponent<Camera>(out Camera cam))
        {
            // Convert world position of the highlighted object to screen point
            Vector3 screenPos = cam.WorldToScreenPoint(_highlightObjectRef.transform.position);
            screenPos.z = 0f;

            if (transform.parent == null || !(transform.parent is RectTransform))
            {
                Debug.LogError("TooltippableAnimated: UpdateLocalCanvasPosition() - Parent is not a RectTransform.");
                Debug.LogError("Parent is: " + transform.parent);
                return;
            }
            
            // Convert screen position to local position in canvas
            Vector2 localPoint = CustomScreenToCanvasLocalPoint(this.transform.parent.GetComponent<Canvas>(), screenPos);
            origin2d = new Vector3 (localPoint.x, localPoint.y, 0f);

            dest2d = new Vector3 (0f, 0f, 0f);

            Debug.Log ("origin: " + origin2d + ", destination: " + dest2d + ", midpoint: " + Vector2.Lerp(origin2d, dest2d, 0.5f) + ", angle: " + Mathf.Atan2(dest2d.y - origin2d.y, dest2d.x - origin2d.x) * Mathf.Rad2Deg);
            // UpdateLineDraw (currScale);
        }
    }

    public void UpdateLineDraw(float scale){
        float totalDistance = Vector2.Distance(origin2d, dest2d);
        Vector3 midPoint = new Vector3 ((origin2d.x + dest2d.x) / 2.0f, (origin2d.y + dest2d.y) / 2.0f, 0.0f);
        float x = Mathf.Lerp (origin2d.x, midPoint.x, scale);
        float y = Mathf.Lerp (origin2d.y, midPoint.y, scale);
        Vector3 newPos = new Vector3 (x, y, 0f);

        this.transform.localPosition = newPos; // new Vector3 (newPos.x, newPos.y, 0f);
        this.transform.localScale = new Vector3(totalDistance * scale, thickness, 1f);

        float angle = Mathf.Atan2(dest2d.y - origin2d.y, dest2d.x - origin2d.x) * Mathf.Rad2Deg;
        this.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }

    
    #region helpers
    #endregion 

    Vector2 CustomScreenToCanvasLocalPoint(Canvas canvas, Vector3 screenPos)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Get canvas size
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Normalize screen point (0,0 at bottom left → 1,1 at top right)
        float normalizedX = screenPos.x / Screen.width;
        float normalizedY = screenPos.y / Screen.height;

        // Adjust for canvas pivot (default is (0.5, 0.5), center)
        Vector2 pivotOffset = new Vector2(canvasSize.x * canvasRect.pivot.x, canvasSize.y * canvasRect.pivot.y);

        // Convert to canvas local position
        float localX = (normalizedX * canvasSize.x) - pivotOffset.x;
        float localY = (normalizedY * canvasSize.y) - pivotOffset.y;

        return new Vector2(localX, localY);
    }
}
