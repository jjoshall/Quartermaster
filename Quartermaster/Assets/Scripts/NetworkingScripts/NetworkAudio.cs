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
}
