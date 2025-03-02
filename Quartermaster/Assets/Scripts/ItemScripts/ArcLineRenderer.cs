using UnityEngine;


// Written following youtube tutorial found here: https://www.youtube.com/watch?v=iLlWirdxass
// "Rendering a launch arc in unity" by Board To Bits Games
[RequireComponent(typeof(LineRenderer))]
public class ArcLineRenderer : MonoBehaviour
{
    LineRenderer lr;

    public float velocity;
    public float angle;
    public int resolution = 10;
    public Vector3 launchDirection = Vector3.forward;

    float g; // force of gravity on the y axis
    float radianAngle;


    void Awake(){
        lr = GetComponent<LineRenderer>();
        // lr.useWorldSpace = true;
        g = Mathf.Abs(Physics.gravity.y);
    }

    // Editor-only function. Reloads arc when values changed in editor.
    void OnValidate(){
        // check that lr is not null and that the game is playing
        if (lr != null && Application.isPlaying){
            RenderArc();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RenderArc();
    }



    void RenderArc(){
        launchDirection = this.transform.forward;
        launchDirection = launchDirection.normalized;
        lr.positionCount = resolution + 1;
        lr.SetPositions(CalculateArcArray());
    }

    Vector3[] CalculateArcArray(){
        Vector3[] arcArray = new Vector3[resolution + 1];
        radianAngle = Mathf.Deg2Rad * angle;
        float maxDistance = (velocity * velocity * Mathf.Sin(2 * radianAngle)) / g;

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
        float verticalOffset = horizontalDistance * Mathf.Tan(radianAngle) -
            ((g * horizontalDistance * horizontalDistance) /
            (2 * velocity * velocity * Mathf.Cos(radianAngle) * Mathf.Cos(radianAngle)));
        
        // Combine the horizontal and vertical components.
        // Assuming the arc starts at the object's position.
        return transform.position + (launchDirection * horizontalDistance) + (Vector3.up * verticalOffset);
    }


}
