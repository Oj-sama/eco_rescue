using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcherMenu : MonoBehaviour
{
    public CinemachineCamera[] cameras;
    private int activeCameraIndex = 0;

    void Start()
    {
        ActivateCamera(0); // Activate the first camera by default
    }

    public void SwitchCamera(int index)
    {
        if (index >= 0 && index < cameras.Length)
        {
            ActivateCamera(index);
        }
    }

    private void ActivateCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].Priority = (i == index) ? 10 : 0; // Higher priority activates the camera
        }
        activeCameraIndex = index;
    }
}