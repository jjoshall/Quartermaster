using UnityEngine;


// Written following youtube tutorial found here: https://www.youtube.com/watch?v=iLlWirdxass
// "Rendering a launch arc in unity" by Board To Bits Games
[RequireComponent(typeof(LineRenderer))]
public class ArcLineRenderer : MonoBehaviour
{
    LineRenderer lr;

    public float velocity;
    public float verticalAngle;
    public int resolution = 10;
    public Vector3 launchDirection = Vector3.forward;

    float g; // force of gravity on the y axis
    private float _radianAngle; // x axis rotation.


    void Awake(){
        lr = GetComponent<LineRenderer>();
        // lr.useWorldSpace = true;
        g = Mathf.Abs(Physics.gravity.y);
    }

    // Editor-only function. Reloads arc when values changed in editor.
    void OnValidate(){
        // check that lr is not null and that the game is playing
        UpdateArc();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RenderArc();
    }

    public void UpdateArc(){
        if (lr != null && Application.isPlaying){
            RenderArc();
        }
    }


    void RenderArc(){
        // launchDirection = this.transform.forward;
        launchDirection = launchDirection.normalized;
        lr.positionCount = resolution + 1;
        lr.SetPositions(CalculateArcArray());
    }

    Vector3[] CalculateArcArray(){
        
        Vector3[] arcArray = new Vector3[resolution + 1];
        _radianAngle = Mathf.Deg2Rad * verticalAngle; // assumes passed in angle. deprecated

        Debug.Log("Radians: " + _radianAngle); 
        Debug.Log("Degrees: " + verticalAngle);

        // SOLUTION 2: CALCULATE WITH ATAN2 FROM LAUNCHDIRECTION.NORMALIZED.
            // // Calculate the pitch angle in radians
            // float pitchRadians = Mathf.Atan2(launchDirection.y, new Vector2(launchDirection.x, launchDirection.z).magnitude);

            // // Convert radians to degrees
            // float pitchDegrees = pitchRadians * Mathf.Rad2Deg;

            // // Assign the calculated pitch to verticalAngle
            // verticalAngle = pitchDegrees;
            // _radianAngle = pitchRadians;

        float maxDistance = (velocity * velocity * Mathf.Abs(Mathf.Sin(2 * _radianAngle))) / g;

        for (int i = 0; i <= resolution; i++){
            float t = (float) i / (float) resolution;
            arcArray[i] = CalculateArcPoint(t, maxDistance);
        }

        return arcArray;
    }

    Vector3 CalculateArcPoint(float t, float maxDistance){
        // Horizontal distance along the launch direction.
        float horizontalDistance = t * maxDistance;
        
        // Calculate the vertical offset using the projectile motion formula.
        float verticalOffset = horizontalDistance * Mathf.Tan(_radianAngle) -
            ((g * horizontalDistance * horizontalDistance) /
            (2 * velocity * velocity * Mathf.Cos(_radianAngle) * Mathf.Cos(_radianAngle)));
        
        // Combine the horizontal and vertical components.
        // Assuming the arc starts at the object's position.
        return transform.position + (launchDirection * horizontalDistance) + (Vector3.up * verticalOffset);
    }


}
