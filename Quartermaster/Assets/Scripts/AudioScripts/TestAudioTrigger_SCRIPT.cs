using Unity.Netcode;
using UnityEngine;

public class TestAudioTrigger : NetworkBehaviour {
    public string soundAddressableKey = "Player/PlayerJump.ogg";

    void Update() {
        if (!IsLocalPlayer)
            return;
            
        if (Input.GetKeyDown(KeyCode.K)) {
            // AudioLibrary.GetClipAsync(soundAddressableKey, (clip) => {
            //     if (clip != null && AudioManager.Instance != null) {
            //         AudioManager.Instance.PlaySoundAtPosition(clip, transform.position);
            //     }
            // });

            GetComponent<NetworkAudio>().RequestSoundServerRpc(soundAddressableKey, transform.position, "SFX");
        }

        if (Input.GetKeyDown(KeyCode.Y)) {
            TooltipManager.SendTooltip("testing tooltip", toAll: true);
        }
    }
}
