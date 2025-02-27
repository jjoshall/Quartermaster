using UnityEngine;

public class SoundEmitter : MonoBehaviour {
    public AudioSource audioSource;
    public void PlaySound(AudioClip clip) {
        if (audioSource != null && clip != null) {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
