using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class WeaponEffects : NetworkBehaviour {
    [Header("Trail Settings")]
    [SerializeField] private List<TrailRenderer> bulletTrailPrefab;
    [SerializeField] private float trailDuration = 0.3f;

    // This ClientRpc spawns the trail on every client.
    [ClientRpc]
    public void SpawnBulletTrailClientRpc(Vector3 startPoint, Vector3 endPoint, int weaponID) {
        if (bulletTrailPrefab != null) {
            TrailRenderer trail = Instantiate(bulletTrailPrefab[weaponID], startPoint, Quaternion.identity);
            StartCoroutine(AnimateTrail(trail, startPoint, endPoint));
        }
    }

    // If the caller isn’t the server, they can ask the server to spawn the trail.
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnBulletTrailServerRpc(Vector3 startPoint, Vector3 endPoint, int weaponID) {
        SpawnBulletTrailClientRpc(startPoint, endPoint, weaponID);
    }

    // Coroutine to animate the trail from start to end.
    private IEnumerator AnimateTrail(TrailRenderer trail, Vector3 startPoint, Vector3 endPoint) {
        float t = 0f;
        while (t < 1f) {
            trail.transform.position = Vector3.Lerp(startPoint, endPoint, t);
            t += Time.deltaTime / (trailDuration * 0.5f);
            yield return null;
        }
        trail.transform.position = endPoint;
        Destroy(trail.gameObject, trail.time);
    }
}
