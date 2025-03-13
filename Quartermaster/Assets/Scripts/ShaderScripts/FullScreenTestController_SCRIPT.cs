// Code help from: https://www.youtube.com/watch?v=hqz4TnvC3fQ&t=128s&ab_channel=SasquatchBStudios

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;

public class FullScreenTestController : MonoBehaviour {
    [Header("Fade Stats")]
    [SerializeField] private float _hurtDisplayTime = 1.5f;
    [SerializeField] private float _hurtFadeOutTime = 0.5f;

    [Header("Critical Health Effect")]
    [SerializeField] private float _criticalPulseSpeed = 0.5f;
    [SerializeField] private float _criticalMinIntensity = 0.3f;
    [SerializeField] private float _criticalMaxIntensity = 1.0f;

    [Header("References")]
    [SerializeField] private ScriptableRendererFeature _fullScreenDamage;
    [SerializeField] private Material _material;

    [Header("Intensity Stats")]
    [SerializeField] private float _voronoiIntensityStat = 2.5f;
    [SerializeField] private float _vignetteIntensityStat = 1.25f;

    private int _voronoiIntensity;
    private int _vignetteIntensity;

    private Coroutine _hurtCoroutine;
    private Coroutine _criticalCoroutine;
    private bool _isCritical = false;

    private void Awake() {
        _voronoiIntensity = Shader.PropertyToID("_VoronoiIntensity");
        _vignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
    }

    private void Start() {
        _fullScreenDamage.SetActive(false);
    }

    public void SetCriticalState(bool isCritical) {
        if (_isCritical == isCritical) return;

        _isCritical = isCritical;

        if (_criticalCoroutine != null)
            StopCoroutine(_criticalCoroutine);

        if (isCritical) {
            _fullScreenDamage.SetActive(true);
            _criticalCoroutine = StartCoroutine(PulseCriticalEffect());
        }
        else {
            _criticalCoroutine = StartCoroutine(FadeOutCritical());
        }
    }

    public IEnumerator Hurt() {
        // If we're already showing effect, spike intensity
        if (_isCritical) {
            float originalVoronoi = _material.GetFloat(_voronoiIntensity);
            float originalVignette = _material.GetFloat(_vignetteIntensity);

            _material.SetFloat(_voronoiIntensity, _voronoiIntensityStat);
            _material.SetFloat(_vignetteIntensity, _vignetteIntensityStat);

            yield return new WaitForSeconds(0.2f); // Brief spike

            // Return to pulsing values
            _material.SetFloat(_voronoiIntensity, originalVoronoi);
            _material.SetFloat(_vignetteIntensity, originalVignette);
            yield break;
        }

        // Regular hurt effect
        if (_hurtCoroutine != null)
            StopCoroutine(_hurtCoroutine);

        _fullScreenDamage.SetActive(true);
        _material.SetFloat(_voronoiIntensity, _voronoiIntensityStat);
        _material.SetFloat(_vignetteIntensity, _vignetteIntensityStat);

        yield return new WaitForSeconds(_hurtDisplayTime);

        float elapsedTime = 0f;
        while (elapsedTime < _hurtFadeOutTime) {
            elapsedTime += Time.deltaTime;
            float lerpVoronoi = Mathf.Lerp(_voronoiIntensityStat, 0f, (elapsedTime / _hurtFadeOutTime));
            float lerpVignette = Mathf.Lerp(_vignetteIntensityStat, 0f, (elapsedTime / _hurtFadeOutTime));

            _material.SetFloat(_voronoiIntensity, lerpVoronoi);
            _material.SetFloat(_vignetteIntensity, lerpVignette);

            yield return null;
        }

        _fullScreenDamage.SetActive(false);
        _hurtCoroutine = null;
    }

    private IEnumerator PulseCriticalEffect() {
        float time = 0f;

        while (_isCritical) {
            time += Time.deltaTime;

            // Calculate pulse intensity using sine wave
            float pulseValue = Mathf.Lerp(_criticalMinIntensity, _criticalMaxIntensity,
                                        (Mathf.Sin(time * _criticalPulseSpeed * Mathf.PI * 2) + 1) * 0.5f);

            _material.SetFloat(_voronoiIntensity, pulseValue);
            _material.SetFloat(_vignetteIntensity, pulseValue * 0.5f); // Less intense vignette

            yield return null;
        }
    }

    private IEnumerator FadeOutCritical() {
        float elapsedTime = 0f;
        float startVoronoi = _material.GetFloat(_voronoiIntensity);
        float startVignette = _material.GetFloat(_vignetteIntensity);

        while (elapsedTime < _hurtFadeOutTime) {
            elapsedTime += Time.deltaTime;

            float lerpVoronoi = Mathf.Lerp(startVoronoi, 0f, (elapsedTime / _hurtFadeOutTime));
            float lerpVignette = Mathf.Lerp(startVignette, 0f, (elapsedTime / _hurtFadeOutTime));

            _material.SetFloat(_voronoiIntensity, lerpVoronoi);
            _material.SetFloat(_vignetteIntensity, lerpVignette);

            yield return null;
        }

        _fullScreenDamage.SetActive(false);
        _criticalCoroutine = null;
    }
}
