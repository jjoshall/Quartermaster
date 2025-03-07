using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BulletTracer : NetworkBehaviour {
    private LineRenderer lineRenderer;
    public float tracerDuration = 0.1f;

    private void Awake() {
        lineRenderer = GetComponent<LineRenderer>();
    }

    [ClientRpc]
    public void SetupTracerClientRpc(Vector3 startPoint, Vector3 endPoint) {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        StartCoroutine(DestroyTracer());
    }

    private IEnumerator DestroyTracer() {
        yield return new WaitForSeconds(tracerDuration);
        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
        else
            Destroy(gameObject);
    }
}
