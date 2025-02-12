// This script is used to move the camera to the player's position and have the camera move with the player.
// If you don't have this, the camera is weird for rigidbody movement.
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
     public Transform cameraPosition;

     // Update is called once per frame
     private void Update()
     {
          transform.position = cameraPosition.position;
     }
}
