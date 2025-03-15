using UnityEngine;

public class DissolveAnimator : IShaderAnimator
{

    [SerializeField] private float _DISSOLVE_MIN_RANGE = 0.0f;
    [SerializeField] private float _DISSOLVE_MAX_RANGE = 1.2f;
    [SerializeField] private float _NOISESCALE_MIN_RANGE = 10.0f;
    [SerializeField] private float _NOISESCALE_MAX_RANGE = 94.0f;

    private Material[] _playerMaterials;
    public float dissolveDuration = 1.0f;
    private float _dissolveTimerStart = 0.0f;
    private bool _dissolveStarted = false;
    
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
        _dissolveStarted = true;
        _dissolveTimerStart = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (_dissolveStarted){
            if (Time.time - _dissolveTimerStart < dissolveDuration){
                LerpDissolveAmount();
            } else {
                _dissolveStarted = false;
                if (loop) Animate();
            }
        }
    }

    void LerpDissolveAmount(){
        float dissolveAmount = Mathf.Lerp(_DISSOLVE_MIN_RANGE, _DISSOLVE_MAX_RANGE, (Time.time - _dissolveTimerStart) / dissolveDuration);
        float noiseScale = Mathf.Lerp(_NOISESCALE_MIN_RANGE, _NOISESCALE_MAX_RANGE, dissolveAmount);
        
        for (int i = 0; i < _playerMaterials.Length; i++){
            _playerMaterials[i].SetFloat("_DissolveStrength", dissolveAmount);
            _playerMaterials[i].SetFloat("_NoiseScale", noiseScale);
        }
    }
}
