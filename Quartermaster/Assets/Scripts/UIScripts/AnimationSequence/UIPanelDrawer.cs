using Unity.VisualScripting;
using UnityEngine;

public class UIPanelDrawer : MonoBehaviour
{
    // Inherited settings.
    private Vector2 _origin;
    private GameObject _lineParent;
    private float _xDirection = 0f;
    private float _yDirection = 0f;

    // Runtime stuff.
    private Vector2 _dest; // calculated based on origin and panel size

    private float _panelWidth = 0f; // 0-1f. canvas.size as units.
    private float _panelHeight = 0f; // 0-1f. canvas.size as units.
    // Horizontal scale animation (part 1)
    private float horizontalDuration = 0.1f;
    private float _currHorizontalScale = 0f; // 0-1f
    // Vertical scale animation (part 2)
    private float verticalDuration = 0.2f;
    private float _initVerticalScale = 0.1f;
    private float _currVerticalScale = 0.05f; // init value determines horizontal line thickness.
    // Text animation (part 3)
    private float textDuration = 0.2f;

    // Child object references
    [SerializeField] private GameObject _borderObj;
    [SerializeField] private GameObject _fillObj;
    [SerializeField] private GameObject _headerTextObj;
    private string _fullHeaderText = "";
    private string _currHeaderText = "";
    private int _currHeaderChars = 0;

    [SerializeField] private GameObject _bodyTextObj;
    private string _fullBodyText = "";
    private string _currBodyText = "";
    private int _currBodyChars = 0;

    public void Init(string headerTooltipText, string bodyTooltipText, GameObject lineDrawer,
                        float panelWidth, float panelHeight, 
                        float horizontalDuration, float verticalDuration, 
                        float initVerticalScale, float textDuration, float headerFontSize, float bodyFontSize)
                    {
        _fullHeaderText = headerTooltipText;
        _fullBodyText = bodyTooltipText;

        _lineParent = lineDrawer;
        _xDirection = lineDrawer.GetComponent<UILineDrawer>().xOffsetSign;
        _yDirection = lineDrawer.GetComponent<UILineDrawer>().yOffsetSign;
        _origin = lineDrawer.GetComponent<UILineDrawer>().dest2d;
        _headerTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = "";
        _bodyTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = ""; // reset text to empty string.

        _currVerticalScale = _initVerticalScale;

        RectTransform canvasRt = this.transform.parent.GetComponent<RectTransform>();
        if (canvasRt == null) return; // safety check
        Vector2 canvasSize = canvasRt.sizeDelta;

        // Set size of all child objects in the panel to init sizes (0). 
        Vector2 panelSize = new Vector2(_panelWidth * canvasSize.x, _panelHeight * canvasSize.y);
        _borderObj.GetComponent<RectTransform>().sizeDelta = new Vector2 (0, 0);
        _fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2 (0, 0);
        _headerTextObj.GetComponent<RectTransform>().sizeDelta = panelSize; // text objs can be full size as they're animated by char
        _bodyTextObj.GetComponent<RectTransform>().sizeDelta = panelSize;

        this._panelWidth = panelWidth;
        this._panelHeight = panelHeight;
        this.horizontalDuration = horizontalDuration;
        this.verticalDuration = verticalDuration;
        this._initVerticalScale = initVerticalScale;
        this.textDuration = textDuration;
        _headerTextObj.GetComponent<TMPro.TextMeshProUGUI>().fontSize = headerFontSize;
        _bodyTextObj.GetComponent<TMPro.TextMeshProUGUI>().fontSize = bodyFontSize;

        UpdateOriginDestination();
        UpdatePosition();
        UpdateScale();
    }

    void UpdateOriginDestination(){
        Canvas canvas = this.transform.parent.GetComponent<Canvas>();
        if (canvas == null) return; // safety check
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return; // safety check
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Update _origin and _dest based on the current position of the line drawer.
        _origin = _lineParent.GetComponent<UILineDrawer>().dest2d;
        _dest = new Vector2(_origin.x + canvasSize.x * _panelWidth * _xDirection, 
                            _origin.y + canvasSize.y * _panelHeight * _yDirection);
        
    }

    void UpdatePosition(){
        Vector2 midpoint = (_origin + _dest) / 2f; // midpoint between origin and destination
        float xPosition = Mathf.Lerp (_origin.x, midpoint.x, _currHorizontalScale);
        float yPosition = Mathf.Lerp (_origin.y, midpoint.y, _currVerticalScale);
        this.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
    }

    void UpdateScale(){
        RectTransform canvasRt = this.transform.parent.GetComponent<RectTransform>();
        if (canvasRt == null) return; // safety check
        Vector2 canvasSize = canvasRt.sizeDelta;

        if (_borderObj != null && _fillObj != null){
            RectTransform rt1 = _borderObj.GetComponent<RectTransform>();
            RectTransform rt2 = _fillObj.GetComponent<RectTransform>();    
            RectTransform rt3 = _headerTextObj.GetComponent<RectTransform>();
            RectTransform rt4 = _bodyTextObj.GetComponent<RectTransform>(); 
            if (rt1 == null || rt2 == null || rt3 == null || rt4 == null) return; // safety check

            // Calculate panel size
            float panelWidth = _panelWidth * canvasSize.x * _currHorizontalScale;
            float panelHeight = _panelHeight * canvasSize.y * _currVerticalScale;

            // Apply size to border and fill
            rt1.sizeDelta = new Vector2(panelWidth, panelHeight);
            rt2.sizeDelta = new Vector2(panelWidth, panelHeight);

            // Compute header and body heights
            float headerHeight = 0.9f * panelHeight * 0.2f; // 1/5 of total
            float bodyHeight = 0.9f * panelHeight - headerHeight;
                        
            // Apply sizes
            rt3.sizeDelta = new Vector2(panelWidth * 0.9f, headerHeight);
            rt4.sizeDelta = new Vector2(panelWidth * 0.9f, bodyHeight);

            // Position header centered vertically within its area
            rt3.anchoredPosition = new Vector2(0, bodyHeight / 2);

            // Position body centered vertically within its area
            rt4.anchoredPosition = new Vector2(0, -(headerHeight / 2));
        }   
    }

    void LateUpdate()
    {
        UpdateOriginDestination();
        UpdatePosition();
        UpdateScale();
    }


    // Entry point.
    public void AnimatePanel(){
        AnimateHorizontalScale();
    }


    private void AnimateHorizontalScale(){
        LeanTween.value(gameObject, 0f, 1f, horizontalDuration) 
            // .setEase(LeanTweenType.easeInOutCubic)
            .setOnUpdate((float val) =>
            {
                _currHorizontalScale = val;
            })
            .setOnComplete(() =>
            {
                AnimateVerticalScale();
            });
    }

    private void AnimateVerticalScale(){
        LeanTween.value(gameObject, _initVerticalScale, 1f, verticalDuration) 
            .setEase(LeanTweenType.easeOutCubic)
            .setOnUpdate((float val) =>
            {
                _currVerticalScale = val;
            })
            .setOnComplete(() =>
            {
                AnimateHeader();
            });
    }
    private void AnimateHeader(){
        int totalChar = _fullHeaderText.Length + _fullBodyText.Length;
        if (totalChar == 0) return; // safety check
        float headerDuration = textDuration * _fullBodyText.Length / totalChar;

        LeanTween.value(gameObject, 0, _fullHeaderText.Length, headerDuration) 
            .setOnUpdate((float val) =>
            {
                _currHeaderChars = Mathf.Clamp(Mathf.FloorToInt(val), 0, _fullHeaderText.Length);
                _currHeaderText = _fullHeaderText.Substring(0, _currHeaderChars);
                _headerTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = _currHeaderText;
                // _currHeaderChars = (int) (val * _fullHeaderText.Length);
                // _currHeaderChars = Mathf.Clamp(_currHeaderChars, 0, _fullHeaderText.Length); // safety clamp
                // _currHeaderText = _fullHeaderText.Substring(0, _currHeaderChars);
                // _headerTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = _currHeaderText;
            })
            .setOnComplete(() =>
            {
                AnimateBodyText();
            });
    }

    private void AnimateBodyText(){
        int totalChar = _fullHeaderText.Length + _fullBodyText.Length;
        if (totalChar == 0) return; // safety check
        float bodyDuration = textDuration * _fullBodyText.Length / totalChar;
        
        LeanTween.value(gameObject, 0, _fullBodyText.Length, bodyDuration) 
            .setOnUpdate((float val) =>
            {
                _currBodyChars = Mathf.Clamp(Mathf.FloorToInt(val), 0, _fullBodyText.Length);
                _currBodyText = _fullBodyText.Substring(0, _currBodyChars);
                _bodyTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = _currBodyText;
                // _currBodyChars = (int) (val * _fullBodyText.Length);
                // _currBodyChars = Mathf.Clamp(_currBodyChars, 0, _fullBodyText.Length); // safety clamp
                // _currBodyText = _fullBodyText.Substring(0, _currBodyChars);
                // _bodyTextObj.GetComponent<TMPro.TextMeshProUGUI>().text = _currBodyText;
            })
            .setOnComplete(() =>
            {
                Debug.Log ("AnimateText() complete.");
            });

    }

    
    // private void SetMinVerticalScale(){
    //     RectTransform canvasRt = this.transform.parent.GetComponent<RectTransform>();
    //     if (canvasRt == null) return; // safety check
    //     Vector2 canvasSize = canvasRt.sizeDelta;
    //     RectTransform rt1 = _borderObj.GetComponent<RectTransform>();
    //     RectTransform rt2 = _fillObj.GetComponent<RectTransform>();     
    //     RectTransform rt3 = _textObj.GetComponent<RectTransform>();
    //     if (rt1 == null || rt2 == null || rt3 == null) return; // safety check
    //     Debug.Log ("Sizes before: " + rt1.sizeDelta + " " + rt2.sizeDelta + " " + rt3.sizeDelta);
    //     rt1.sizeDelta = new Vector2(0, _panelHeight * canvasSize.y * _minVerticalScale);
    //     rt2.sizeDelta = new Vector2(0, _panelHeight * canvasSize.y * _minVerticalScale);
    //     rt3.sizeDelta = new Vector2(0, _panelHeight * canvasSize.y * _minVerticalScale);
    //     Debug.Log ("Sizes after: " + rt1.sizeDelta + " " + rt2.sizeDelta + " " + rt3.sizeDelta);
    // }
}
