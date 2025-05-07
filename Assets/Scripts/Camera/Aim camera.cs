using UnityEngine;
using Unity.Cinemachine;

public class aimcamera : MonoBehaviour
{
    public Transform playerBody;            // Reference to the player's body for rotation
    public Transform spine;                 // Reference to the spine or upper body for rotation
    public float mouseSensitivityX = 2.0f;  // Sensitivity for mouse movement on X (horizontal)
    public float mouseSensitivityY = 2.0f;  // Sensitivity for mouse movement on Y (vertical)
    public float maxVerticalAngle = 80f;    // Max angle the player can aim up
    public float minVerticalAngle = -80f;   // Min angle the player can aim down

    private float currentRotationX = 0f;    // To track the current X-axis rotation of the camera
    private float currentRotationY = 0f;    // To track the current Y-axis rotation of the camera

    public CinemachineCamera freeLookCam;  // Reference to the Cinemachine FreeLook Camera

    void Start()
    {
        // Get the reference to the Cinemachine FreeLook component attached to this script
        freeLookCam = GetComponent<CinemachineCamera>();
    }

    void Update()
    {
        // Check if the freeLookCam is valid
        if (freeLookCam != null)
        {
            // Get mouse input
            float mouseInputX = Input.GetAxis("Mouse X");
            float mouseInputY = Input.GetAxis("Mouse Y");

            // Rotate the camera horizontally (player body follows mouse X)
            currentRotationX += mouseInputX * mouseSensitivityX;

            // Rotate vertically (spine follows mouse Y)
            currentRotationY -= mouseInputY * mouseSensitivityY;
            currentRotationY = Mathf.Clamp(currentRotationY, minVerticalAngle, maxVerticalAngle);  // Clamp vertical rotation

            // Apply body rotation for horizontal turning
            playerBody.rotation = Quaternion.Euler(0, currentRotationX, 0);  // Rotate the player's body

            // Apply spine rotation to allow aiming with the Y-axis (clamped for realism)
            spine.localRotation = Quaternion.Euler(currentRotationY, 0, 0);  // Rotate the spine on X-axis

            // Apply vertical rotation to the Cinemachine FreeLook camera's transform (Y-axis movement)
            freeLookCam.transform.rotation = Quaternion.Euler(currentRotationY, freeLookCam.transform.rotation.eulerAngles.y, 0);
        }
    }
}
