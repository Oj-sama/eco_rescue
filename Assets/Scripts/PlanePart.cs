using UnityEngine;
using Unity.Netcode;

public class PlanePart : NetworkBehaviour
{
    public float detectionRange = 20f;
    public Renderer objectRenderer;
    public Renderer placementIndicatorRenderer;
    private Color originalColor;
    private NetworkVariable<bool> check = new NetworkVariable<bool>();

    void Start()
    {
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
        else
        {
            Debug.LogError("Renderer not assigned in " + gameObject.name);
        }

        placementIndicatorRenderer.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            check.Value = false;
        }
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            ServerCheckPlayers();
        }

        UpdateVisuals();
    }

    private void ServerCheckPlayers()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client?.PlayerObject == null) continue;

            NetworkObject playerObject = client.PlayerObject;

        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryInteractServerRpc(ServerRpcParams rpcParams = default)
    {
        if (check.Value) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        NetworkObject playerObject = client.PlayerObject;
        if (playerObject == null) return;

        if (IsPlayerInRange(playerObject) && HasInventoryItem(playerObject))
        {
            ProcessInteraction(playerObject);
        }
    }

    private bool IsPlayerInRange(NetworkObject player)
    {
        return Vector3.Distance(transform.position, player.transform.position) <= detectionRange;
    }

    private bool HasInventoryItem(NetworkObject player)
    {
        Inventory inventory = player.GetComponentInChildren<Inventory>();
        return inventory != null && CheckInventoryForItem(inventory);
    }

    private bool CheckInventoryForItem(Inventory inventory)
    {
        foreach (ItemSlotInfo item in inventory.items)
        {
            if (item.item != null && item.item.GiveName() == gameObject.name)
            {
                return true;
            }
        }
        return false;
    }

    private void ProcessInteraction(NetworkObject player)
    {
        Inventory inventory = player.GetComponentInChildren<Inventory>();
        foreach (ItemSlotInfo slot in inventory.items)
        {
            if (slot.item != null && slot.item.GiveName() == gameObject.name)
            {
                slot.stacks--;
                if (slot.stacks <= 0) slot.item = null;
                check.Value = true;

                UpdateVisualsClientRpc();
                break;
            }
        }
    }

    [ClientRpc]
    private void UpdateVisualsClientRpc()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (check.Value)
        {
            placementIndicatorRenderer.enabled = true;
            placementIndicatorRenderer.material.color = originalColor;
            return;
        }

        NetworkObject localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        Inventory inventory = localPlayer.GetComponentInChildren<Inventory>();

        bool hasItem = inventory != null && CheckInventoryForItem(inventory);
        bool inRange = distance <= detectionRange;

        placementIndicatorRenderer.enabled = inRange && hasItem;
        placementIndicatorRenderer.material.color = (inRange && hasItem) ? Color.green : originalColor;
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null &&
            Input.GetKeyDown(KeyCode.P))
        {
            TryInteractServerRpc();
        }
    }

    private void OnDestroy()
    {
        if (check != null) check.Dispose();
    }
}