using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();

    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        playerName.Value = new FixedString64Bytes(name);
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Update your player name display here
            Debug.Log($"Player spawned with name: {playerName.Value}");
        }
    }
}