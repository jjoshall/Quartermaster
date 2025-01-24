// Code help from https://www.youtube.com/watch?v=f473C43s8nE&t=398s&ab_channel=Dave%2FGameDevelopment
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
     public float sensX;
     public float sensY;

     public Transform orientation;

     float xRotation;
     float yRotation;

     private void Start()
     {
          Cursor.lockState = CursorLockMode.Locked;
          Cursor.visible = false;
     }

     private void Update()
     {
          float mouseX = Input.GetAxis("Mouse X") * Time.fixedDeltaTime * sensX;
          float mouseY = Input.GetAxis("Mouse Y") * Time.fixedDeltaTime * sensY;

          yRotation += mouseX;
          xRotation -= mouseY;
          xRotation = Mathf.Clamp(xRotation, -90f, 90f);

          // rotate cam and orientation
          transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
          orientation.rotation = Quaternion.Euler(0, yRotation, 0);
     }
}
