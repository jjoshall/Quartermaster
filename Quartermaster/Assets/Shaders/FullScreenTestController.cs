// Code help from: https://www.youtube.com/watch?v=hqz4TnvC3fQ&t=128s&ab_channel=SasquatchBStudios

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FullScreenTestController : MonoBehaviour
{
     [Header("Time Stats")]
     [SerializeField] private float _hurtDisplayTime = 1.5f;
     [SerializeField] private float _hurtFadeOutTime = 0.5f;

     [Header("References")]
     [SerializeField] private ScriptableRendererFeature _fullScreenDamage;
     [SerializeField] private Material _material;

     [Header("Intensity Stats")]
     [SerializeField] private float _voronoiIntensityStat = 2.5f;
     [SerializeField] private float _vignetteIntensityStat = 1.25f;

     private int _voronoiIntensity = Shader.PropertyToID("_VoronoiIntensity");
     private int _vignetteIntensity = Shader.PropertyToID("_VignetteIntensity");

     private void Start()
     {
          _fullScreenDamage.SetActive(false);
     }

     public IEnumerator Hurt()
     {
          _fullScreenDamage.SetActive(true);
          _material.SetFloat(_voronoiIntensity, _voronoiIntensityStat);
          _material.SetFloat(_vignetteIntensity, _vignetteIntensityStat);

          yield return new WaitForSeconds(_hurtDisplayTime);

          float elapsedTime = 0f;
          while (elapsedTime < _hurtFadeOutTime)
          {
               elapsedTime += Time.deltaTime;

               float lerpVoronoi = Mathf.Lerp(_voronoiIntensityStat, 0f, (elapsedTime / _hurtFadeOutTime));
               float lerpVignette = Mathf.Lerp(_vignetteIntensityStat, 0f, (elapsedTime / _hurtFadeOutTime));

               _material.SetFloat(_voronoiIntensity, lerpVoronoi);
               _material.SetFloat(_vignetteIntensity, lerpVignette);

               yield return null;
          }

          _fullScreenDamage.SetActive(false);
     }
}
