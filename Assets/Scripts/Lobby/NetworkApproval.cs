using Unity.Netcode;
using UnityEngine;

public class NetworkApproval : MonoBehaviour
{
    private void Start()
    {
        // Register the connection approval callback in the game scene.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;
        }
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log("🔑 Approving connection from client...");

        // Approve the connection request.
        response.Approved = true;

        // Ensure that a player object is created.
        response.CreatePlayerObject = true;

        // Set spawn position and rotation (customize as needed).
        response.Position = Vector3.zero;  // Modify this as needed.
        response.Rotation = Quaternion.identity;  // Modify this as needed.

        Debug.Log("🔑 Connection approved. Player will spawn.");
    }
}
