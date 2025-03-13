using UnityEngine;

public class OscillatingRotator : MonoBehaviour
{
    public float rotationRange = 20f; // 30-degree range
    public float speed = 1f; // Speed of oscillation
    public float startOffset = 0f; // Offset in degrees (between -1 and 1 for a phase shift)

    private float startAngle;

    void Start()
    {
        startAngle = transform.localEulerAngles.x;
    }

    void Update()
    {
        float angle = startAngle + Mathf.Sin(Time.time * speed + (startOffset * Mathf.PI)) * rotationRange;
        transform.localRotation = Quaternion.Euler(angle, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }
}
