using Unity.Netcode;
using UnityEngine;

public class NetworkAudio : NetworkBehaviour {
    [ClientRpc]
    public void PlaySoundClientRpc(string soundName, Vector3 soundPosition) {
        AudioClip clip = AudioLibrary.GetClip(soundName);
        if (clip != null && AudioManager.Instance != null) {
            AudioManager.Instance.PlaySoundAtPosition(clip, soundPosition);
        }
    }
}
