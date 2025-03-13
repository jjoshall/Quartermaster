using UnityEngine;

public class SpinGlow : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float glowIntensity = 2f;
    public float glowSpeed = 2f;
    private Light glowLight;

    void Start()
    {
        // Add a Light component dynamically
        glowLight = gameObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.range = 15f;
        glowLight.intensity = glowIntensity;
        glowLight.color = new Color(1f, 0.5f, 0f); // Orange color
    }

    void Update()
    {
        // Rotate object around the Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Make the glow intensity pulse over time
        glowLight.intensity = glowIntensity + Mathf.Sin(Time.time * glowSpeed);
    }
}
