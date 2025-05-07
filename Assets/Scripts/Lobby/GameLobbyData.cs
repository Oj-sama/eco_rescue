using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class GameLobbyData : MonoBehaviour
{
    public static GameLobbyData Instance;

    public class PlayerData
    {
        public string playerId;
        public int prefabIndex;
    }

    public List<PlayerData> allPlayers = new List<PlayerData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveLobbyData(Lobby lobby, string localPlayerId)
    {
        allPlayers.Clear();

        foreach (var player in lobby.Players)
        {
            int prefabIndex = 0;
            if (player.Data.TryGetValue("PlayerPrefab", out var prefabData))
            {
                if (!int.TryParse(prefabData.Value, out prefabIndex))
                {
                    Debug.LogWarning($"Invalid prefab index for player {player.Id}");
                    prefabIndex = 0; // Default to first prefab
                }
            }

            allPlayers.Add(new PlayerData
            {
                playerId = player.Id,
                prefabIndex = prefabIndex
            });
        }
    }
}