using UnityEngine;

public class SoundEmitter : MonoBehaviour {
    public AudioSource audioSource;  // Assign in the Inspector.

    public void PlaySound(AudioClip clip) {
        if (audioSource != null && clip != null) {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
