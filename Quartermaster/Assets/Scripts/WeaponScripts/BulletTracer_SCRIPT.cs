using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BulletTracer : NetworkBehaviour {
    private LineRenderer lineRenderer;
    public float tracerDuration = 0.1f;

    private void Awake()
    {
        // Cache the LineRenderer.
        lineRenderer = GetComponent<LineRenderer>();
    }

    // This method initializes the tracer on all clients.
    [ClientRpc]
    public void SetupTracerClientRpc(Vector3 startPoint, Vector3 endPoint)
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        // Start a coroutine to destroy the tracer after a short delay.
        StartCoroutine(DestroyTracer());
    }

    private IEnumerator DestroyTracer()
    {
        yield return new WaitForSeconds(tracerDuration);
        // If running on the server, despawn the network object.
        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
        else
            Destroy(gameObject);
    }
}
