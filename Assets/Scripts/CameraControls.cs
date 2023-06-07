using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
  [SerializeField]
  private float speed = 5.0f, rotationSpeed = 2.0f;

  void Update()
  {
    if (Input.GetMouseButton(1)) // Check if the right mouse button is held down
    {
      // Get the input for WASD keys
      float horizontal = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
      float vertical = Input.GetAxis("Vertical") * speed * Time.deltaTime;

      // Get the input for Q and E keys
      float upDown = 0;
      if (Input.GetKey(KeyCode.Q))
      {
        upDown = -speed * Time.deltaTime;
      }
      else if (Input.GetKey(KeyCode.E))
      {
        upDown = speed * Time.deltaTime;
      }

      // Move the camera
      transform.Translate(horizontal, upDown, vertical);

      // Rotate the camera based on mouse movement
      float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
      float mouseY = -Input.GetAxis("Mouse Y") * rotationSpeed;

      transform.Rotate(new Vector3(mouseY, mouseX, 0));

      // Get the input for the mouse wheel and adjust the speed parameter
      float mouseWheelInput = Input.GetAxis("Mouse ScrollWheel");
      if (mouseWheelInput != 0)
      {
        speed += mouseWheelInput * 10;
      }
    }
  }
}
