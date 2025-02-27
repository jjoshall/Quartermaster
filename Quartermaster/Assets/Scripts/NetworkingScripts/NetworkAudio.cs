using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkAudio : NetworkBehaviour {
    [ClientRpc]
    public void PlaySoundClientRpc(string soundAddressableKey, Vector3 soundPosition) {
        if (!IsClient) { return; }

        AudioLibrary.GetClipAsync(soundAddressableKey, (clip) => {
            if (clip != null && AudioManager.Instance != null) {
                AudioManager.Instance.PlaySoundAtPosition(clip, soundPosition);
            } else {
                Debug.LogWarning("Clip not found or AudioManager missing. Check your Addressable key and AudioManager setup.");
            }
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSoundServerRpc(string soundAddressableKey, Vector3 soundPosition) {
        if (!IsServer) { return; }
        PlaySoundClientRpc(soundAddressableKey, soundPosition);
    }
}
