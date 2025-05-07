using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro for better UI

public class LobbyUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField lobbyNameInput; // Input field for lobby name
    public TMP_Dropdown maxPlayersDropdown; // Dropdown for max players
    public Button createLobbyButton; // Button to create a lobby

    [Header("Lobby Info UI")]
    public TMP_Text lobbyNameText;
    public TMP_Text playersCountText;
    public TMP_Text lobbyCodeText;

    private LobbyManager lobbyManager;

    private void Start()
    {
        // Get the LobbyManager script from the scene
        lobbyManager = FindAnyObjectByType<LobbyManager>();

        // Add listener to create lobby button
        createLobbyButton.onClick.AddListener(CreateLobby);
    }

   public void CreateLobby()
{
    string lobbyName = lobbyNameInput.text;
    
    if (!int.TryParse(maxPlayersDropdown.options[maxPlayersDropdown.value].text, out int maxPlayers))
    {
        Debug.LogWarning("❌ Invalid max players value selected.");
        return;
    }

    if (string.IsNullOrEmpty(lobbyName))
    {
        Debug.LogWarning("❌ Lobby name cannot be empty!");
        return;
    }

    StartCoroutine(CreateLobbyCoroutine(lobbyName, maxPlayers));
}


    private IEnumerator CreateLobbyCoroutine(string lobbyName, int maxPlayers)
    {
        lobbyManager.createLobby(lobbyName, maxPlayers);

        // Wait a short time for the lobby to be created
        yield return new WaitForSeconds(1.5f);

        // Update UI after lobby is created
        UpdateLobbyUIHost();
    }

    private void UpdateLobbyUIHost()
    {
        if (lobbyManager.hostlobby != null)
        {
            lobbyNameText.text = "Lobby Name: " + lobbyManager.hostlobby.Name;
            playersCountText.text = "Players: " + lobbyManager.hostlobby.Players.Count + "/" + lobbyManager.hostlobby.MaxPlayers;
            lobbyCodeText.text = "Lobby Code: " + lobbyManager.hostlobby.LobbyCode;
        }
    }
    public void UpdateLobbyUIJoin()
    {
        if (lobbyManager.joinedlobby != null)
        {
            lobbyNameText.text = "Lobby Name: " + lobbyManager.joinedlobby.Name;
            playersCountText.text = "Players: " + lobbyManager.joinedlobby.Players.Count + "/" + lobbyManager.joinedlobby.MaxPlayers;
            lobbyCodeText.text = "Lobby Code: " + lobbyManager.joinedlobby.LobbyCode;
        }
    }
}
