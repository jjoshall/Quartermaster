using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITargetCircle : MonoBehaviour
{
    [SerializeField] private Color _primary = Color.white;
    [SerializeField] private Color _panelFill = Color.white;

    [Header("CircleSettings")]
    public Image radialImage;
    public float fillDuration = 1f;

    [Header("LineSettings")]
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private float borderClampXMargin = 0.1f; // multiplier of canvas size to clamp the destination point within. 
    [SerializeField] private float borderClampYMargin = 0.125f;
    [SerializeField] private float destXOffset = 0.12f; // 0-1f, multiplier of canvas size.
    [SerializeField] private float destYOffset = 0.08f; // 0-1f, multiplier of canvas size.
    [SerializeField] private float _lineDrawDuration = 0.5f;

    [Header("PanelSettings")]
    // height, width, drawduration, color
    [SerializeField] private float _panelWidth = 0f; // 0-1f. canvas.size as units.
    [SerializeField] private float _panelHeight = 0f; // 0-1f. canvas.size as units.
    // Horizontal scale animation (part 1)
    [SerializeField] private float horizontalDuration = 0.1f;
    // Vertical scale animation (part 2)
    [SerializeField] private float verticalDuration = 0.2f;
    [SerializeField] private float _initVerticalScale = 0.1f;
    // Text animation (part 3)
    [SerializeField] private float textDuration = 0.2f;


    private GameObject _camRef;                  
    private GameObject _highlightObjectRef;

    private Vector3 _lineOrigin;
    private Vector3 _lineDestination;

    [SerializeField] private GameObject lineRendererPrefab;
    [SerializeField] private GameObject tooltipPanelPrefab;
    private GameObject tooltippableLine; 
    private GameObject tooltippablePanel;

    void OnDestroy()
    {
        if (tooltippableLine != null)
        {
            Destroy(tooltippableLine);
        }
        if (tooltippablePanel != null)
        {
            Destroy(tooltippablePanel);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        radialImage.fillAmount = 0f;
        this.GetComponent<Image>().color = _primary;
    }

    public void Initialize(GameObject cam, GameObject highlightObj, string text, int fontSize){
        _camRef = cam;
        // UIManager.instance.playerDrawCanvas.worldCamera = cam.GetComponent<Camera>();
        _highlightObjectRef = highlightObj;
        this.transform.localScale = new Vector3 (1f, 1f, 0f);
        tooltippableLine = Instantiate(lineRendererPrefab, this.transform.parent);
        tooltippablePanel = Instantiate(tooltipPanelPrefab, this.transform.parent);

        float originOffset = radialImage.GetComponent<RectTransform>().rect.width / 2f;
        Debug.Log ("originOffset is " + originOffset);
        tooltippableLine.GetComponent<UILineDrawer>().Initialize(_camRef, _highlightObjectRef, 
                                                                originOffset,
                                                                _lineWidth, _primary,
                                                                _lineDrawDuration, 
                                                                borderClampXMargin,
                                                                borderClampYMargin,
                                                                destXOffset,
                                                                destYOffset,
                                                                tooltippablePanel);
        tooltippablePanel.GetComponent<UIPanelDrawer>().Init(text, tooltippableLine,
                                                                _panelWidth, _panelHeight, 
                                                                horizontalDuration, verticalDuration, 
                                                                _initVerticalScale, textDuration, fontSize);
                                                                
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
