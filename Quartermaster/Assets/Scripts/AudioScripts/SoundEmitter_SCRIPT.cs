using Unity.Netcode;
using UnityEngine;

public class SoundEmitter : MonoBehaviour {
    public AudioSource audioSource;
    private NetworkAudio networkAudio;

    private void Awake() {
        networkAudio = GetComponent<NetworkAudio>(); 
    }

    public void PlayNetworkedSound(string soundAddressableKey, Vector3 soundPosition) {
        if (networkAudio != null) {
            networkAudio.RequestSoundServerRpc(soundAddressableKey, soundPosition);
        } else {
            Debug.LogError("[SoundEmitter] No NetworkAudio component found!");
        }
    }
}
