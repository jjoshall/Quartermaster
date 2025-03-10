using UnityEngine;
using Unity.Netcode;

public class HoldableWeaponController : NetworkBehaviour {
    public ParticleSystem flameParticle;

    [ClientRpc]
    void RpcStartFlameClientRpc() {
        if (flameParticle != null && !flameParticle.isPlaying)
            flameParticle.Play();
    }

    [ClientRpc]
    void RpcStopFlameClientRpc() {
        if (flameParticle != null && flameParticle.isPlaying)
            flameParticle.Stop();
    }

    public void StartFlame() {
        if (!IsSpawned) {
            Debug.LogWarning("HoldableWeaponController is not spawned yet.");
            return;
        }


        if (IsServer) {
            RpcStartFlameClientRpc();
        } else {
            StartFlameServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void StartFlameServerRpc() {
        RpcStartFlameClientRpc();
    }

    public void StopFlame() {
        if (IsServer) {
            RpcStopFlameClientRpc();
        } else {
            StopFlameServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void StopFlameServerRpc() {
        RpcStopFlameClientRpc();
    }
}
