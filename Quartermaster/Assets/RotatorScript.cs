using UnityEngine;

public class MoverScript : MonoBehaviour {
    public float speed = 0.5f; // Movement speed

    void Update() {
        transform.position += Vector3.forward * -1 * speed * Time.deltaTime;
    }
}
