using UnityEngine;

public class SpinObject : MonoBehaviour {
    public float rotationSpeed = 100f; // Speed in degrees per second
    public bool reverse = false; // Toggle for reversing rotation

    void Update() {
        float direction = reverse ? -1f : 1f;
        transform.Rotate(0, direction * rotationSpeed * Time.deltaTime, 0);
    }
}
