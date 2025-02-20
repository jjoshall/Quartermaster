using UnityEngine;
using Unity.Netcode;

public class NetworkFloatingObject : NetworkBehaviour
{
    public float moveDistance = 0.2f;
    public float moveSpeed = 2f;
    public float rotationSpeed = 30f;
    public float startHeight = 23f;

    private Vector3 startPosition;
    private float timeOffset;

    void Start()
    {
        if (IsServer)
        {
            startPosition = new Vector3(transform.position.x, startHeight, transform.position.z);
            timeOffset = Random.value * Mathf.PI * 2;
            transform.position = startPosition;
        }
    }

    void Update()
    {
        if (IsServer)
        {
            float newY = startHeight + Mathf.Sin(Time.time * moveSpeed + timeOffset) * moveDistance * 0.5f;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
}
