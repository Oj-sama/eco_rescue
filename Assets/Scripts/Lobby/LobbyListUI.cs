using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyListUI : MonoBehaviour
{
    public GameObject lobbyItemPrefab; // Assign the LobbyItem prefab in Unity
    public Transform contentPanel; // Assign the "LobbyListContent" GameObject

    private List<GameObject> lobbyItems = new List<GameObject>();
    private LobbyManager lobbyListUI;

  

    public void RefreshLobbyList()
    {
        
    }
}
