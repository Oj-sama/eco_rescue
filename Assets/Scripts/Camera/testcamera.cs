using UnityEngine;

using Unity.Cinemachine;

public class testcamera : MonoBehaviour
{
    public CinemachineVirtualCamera freeLookCamera; // ✅ Use Virtual Camera Instead
    public Transform player;

    private void Start()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }

        if (freeLookCamera != null && player != null)
        {
            freeLookCamera.Follow = player;
            freeLookCamera.LookAt = player;
        }
    }
}
