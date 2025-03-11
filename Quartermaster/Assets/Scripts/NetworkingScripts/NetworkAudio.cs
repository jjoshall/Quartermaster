using Unity.Netcode;
using UnityEngine;

public class NetworkAudio : NetworkBehaviour {
    [ClientRpc]
    public void PlaySoundClientRpc(string soundAddressableKey, Vector3 soundPosition, string destinationMixer) {
        if (!IsClient) { return; }
        AudioLibrary.GetClipAsync(soundAddressableKey, (clip) => {
            if (clip != null && AudioManager.Instance != null) {
                Debug.Log("[NetworkAudio] Playing sound: " + soundAddressableKey);
                AudioManager.Instance.PlaySoundAtPosition(clip, soundPosition, destinationMixer);
            } else {
                Debug.LogWarning("Clip not found or AudioManager missing. Check your Addressable key and AudioManager setup.");
            }
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSoundServerRpc(string soundAddressableKey, Vector3 soundPosition, string destinationMixer) {
        if (!IsServer) { return; }
        Debug.Log("got to inside RequestSoundServerRpc");
        PlaySoundClientRpc(soundAddressableKey, soundPosition, destinationMixer);
        Debug.Log("got to after PlaySoundClientRpc");
    }

    // --- New methods for looping sounds ---

    [ClientRpc]
    public void StartLoopedSoundClientRpc(string soundAddressableKey, Vector3 soundPosition, string destinationMixer) {
        if (!IsClient) { return; }
        AudioLibrary.GetClipAsync(soundAddressableKey, (clip) => {
            if (clip != null && AudioManager.Instance != null) {
                Debug.Log("[NetworkAudio] Starting looped sound: " + soundAddressableKey);
                AudioManager.Instance.PlayLoopingSoundAtPosition(clip, soundPosition, destinationMixer);
            } else {
                Debug.LogWarning("Clip not found or AudioManager missing for looping sound. Check your Addressable key and setup.");
            }
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestLoopedSoundServerRpc(string soundAddressableKey, Vector3 soundPosition, string destinationMixer) {
        if (!IsServer) { return; }
        StartLoopedSoundClientRpc(soundAddressableKey, soundPosition, destinationMixer);
    }

    [ClientRpc]
    public void StopLoopedSoundClientRpc(string soundAddressableKey, Vector3 soundPosition, string destinationMixer) {
        if (!IsClient) { return; }
        if (AudioManager.Instance != null) {
            Debug.Log("[NetworkAudio] Stopping looped sound: " + soundAddressableKey);
            AudioManager.Instance.StopLoopingSoundAtPosition(soundAddressableKey, soundPosition, destinationMixer);
        } else {
            Debug.LogWarning("AudioManager is missing, cannot stop looping sound.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopLoopedSoundServerRpc(string soundAddressableKey, Vector3 soundPosition, string destinationMixer) {
        if (!IsServer) { return; }
        StopLoopedSoundClientRpc(soundAddressableKey, soundPosition, destinationMixer);
    }
}
