using Unity.VisualScripting;
using UnityEngine;

public class SoundEmitter : MonoBehaviour {
    private NetworkAudio networkAudio;

    public string emitterID;
    [SerializeField] string destinationMixer;

    [SerializeField] string[] soundAddressableKeys;

    private void Awake() {
        networkAudio = GetComponent<NetworkAudio>(); 
    }

    public void PlayNetworkedSound(string soundAddressableKey, Vector3 soundPosition) {
        if (networkAudio == null) {
            Debug.LogError("[SoundEmitter] No NetworkAudio component found!");
            return;
        }

        if (soundAddressableKeys == null || soundAddressableKeys.Length == 0) {
            Debug.LogError($"[SoundEmitter] No sound keys assigned for {gameObject.name}!");
            return;
        }

        // Select a random sound if multiple are available
        if (soundAddressableKeys.Length == 1) {
            soundAddressableKey = soundAddressableKeys[0];
        } else {
            int randomIndex = Random.Range(0, soundAddressableKeys.Length);
            if (randomIndex >= 0 && randomIndex < soundAddressableKeys.Length) {
                soundAddressableKey = soundAddressableKeys[randomIndex];
            } else {
                Debug.LogError($"[SoundEmitter] Random index {randomIndex} out of bounds for {gameObject.name}!");
                return;
            }
        }

        networkAudio.RequestSoundServerRpc(soundAddressableKey, soundPosition, destinationMixer);
    }
}
