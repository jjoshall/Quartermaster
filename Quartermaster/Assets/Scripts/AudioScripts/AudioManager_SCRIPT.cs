using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer gameMixer;

    [Header("Listener & Distance Settings")]
    public Transform playerTransform;
    public float minDistance = 5f;
    public float maxDistance = 50f;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void SetMusicVolume(float volume) {
        gameMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume) {
        gameMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }

    public void PlaySoundAtPosition(AudioClip clip, Vector3 sourcePosition) {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = sourcePosition;
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.spatialBlend = 1.0f;
        aSource.minDistance = minDistance;
        aSource.maxDistance = maxDistance;
        
        // Optional: Set output group if you have one (e.g., aSource.outputAudioMixerGroup = yourSFXGroup).
        aSource.Play();
        Destroy(tempGO, clip.length);
    }
}
