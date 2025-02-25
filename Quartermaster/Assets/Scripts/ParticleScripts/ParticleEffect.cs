using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    #region = Variables
    private float _timeSinceSpawn = 0.0f;
    private string _particleType = "";
    private float _playDuration = 0.0f; // Inspector setting. Set this for each particle prefab.

    private bool _isPlaying = false;
    #endregion

    // ==============================================================================================
    #region = UnityFuncs
    void Start()
    {
        
    }
    void Update()
    {
        if (_isPlaying)
            expirationTimer();
    }
    #endregion

    // ==============================================================================================
    #region = Helpers
    public void InitializeParticle(string key, float duration){ // called by ParticleManager, which knows key.
        _particleType = key;                    // setting particleType here to avoid having to set for each prefab.
        _timeSinceSpawn = 0.0f;
        _isPlaying = true;
        _playDuration = duration;
    }
    private void expirationTimer()
    {
        _timeSinceSpawn += Time.deltaTime;
        if (_timeSinceSpawn >= _playDuration)
        {
            _isPlaying = false;
            _timeSinceSpawn = 0.0f;
            ParticleManager.instance.DespawnParticleLocal(_particleType, gameObject);
        }
    }
    #endregion
}
