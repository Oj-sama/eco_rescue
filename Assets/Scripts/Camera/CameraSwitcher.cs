using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera camera1; // First gameplay camera
    public CinemachineCamera camera2; // Second gameplay camera
    public CinemachineCamera aimCamera; // Aiming camera

    private CinemachineCamera lastActiveCamera;
    private CinemachineCamera activeCamera;
    public GameObject playerCameraControl; // Reference to player camera control script

    void Start()
    {
        // Set initial active camera
        activeCamera = camera1;
        lastActiveCamera = camera1;
        camera1.Priority = 10;
        camera2.Priority = 0;
        aimCamera.Priority = 0; // Aim camera starts inactive

        LockCursor(true); // Lock cursor on game start
    }

    void Update()
    {
        // Toggle between camera1 and camera2 with "V"
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleGameplayCamera();
        }
    }

    void ToggleGameplayCamera()
    {
        if (activeCamera == camera1)
        {
            ActivateCamera(camera2);
        }
        else
        {
            ActivateCamera(camera1);
        }
    }

    public void StartAiming()
    {
        lastActiveCamera = activeCamera; // Store the last used camera
        ActivateCamera(aimCamera);
        LockCursor(true); // Unlock cursor when aiming
        DisablePlayerControl(false); // Disable camera control
    }

    public void StopAiming()
    {
        ActivateCamera(lastActiveCamera); // Return to previous camera
        LockCursor(true); // Lock cursor again
        DisablePlayerControl(false); // Re-enable camera control
    }

    void ActivateCamera(CinemachineCamera cam)
    {
        camera1.Priority = 0;
        camera2.Priority = 0;
        aimCamera.Priority = 0;
        cam.Priority = 10; // Higher priority activates the camera
        activeCamera = cam;
    }

    void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void DisablePlayerControl(bool disable)
    {
        if (playerCameraControl != null)
        {
            playerCameraControl.SetActive(!disable);
        }
    }
}
