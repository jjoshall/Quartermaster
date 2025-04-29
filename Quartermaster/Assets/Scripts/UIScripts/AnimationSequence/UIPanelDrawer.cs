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

    [SerializeField] private float _panelWidth = 0f; // 0-1f. canvas.size as units.
    [SerializeField] private float _panelHeight = 0f; // 0-1f. canvas.size as units.
    // Horizontal scale animation (part 1)
    [SerializeField] private float horizontalDuration = 0.1f;
    private float _currHorizontalScale = 0f; // 0-1f
    // Vertical scale animation (part 2)
    [SerializeField] private float verticalDuration = 0.2f;
    private float _currVerticalScale = 0f; // 0-1f
    // Text animation (part 3)
    [SerializeField] private float textDuration = 0.2f;

    // Child object references
    [SerializeField] private GameObject _borderObj;
    [SerializeField] private GameObject _fillObj;
    [SerializeField] private GameObject _textObj;
    private string _fullText = "";
    private string _currText = "";
    private int _currChars = 0;



    public void Init(string tooltipText, GameObject lineDrawer){
        _fullText = tooltipText;
        _lineParent = lineDrawer;
        _xDirection = lineDrawer.GetComponent<UILineDrawer>().xOffsetSign;
        _yDirection = lineDrawer.GetComponent<UILineDrawer>().yOffsetSign;
        _origin = lineDrawer.GetComponent<UILineDrawer>().dest2d;
        _textObj.GetComponent<TMPro.TextMeshProUGUI>().text = "";

        _borderObj.transform.localScale = new Vector3(0f, 0f, 1f);
        _fillObj.transform.localScale = new Vector3(0f, 0f, 1f);    
        _textObj.transform.localScale = new Vector3(0f, 0f, 1f); // set to 0 scale to hide it.

        UpdateOriginDestination();
        UpdatePosition();
        UpdateScale();
    }

    void UpdateOriginDestination(){
        // Update _origin and _dest based on the current position of the line drawer.
        _origin = _lineParent.GetComponent<UILineDrawer>().dest2d;
        _dest = new Vector2(_origin.x + _panelWidth * _xDirection, _origin.y + _panelHeight * _yDirection);
        
    }

    void UpdatePosition(){
        Vector2 midpoint = (_origin + _dest) / 2f;
        float xPosition = Mathf.Lerp (_origin.x, midpoint.x, _currHorizontalScale);
        float yPosition = Mathf.Lerp (_origin.y, midpoint.y, _currVerticalScale);

        Debug.Log ("origin is " + _origin + ", dest is " + _dest + ", midpoint is " + midpoint + ", xOffset is " + _xDirection + ", yOffset is " + _yDirection + ", x_offsetsign is " + _xDirection + ", y_offsetsign is " + _yDirection);  
        this.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
        // _borderObj.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
        // _fillObj.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
        // _textObj.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
    }

    void UpdateScale(){
        if (_borderObj != null && _fillObj != null && _textObj != null){
            _borderObj.transform.localScale = new Vector3(_currHorizontalScale, _currVerticalScale, 1f);
            _fillObj.transform.localScale = new Vector3(_currHorizontalScale, _currVerticalScale, 1f);
            _textObj.transform.localScale = new Vector3(_currHorizontalScale, _currVerticalScale, 1f);
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
        Debug.Log ("animatePanel() called.");
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
        LeanTween.value(gameObject, 0f, 1f, verticalDuration) 
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
}
