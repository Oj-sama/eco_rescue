using Unity.Netcode;
using UnityEngine;

public class NetworkManagerInitializer : MonoBehaviour
{
    void Awake()
    {
        // Make sure the NetworkManager doesn't get destroyed on load
        if (NetworkManager.Singleton != null)
        {
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
            Debug.Log("✅ NetworkManager is set to persist across scenes.");
            NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;
        }
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log("🔑 Approving connection from client...");
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
    }
}
