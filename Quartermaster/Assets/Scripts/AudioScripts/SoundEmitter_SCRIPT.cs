using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;




public class SoundEmitter : MonoBehaviour {
    private NetworkAudio networkAudio;

    public string emitterID;
    [SerializeField] string destinationMixer;

    [SerializeField] string[] soundAddressableKeys;
    private string soundAddressableKey;

    private void Awake() {
        networkAudio = GetComponentInParent<NetworkAudio>(); 
    }

    [System.Serializable]
    public struct emitterType {
        public string emitterID;
        public string destinationMixer;
        public string[] soundAddressableKeys;
    }

    public List<emitterType> emitterTypes;

    public void PlayUserBasedNetworkSound(Vector3 soundPosition, string emitterId){
        foreach (emitterType emitter in emitterTypes){
            if (emitter.emitterID == emitterId){
                destinationMixer = emitter.destinationMixer;
                soundAddressableKeys = emitter.soundAddressableKeys;
                PlayNetworkedSound(soundPosition);
            }
        }
    }

    public void PlayNetworkedSound(Vector3 soundPosition) {
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

        Debug.Log("[SoundEmitter] Playing sound: " + soundAddressableKey);

        Debug.Log("Requestion sound server RPC for " + soundAddressableKey);
        networkAudio.RequestSoundServerRpc(soundAddressableKey, soundPosition, destinationMixer);
    }

    // helpers for looped sounds
    public void StartLoopedSound(Vector3 soundPosition) {
        if (networkAudio == null) {
            Debug.LogError("[SoundEmitter] No NetworkAudio component found!");
            return;
        }

        if (soundAddressableKeys == null || soundAddressableKeys.Length == 0) {
            Debug.LogError($"[SoundEmitter] No sound keys assigned for {gameObject.name}!");
            return;
        }

        soundAddressableKey = soundAddressableKeys[0];
        Debug.Log("[SoundEmitter] Starting looping sound: " + soundAddressableKey);
        networkAudio.RequestLoopedSoundServerRpc(soundAddressableKey, soundPosition, destinationMixer);
    }

    public void StopLoopedSound(Vector3 soundPosition) {
        if (networkAudio == null) {
            Debug.LogError("[SoundEmitter] No NetworkAudio component found!");
            return;
        }

        Debug.Log("[SoundEmitter] Stopping looping sound: " + soundAddressableKey);
        networkAudio.StopLoopedSoundServerRpc(soundAddressableKey, soundPosition, destinationMixer);
    }
}
