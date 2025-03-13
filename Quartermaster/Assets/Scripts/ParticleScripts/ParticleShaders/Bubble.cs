using UnityEngine;

public class BubbleAnimator : IShaderAnimator
{


    [SerializeField] private float _BUBBLE_SCALE_MIN_RANGE = 0.1f;
    [HideInInspector] public float BUBBLE_SCALE_MAX_RANGE = 1.0f;

    [SerializeField] private float _BUBBLE_OPACITY_MIN_RANGE = 0.0f;
    [SerializeField] private float _BUBBLE_OPACITY_MAX_RANGE = 0.5f;

    private Material[] _playerMaterials;
    public float fadeDuration = 1.0f;
    private float _fadeTimerStart = 0.0f;
    private bool _fadeStarted = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (renderer){
            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++){
                newMaterials[i] = new Material(renderer.materials[i]);
            }
            _playerMaterials = newMaterials;
            renderer.materials = newMaterials;
        } else {
            Debug.LogError("Renderer is not set in the DissolveAnimator script.");
        }

        Animate();
    }

    public override void Animate(){
        _fadeStarted = true;
        _fadeTimerStart = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (_fadeStarted){
            if (Time.time - _fadeTimerStart < fadeDuration){
                InterpolateBubble();
            } else {
                _fadeStarted = false;
                if (loop) Animate();
            }
        }
    }

    void InterpolateBubble(){
        float scale = EaseOutCirc((Time.time - _fadeTimerStart) / fadeDuration, _BUBBLE_SCALE_MIN_RANGE, BUBBLE_SCALE_MAX_RANGE);
        float opacity = EaseOutCirc((Time.time - _fadeTimerStart) / fadeDuration, _BUBBLE_OPACITY_MIN_RANGE, _BUBBLE_OPACITY_MAX_RANGE);
        
        for (int i = 0; i < _playerMaterials.Length; i++){
            _playerMaterials[i].SetFloat("_BlendOpacity", opacity);
            this.gameObject.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    // Ease out interpolation function SOURCE: easings.net
    public static float EaseOutCirc(float x, float min, float max)
    {
        x = Mathf.Clamp01(x);
        float easedValue = Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
        return Mathf.Lerp(min, max, easedValue);
    }
}
