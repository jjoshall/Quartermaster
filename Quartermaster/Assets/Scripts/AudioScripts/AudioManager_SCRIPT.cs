using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

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
    public void PlaySoundAtPosition(AudioClip clip, Vector3 sourcePosition, string destinationMixer, bool isLooped = false) {
        //Debug.Log("Before temp audio created");
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = sourcePosition;
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.spatialBlend = 1.0f;
        aSource.minDistance = minDistance;
        aSource.maxDistance = maxDistance;
        aSource.loop = isLooped;
        // Route this AudioSource to the SFX group.
        aSource.outputAudioMixerGroup = gameMixer.FindMatchingGroups(destinationMixer)[0];

        //Debug.Log("[AudioManager] Playing sound: " + clip.name);

        aSource.Play();
        //Debug.Log("After temp audio played");
        if (!isLooped) {
            Destroy(tempGO, clip.length);
        }
    }

    public void DestroySoundByGameObject() {
        
    }
}
