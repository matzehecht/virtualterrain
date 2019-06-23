using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingCamControl : MonoBehaviour
{
    /*
	    Movement shortcuts in FlyCam Mode:
	    WASD or Arrows: General Movement
        Q: "Climb" with camera
        E: "Drop" camera
        Shift: Move faster
        Control: Move slower
        Esc: Exit / Free the mouse for debugging or other reasons
	*/

    public float climbSpeed = 4;
    public float normalMoveSpeed = 10;
    public float slowMoveFactor = 0.25f;
    public float fastMoveFactor = 3;

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    private float cameraSensitivity = 90;

    // Use this for initialization
    void Start()
    {
        // Lock the cursor in the middle of the screen to achieve a fps handling
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // -- MOUSE CONTROLLER --
        // Save the mouse movements of the single axis into variables
        rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        // Clamp is used tp apply a limit to the y rotation's value
        rotationY = Mathf.Clamp(rotationY, -90, 90);

        // apply the rotation with the help of a quaternion object
        transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);


        // -- KEYBOARD CONTROLLER --
        // implementation of standard movement keys and the movement applied when pressing them as explained above
        // ... for fast movement if shift key pressed
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
        }
        // ... for slow movement if control key pressed
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
        }
        // ... for normal movement otherwise
        else
        {
            transform.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
        }

        // Climb or drop camera for a minecraft style movement
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position += transform.up * climbSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position -= transform.up * climbSpeed * Time.deltaTime;
        }

        // Free the mouse for debugging or other reasons
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState != CursorLockMode.Locked) ? Cursor.lockState = CursorLockMode.Locked : Cursor.lockState = CursorLockMode.None; ;
        }
    }
}
