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

    public float floorHeight;

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

        // float maxDistance = (velocity * velocity * Mathf.Abs(Mathf.Sin(2 * _radianAngle))) / g;

        float maxDistance = GetMaxDistance(velocity, verticalAngle, transform.position.y);

        for (int i = 0; i <= resolution; i++){
            float t = (float) i / (float) resolution;
            arcArray[i] = CalculateArcPoint(t, maxDistance);
        }

        return arcArray;
    }

    float GetMaxDistance(float velocity, float angle, float initialHeight)
{
        float radianAngle = angle * Mathf.Deg2Rad;
        
        float cosAngle = Mathf.Cos(radianAngle);
        float tanAngle = Mathf.Tan(radianAngle);
        
        float termUnderSqrt = tanAngle * tanAngle + (2 * g * initialHeight) / (velocity * velocity * cosAngle * cosAngle);
        
        if (termUnderSqrt < 0)
            return 0f; // If the value under sqrt is negative, there's no real solution.

        float maxDistance = (velocity * velocity * cosAngle * cosAngle / g) * 
                            (tanAngle + Mathf.Sqrt(termUnderSqrt));

        return maxDistance;
    }

    Vector3 CalculateArcPoint(float t, float maxDistance){
        // Horizontal distance along the trajectory.
        float horizontalDistance = t * maxDistance; // this is x.  in y = f(x)
        
        // Calculate the vertical _offset using the projectile motion formula.
        float verticalOffset = horizontalDistance * Mathf.Tan(_radianAngle) - 
           (   (g * horizontalDistance * horizontalDistance) /
               (2 * velocity * velocity * Mathf.Cos(_radianAngle) * Mathf.Cos(_radianAngle))   );
        
        // Create a horizontal version of the launch direction by zeroing out its vertical component.
        Vector3 horizontalDirection = launchDirection;
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();
        
        // Combine the horizontal displacement and the physics-calculated vertical _offset.
        return transform.position + (horizontalDirection * horizontalDistance) + (Vector3.up * verticalOffset);
    }

    public void ClearArc(){
        lr.SetPositions(new Vector3[0]);
        lr.positionCount = 0;
    }

}
