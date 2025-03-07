using UnityEngine;

public class FloatingText_SCRIPT : MonoBehaviour {
    public float destroyTime = 3f;
    public Vector3 offset = new Vector3(0, 2.5f, 0);
    public Vector3 randomizeIntensity = new Vector3(2f, 0, 0);
    public Transform playerCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() {
        playerCamera = Camera.main.transform;

        // Set the color of the text to red
        GetComponent<MeshRenderer>().material.color = Color.red;

        Destroy(gameObject, destroyTime);
        transform.localPosition += offset;
        transform.localPosition += new Vector3(Random.Range(-randomizeIntensity.x, randomizeIntensity.x),
            Random.Range(-randomizeIntensity.y, randomizeIntensity.y),
            Random.Range(-randomizeIntensity.z, randomizeIntensity.z));
    }

    private void Update() {
        if (playerCamera != null) {
            transform.LookAt(playerCamera);
            transform.rotation = Quaternion.LookRotation(transform.position - playerCamera.position);
        }
    }
}
