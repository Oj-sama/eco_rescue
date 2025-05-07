using UnityEngine;
using Unity.Netcode;

public class Collectable : NetworkBehaviour
{
    public Item item;
    public int quantity = 1;
    public GameObject inplayerprefab;

    public void Pickup()
    {
        // Only the client that interacted will call this
        if (IsClient && !IsHost)
        {
            RequestPickupServerRpc();
        }
        else if (IsServer) // Host can destroy directly
        {
            HandlePickup();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        HandlePickup();
    }

    private void HandlePickup()
    {
        // Use Despawn instead of Destroy when dealing with NetworkObjects
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
