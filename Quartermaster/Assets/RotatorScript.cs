using UnityEngine;

public class RotatorScript : MonoBehaviour
{
    public float X = 30f; // Rotation amount per side (final rotation is X * 2)
    public float Y = 3f;  // Duration in seconds for full rotation

    private float elapsedTime = 0f;
    private bool isRotating = false;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y) && !isRotating) 
        {
            StartRotation();
        }

        if (isRotating)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / Y); // Normalize time for smooth transition
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (elapsedTime >= Y)
            {
                isRotating = false; // Stop rotating once complete
            }
        }
    }

    private void StartRotation()
    {
        startRotation = transform.rotation;
        targetRotation = Quaternion.Euler(0, X * 2, 0) * startRotation;
        elapsedTime = 0f;
        isRotating = true;
    }
}
