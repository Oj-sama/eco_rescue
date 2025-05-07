using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnCollisionCheckRadius = 1.5f;
    [SerializeField] private LayerMask playerLayerMask;

    private int nextSpawnIndex = 0;
    private HashSet<string> usedNames = new HashSet<string>();
    private int playerCounter = 1;
    private Dictionary<ulong, string> clientPlayerNames = new Dictionary<ulong, string>();
    private Dictionary<int, ulong> spawnPointOccupants = new Dictionary<int, ulong>();

    private NetworkVariable<int> currentSpawnIndex = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    #region Unity Callbacks
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (IsClient)
        {
            if (!IsHost && NetworkManager.LocalClientId != 0)
            {
                RequestSpawnPointServerRpc(NetworkManager.LocalClientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    #endregion

    #region Player Management
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        string playerName = AssignUniqueName();
        clientPlayerNames[clientId] = playerName;

        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            SetupPlayer(client, playerName);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        ReleaseClientSpawnPoint(clientId);

        if (clientPlayerNames.TryGetValue(clientId, out string playerName))
        {
            ReleaseName(playerName);
            clientPlayerNames.Remove(clientId);
        }
    }

    private void ReleaseClientSpawnPoint(ulong clientId)
    {
        foreach (var kvp in new Dictionary<int, ulong>(spawnPointOccupants))
        {
            if (kvp.Value == clientId)
            {
                Debug.Log($"Releasing spawn point {kvp.Key} from client {clientId}");
                spawnPointOccupants.Remove(kvp.Key);
                break;
            }
        }
    }

    private void SetupPlayer(NetworkClient client, string playerName)
    {
        (Transform spawnPoint, int spawnIndex) = GetAvailableSpawnPoint(client.ClientId);
        Debug.Log($"Server: Spawning player {playerName} at spawn point {spawnIndex}");

        if (client.PlayerObject == null)
        {
            Debug.LogError($"Client {client.ClientId} has no PlayerObject!");
            return;
        }

        client.PlayerObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        AssignSpawnPointClientRpc(client.ClientId, spawnIndex);

        if (client.PlayerObject.TryGetComponent<PControllerV2>(out var playerController))
        {
            playerController.playerName.Value = new FixedString64Bytes(playerName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPointServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            (Transform _, int spawnIndex) = GetAvailableSpawnPoint(clientId);

            AssignSpawnPointClientRpc(clientId, spawnIndex);
        }
    }

    [ClientRpc]
    public void AssignSpawnPointClientRpc(ulong clientId, int spawnIndex)
    {
        if (NetworkManager.LocalClientId != clientId || !IsClient) return;

        if (spawnPoints == null || spawnPoints.Length == 0 || spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
        {
            Debug.LogError("Invalid spawn point index or no spawn points available!");
            return;
        }

        Transform spawnPoint = spawnPoints[spawnIndex];
        Debug.Log($"Client {clientId}: Moving to spawn point {spawnIndex} at {spawnPoint.position}");

        if (NetworkManager.LocalClient.PlayerObject != null)
        {
            NetworkManager.LocalClient.PlayerObject.transform.SetPositionAndRotation(
                spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"Client {clientId} has no PlayerObject to position!");
        }
    }
    #endregion

    #region Spawn Point Management
    private (Transform, int) GetAvailableSpawnPoint(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned! Using default position.");
            return (transform, -1);
        }

        foreach (var kvp in spawnPointOccupants)
        {
            if (kvp.Value == clientId)
            {
                Debug.Log($"Client {clientId} already has spawn point {kvp.Key}");
                return (spawnPoints[kvp.Key], kvp.Key);
            }
        }

        int startIndex = nextSpawnIndex;
        int currentIndex = startIndex;
        bool foundSpawnPoint = false;

        do
        {
            if (!spawnPointOccupants.ContainsKey(currentIndex))
            {
                if (!IsSpawnPointOccupied(currentIndex))
                {
                    foundSpawnPoint = true;
                    break;
                }
            }

            currentIndex = (currentIndex + 1) % spawnPoints.Length;
        } while (currentIndex != startIndex);

        if (!foundSpawnPoint)
        {
            Debug.LogWarning("All spawn points are occupied! Using the next one in sequence anyway.");
        }

        spawnPointOccupants[currentIndex] = clientId;

        nextSpawnIndex = (currentIndex + 1) % spawnPoints.Length;
        currentSpawnIndex.Value = nextSpawnIndex;

        return (spawnPoints[currentIndex], currentIndex);
    }

    private bool IsSpawnPointOccupied(int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
            return true;

        Transform spawnPoint = spawnPoints[spawnIndex];
        if (spawnPoint == null) return true;

        Collider[] colliders = Physics.OverlapSphere(spawnPoint.position, spawnCollisionCheckRadius, playerLayerMask);

        if (colliders.Length > 0)
        {
            Debug.Log($"Spawn point {spawnIndex} is physically occupied by {colliders.Length} objects");
            return true;
        }

        return false;
    }
    #endregion

    #region Utility Methods
    public string AssignUniqueName()
    {
        string playerName;
        do
        {
            playerName = $"Player{playerCounter++}";
        } while (usedNames.Contains(playerName));

        usedNames.Add(playerName);
        return playerName;
    }

    public void ReleaseName(string playerName)
    {
        if (!string.IsNullOrEmpty(playerName))
        {
            usedNames.Remove(playerName);
        }
    }
    #endregion
}