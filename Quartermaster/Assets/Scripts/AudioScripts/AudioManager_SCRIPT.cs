using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Audio;
using NUnit.Framework.Constraints;

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

    // For testing local sound playback via Addressables when pressing K.
    private void Update() {
        if (Input.GetKeyDown(KeyCode.L)) {
            DebugMixerVolumes();
        }
    }

    public void DebugMixerVolumes() {
        float musicVol, sfxVol, masterVol;

        if (gameMixer.GetFloat("MusicVolume", out musicVol)) {
            Debug.Log($"[AudioManager] Music Volume: {musicVol} dB");
        } else {
            Debug.LogWarning("[AudioManager] MusicVolume parameter not found in Audio Mixer.");
        }

        if (gameMixer.GetFloat("SFXVolume", out sfxVol)) {
            Debug.Log($"[AudioManager] SFX Volume: {sfxVol} dB");
        } else {
            Debug.LogWarning("[AudioManager] SFXVolume parameter not found in Audio Mixer.");
        }

        if (gameMixer.GetFloat("MasterVolume", out masterVol)) {
            Debug.Log($"[AudioManager] Master Volume: {masterVol} dB");
        } else {
            Debug.LogWarning("[AudioManager] MasterVolume parameter not found in Audio Mixer.");
        }
    }


    public void TestPlaySound() {
        // Here we use the Addressable key directly.
        string audioKey = "Audio/pew.ogg";
        // Use AudioLibrary to load asynchronously.
        AudioLibrary.GetClipAsync(audioKey, (clip) => {
            if (clip != null) {
                PlaySoundAtPosition(clip, playerTransform.position);
            }
        });
    }

    // Volume control methods
    public void SetMusicVolume(float volume) {
        float dbVolume = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        gameMixer.SetFloat("MusicVolume", dbVolume);
        Debug.Log($"[AudioManager] Set Music Volume: {dbVolume} dB");
    }

    public void SetSFXVolume(float volume) {
        float dbVolume = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        gameMixer.SetFloat("SFXVolume", dbVolume);
        Debug.Log($"[AudioManager] Set SFX Volume: {dbVolume} dB");
    }

    public void SetMasterVolume(float volume) {
        float dbVolume = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
        gameMixer.SetFloat("MasterVolume", dbVolume);
        Debug.Log($"[AudioManager] Set Master Volume: {dbVolume} dB");
    }
    // Plays the clip at the given position with 3D settings.
    public void PlaySoundAtPosition(AudioClip clip, Vector3 sourcePosition) {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = sourcePosition;
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.spatialBlend = 1.0f;
        aSource.minDistance = minDistance;
        aSource.maxDistance = maxDistance;
        // Route this AudioSource to the SFX group.
        aSource.outputAudioMixerGroup = gameMixer.FindMatchingGroups("SFX")[0];

        aSource.Play();
        Destroy(tempGO, clip.length);
    }
}
