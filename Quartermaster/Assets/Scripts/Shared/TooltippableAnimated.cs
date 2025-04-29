using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TooltippableAnimated : MonoBehaviour
{
    [Header("CircleSettings")]
    public Image radialImage;
    public float fillDuration = 1f;

    [Header("LineSettings")]
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private Color _lineColor = Color.white;
    [SerializeField] private float _lineDrawDuration = 0.5f;

    [Header("PanelSettings")]
    // height, width, drawduration, color



    private GameObject _camRef;                  
    private GameObject _highlightObjectRef;

    private Vector3 _lineOrigin;
    private Vector3 _lineDestination;

    private GameObject tooltippableLine; 
    private GameObject tooltippablePanel; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        radialImage.fillAmount = 0f;
    }

    public void Initialize(GameObject cam, GameObject highlightObj,
                            GameObject line, 
                            GameObject panel, string tooltipText){
        _camRef = cam;
        // UIManager.instance.playerDrawCanvas.worldCamera = cam.GetComponent<Camera>();
        _highlightObjectRef = highlightObj;
        this.transform.localScale = new Vector3 (1f, 1f, 0f);
        tooltippableLine = line;
        tooltippablePanel = panel;
        AnimateTargetCircle();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (_camRef != null && _highlightObjectRef != null){
            UpdateLocalCanvasPosition();
        }
        if (_lineOrigin != null && _lineDestination != null){
            UpdateLineRenderers();
        }
    }

    void AnimateTargetCircle(){
        Debug.Log ("AnimateFill() called.");
        LeanTween.value(radialImage.gameObject, 0f, 1f, fillDuration)
            .setEase(LeanTweenType.easeOutCubic)
            .setOnUpdate((float val) =>
            {
                radialImage.fillAmount = val;
            })
            .setOnComplete(() =>
            {
                // Step 2: Line draw animation
                AnimateLineRenderer();
            });
    }

    void AnimateLineRenderer(){
        float originOffset = radialImage.GetComponent<RectTransform>().rect.width / 2f;
        Debug.Log ("originOffset is " + originOffset);
        tooltippableLine.GetComponent<UILineDrawer>().Initialize(_camRef, _highlightObjectRef, 
                                                                _lineOrigin, originOffset,
                                                                _lineDestination,
                                                                _lineWidth, _lineColor,
                                                                _lineDrawDuration);
        tooltippableLine.GetComponent<UILineDrawer>()?.AnimateDrawLine();
        Debug.Log ("AnimateLineRenderer() called.");
    }

    // void AnimateFillPanel(){
    //     Debug.Log ("AnimateFillPanel() called.");
    //     LeanTween.scale(this.gameObject, new Vector3(1f, 1f, 1f), fillDuration)
    //         .setOnComplete(() =>
    //         {
    //             // Step 2: Line draw animation
    //             AnimateLineRenderer();
    //         });
    // }

    #region CameraUpdates
    #endregion
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
            transform.localPosition = new Vector3 (localPoint.x, localPoint.y, 0f); 

            _lineOrigin = new Vector3 (localPoint.x, localPoint.y, 0f);
            _lineDestination = new Vector3 (200f   , 400f, 0);
        }
    }


    void UpdateLineRenderers(){
        // _lineOrigin = new Vector3 (transform.localPosition.x, transform.localPosition.y, 0f); // Assuming this is the origin position
        // _lineDestination = new Vector3 (_highlightObjectRef.transform.localPosition.x, _highlightObjectRef.transform.localPosition.y, 0f); // Assuming this is the target position

        // UILineRenderer lr = tooltippableLine.GetComponent<UILineRenderer>();
        // if (lr == null) return;

        // lr.points.Clear(); // Clear previous points
        // lr.points.Add(new Vector2(_lineOrigin.x, _lineOrigin.y)); // Start point
        // lr.points.Add(new Vector2(_lineDestination.x, _lineDestination.y)); // End point
        // lr.SetVerticesDirty(); // Mark the line renderer as dirty to update it
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
