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
    [SerializeField] private GameObject _textObj;
    private string _fullText = "";
    private string _currText = "";
    private int _currChars = 0;

    public void Init(string tooltipText, GameObject lineDrawer,
                        float panelWidth, float panelHeight, 
                        float horizontalDuration, float verticalDuration, 
                        float initVerticalScale, float textDuration, float fontSize)
                    {
        _fullText = tooltipText;
        _lineParent = lineDrawer;
        _xDirection = lineDrawer.GetComponent<UILineDrawer>().xOffsetSign;
        _yDirection = lineDrawer.GetComponent<UILineDrawer>().yOffsetSign;
        _origin = lineDrawer.GetComponent<UILineDrawer>().dest2d;
        _textObj.GetComponent<TMPro.TextMeshProUGUI>().text = "";
        _currVerticalScale = _initVerticalScale;

        _borderObj.GetComponent<RectTransform>().sizeDelta = new Vector2 (0, 0);
        _fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2 (0, 0);
        _textObj.GetComponent<RectTransform>().sizeDelta = new Vector2 (0, 0);

        this._panelWidth = panelWidth;
        this._panelHeight = panelHeight;
        this.horizontalDuration = horizontalDuration;
        this.verticalDuration = verticalDuration;
        this._initVerticalScale = initVerticalScale;
        this.textDuration = textDuration;
        _textObj.GetComponent<TMPro.TextMeshProUGUI>().fontSize = fontSize;

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

        if (_borderObj != null && _fillObj != null && _textObj != null){
            RectTransform rt1 = _borderObj.GetComponent<RectTransform>();
            RectTransform rt2 = _fillObj.GetComponent<RectTransform>();     
            RectTransform rt3 = _textObj.GetComponent<RectTransform>();
            if (rt1 == null || rt2 == null || rt3 == null) return; // safety check
            rt1.sizeDelta = new Vector2(_panelWidth * canvasSize.x * _currHorizontalScale, _panelHeight * canvasSize.y * _currVerticalScale);
            rt2.sizeDelta = new Vector2(_panelWidth * canvasSize.x * _currHorizontalScale, _panelHeight * canvasSize.y * _currVerticalScale);
            rt3.sizeDelta = new Vector2(_panelWidth * canvasSize.x * _currHorizontalScale, _panelHeight * canvasSize.y * _currVerticalScale);
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
                AnimateText();
            });
    }
    private void AnimateText(){
        LeanTween.value(gameObject, 0, _fullText.Length, verticalDuration) 
            .setOnUpdate((float val) =>
            {
                _currChars = (int) (val * _fullText.Length);
                _currChars = Mathf.Clamp(_currChars, 0, _fullText.Length); // safety clamp
                _currText = _fullText.Substring(0, _currChars);
                _textObj.GetComponent<TMPro.TextMeshProUGUI>().text = _currText;
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
