using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefabs")]
     public GameObject[] playerPrefabs; // Assign in Inspector

    [Header("Spawn Settings")]
    [SerializeField] private Vector2 spawnArea = new Vector2(5f, 5f);


  
    private void Start()
    {
        if (!IsServer) return;

        Debug.Log("Server starting player spawn...");
        
        // Verify we have prefabs assigned
        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("No player prefabs assigned in PlayerSpawner!");
            return;
        }

        // Verify GameLobbyData exists
        if (GameLobbyData.Instance == null)
        {
            Debug.LogError("GameLobbyData instance not found!");
            return;
        }

        Debug.Log($"Found {GameLobbyData.Instance.allPlayers.Count} players to spawn");

        // Spawn players after short delay
        Invoke(nameof(SpawnAllPlayers), 0.5f);
    }
    private void OnSceneLoaded(string sceneName, LoadSceneMode mode)
    {
        if (sceneName == "YourGameSceneName") // Optional check
        {
            SpawnAllPlayers(); // Call your player spawn function
        }
    }
    public void SetPlayerPrefabs(GameObject[] prefabs)
    {
        playerPrefabs = prefabs;
    }

    private void SpawnAllPlayers()
    {
        foreach (var playerData in GameLobbyData.Instance.allPlayers)
        {
            SpawnPlayer(playerData);
        }
    }

    private void SpawnPlayer(GameLobbyData.PlayerData data)
    {
        // Debug the incoming data
      

        // Validate prefab index
        if (data.prefabIndex < 0 || data.prefabIndex >= playerPrefabs.Length)
        {
            Debug.LogError($"Invalid prefab index {data.prefabIndex}. Valid range: 0-{playerPrefabs.Length - 1}");
            return;
        }

        // Validate prefab
        if (playerPrefabs[data.prefabIndex] == null)
        {
            Debug.LogError($"Prefab at index {data.prefabIndex} is null!");
            return;
        }

        // Calculate spawn position
        Vector3 spawnPos = new Vector3(
            Random.Range(-spawnArea.x, spawnArea.x),
            0,
            Random.Range(-spawnArea.y, spawnArea.y));

        // Instantiate player
        GameObject player = Instantiate(
            playerPrefabs[data.prefabIndex],
            spawnPos,
            Quaternion.identity);

        // Get NetworkObject
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Player prefab missing NetworkObject component!");
            Destroy(player);
            return;
        }

        // Spawn with ownership
        ulong clientId;
        if (!ulong.TryParse(data.playerId, out clientId))
        {
            Debug.LogError($"Invalid client ID format: {data.playerId}");
            Destroy(player);
            return;
        }

        netObj.SpawnWithOwnership(clientId, true);
       
    }
}