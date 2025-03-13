using UnityEngine;

public class CoolSpinEffect : MonoBehaviour {
    public Vector3 rotationSpeed = new Vector3(10f, 20f, 15f); // Rotation speed for each axis

    void Update() {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
