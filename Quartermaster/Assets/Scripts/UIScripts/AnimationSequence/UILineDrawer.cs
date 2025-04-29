using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UILineDrawer : MonoBehaviour
{
    // Inherited settings.
    private GameObject _camRef;
    private GameObject _highlightObjectRef;
    private GameObject _panelDrawer;


    private float _originOffset = 0.0f;
    private float _lineWidth = 2f;
    private float drawDuration = 1f;

    private float borderClampXMargin = 0.1f; // multiplier of canvas size to clamp the destination point within. 
    private float borderClampYMargin = 0.125f;
    private float destXOffset = 0.12f; // 0-1f, multiplier of canvas size.
    private float destYOffset = 0.08f; // 0-1f, multiplier of canvas size.
    // private Color lineColor = Color.white;

    // Runtime variables.
    public Vector2 origin2d;
    public Vector2 dest2d;
    private float _currScale = 0f; // 0-1f
    public float xOffsetSign = 0f;
    public float yOffsetSign = 0f;


    void Start()
    {
        // Initialize the line with default values
        this.origin2d = Vector2.zero;
        this.dest2d = Vector2.zero;
        this.transform.localScale = new Vector3(0f, 0f, 0f);
    }

    public void Initialize(GameObject cam, GameObject highlightObj, 
                            float originOffset,
                            float lineWidth, 
                            Color lineColor,
                            float duration,
                            GameObject nextAnimation
                            )
    {
        _camRef = cam;
        _highlightObjectRef = highlightObj;
        _panelDrawer = nextAnimation;

        this._lineWidth = lineWidth;
        this.gameObject.GetComponent<Image>().color = lineColor;
        this.drawDuration = duration;

        _originOffset = originOffset;
        UpdateLocalCanvasPosition();
        UpdateLineDraw(_currScale);
    }

    // called once, externally by previous animation in sequence (tooltippableanimated)
    // animates _currScale value.
    public void AnimateDrawLine(){
        LeanTween.value(gameObject, 0f, 1f, drawDuration) 
            .setEase(LeanTweenType.easeInOutCubic)
            .setOnUpdate((float val) =>
            {
                _currScale = val;
            })
            .setOnComplete(() =>
            {
                UIPanelDrawer next = _panelDrawer.GetComponent<UIPanelDrawer>();
                if (next != null)
                {
                    next.AnimatePanel(); // animate the panel after the line is drawn.
                }
                else
                {
                    Debug.LogError("UIPanelDrawer component not found on the next animation object.");
                }
            });
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (_camRef != null && _highlightObjectRef != null){
            UpdateLocalCanvasPosition(); // updates the origin (based on obj) and destination (based on origin, offset & clamped)
            UpdateLineDraw(_currScale); // animates the line
        }
    }


    void UpdateLocalCanvasPosition()
    {
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
            Vector2 temp = GetDestinationPoint(this.transform.parent.GetComponent<Canvas>(), origin2d);
            dest2d = new Vector3 (temp.x, temp.y, 0f);

            Vector3 direction = (dest2d - origin2d).normalized;
            origin2d = new Vector3 (origin2d.x + _originOffset * direction.x, origin2d.y + _originOffset * direction.y, 0f);
        }
    }

    public void UpdateLineDraw(float scale)
    {
        float totalDistance = Vector2.Distance(origin2d, dest2d);
        Vector3 midPoint = new Vector3 ((origin2d.x + dest2d.x) / 2.0f, (origin2d.y + dest2d.y) / 2.0f, 0.0f);
        float x = Mathf.Lerp (origin2d.x, midPoint.x, scale);
        float y = Mathf.Lerp (origin2d.y, midPoint.y, scale);
        Vector3 newPos = new Vector3 (x, y, 0f);

        this.transform.localPosition = newPos; // new Vector3 (newPos.x, newPos.y, 0f);
        this.transform.localScale = new Vector3(totalDistance * scale, _lineWidth, 1f);

        float angle = Mathf.Atan2(dest2d.y - origin2d.y, dest2d.x - origin2d.x) * Mathf.Rad2Deg;
        this.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }

    // Takes origin as a canvas local point. 
    Vector2 GetDestinationPoint(Canvas canvas, Vector2 origin)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;

        if (xOffsetSign == 0f && yOffsetSign == 0f){
            // If no offset is set, we initialize the offset sign.
            // Initialized once and not every call so that the destination doesn't flip back and forth. 
            xOffsetSign = origin.x > 0f ? 1f : -1f;
            yOffsetSign = origin.y > 0f ? 1f : -1f;
        }
        // We offset towards sides of screen to avoid cluttering center of player view.
        float xOffset = xOffsetSign * canvasSize.x * destXOffset;
        float yOffset = yOffsetSign * canvasSize.y * destYOffset; 
        Vector2 dest2d = new Vector2 (origin.x + xOffset, origin.y + yOffset);
        return GetClampedVector(canvas, dest2d);
    }
    
    #region helpers
    #endregion 

    // couldn't get the recttransform utility thing to work so i made this function instead.
    // scales screenpos to canvasrect and adjust for canvas pivot
    // written with help from chatgpt. 
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

    // Takes point as a canvas local point. 
    Vector2 GetClampedVector(Canvas canvas, Vector2 point){
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;

        float aX = -canvasSize.x * canvasRect.pivot.x + borderClampXMargin * canvasSize.x;
        float bX = canvasSize.x * (1 - canvasRect.pivot.x) - borderClampXMargin * canvasSize.x;
        float aY = -canvasSize.y * canvasRect.pivot.y  + borderClampYMargin * canvasSize.y;
        float bY = canvasSize.y * (1 - canvasRect.pivot.y) - borderClampYMargin * canvasSize.y;
        Vector2 clampedPoint = new Vector2(
            Mathf.Clamp(point.x, aX, bX), 
            Mathf.Clamp(point.y, aY, bY)
        );
        return clampedPoint;
    }
}
