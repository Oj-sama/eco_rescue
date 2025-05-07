using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class LobbyManager : MonoBehaviour
{
    public Lobby hostlobby;
    public Lobby joinedlobby;
    private float heartbeattimer;
    private float lobbyUpdateTimer;
    private string PlayerName;
    private List<GameObject> lobbyItems = new List<GameObject>();

    public GameObject lobbyItemPrefab; // Assign the LobbyItem prefab in Unity
    public Transform contentPanel;
    public LobbyUIManager LobbyUIManager;
    public CameraSwitcherMenu CameraSwitcher;
    public Transform playerListPanel; // Panel where player UIs will go
    public GameObject playerPrefab;  // Player prefab with UI for name, ready, kick, promote
    public GameObject[] playerPrefabs; // Prefab slots in the lobby
    private Dictionary<string, int> playerSlotAssignments = new Dictionary<string, int>(); // Track assignments
    private Dictionary<string, int> playerPrefabIndices = new Dictionary<string, int>();
    [SerializeField] private string gameSceneName = "Map";

    private async void Start()
    {

        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("signed in" + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        PlayerName = "Aziz Hammedi" + UnityEngine.Random.Range(10, 99);
        Debug.Log("player Name : " + PlayerName);
    }
    private void Update()
    {
        handlelobbyheartbeat();
        handlelobbypollforupdates();


    }

    private async void handlelobbyheartbeat()
    {
        if (hostlobby != null)
        {
            heartbeattimer -= Time.deltaTime;
            if (heartbeattimer < 0f)
            {
                float heartbeattimermax = 15;
                heartbeattimer = heartbeattimermax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostlobby.Id);
            }
        }

    }


    private async void handlelobbypollforupdates()
    {
        try
        {
            // Early exit if services aren't ready
            if (UnityServices.State != ServicesInitializationState.Initialized ||
                !AuthenticationService.Instance.IsSignedIn ||
                LobbyService.Instance == null)
            {
                return;
            }

            // Early exit if no joined lobby
            if (joinedlobby == null)
            {
                return;
            }

            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                lobbyUpdateTimer = 1.1f;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedlobby.Id);
                if (lobby == null)
                {
                    Debug.LogWarning("Failed to get lobby update");
                    return;
                }

                joinedlobby = lobby;

                // Check if game started - REMOVED IsHost() check!
                if (lobby.Data != null &&
                    lobby.Data.TryGetValue("GameStarted", out var gameStartedData) &&
                    gameStartedData.Value == "true")
                {
                    if (lobby.Data.TryGetValue("Map", out var mapData) &&
                        !string.IsNullOrEmpty(mapData.Value))
                    {
                        // Save lobby data for all players
                        if (GameLobbyData.Instance != null)
                        {
                            GameLobbyData.Instance.SaveLobbyData(lobby, AuthenticationService.Instance.PlayerId);
                        }

                        // Different behavior for host vs clients
                        if (NetworkManager.Singleton != null)
                        {
                            if (IsHost())
                            {
                                // Host starts hosting and loads scene
                                if (!NetworkManager.Singleton.IsServer)
                                {
                                    NetworkManager.Singleton.StartHost();
                                }
                                NetworkManager.Singleton.SceneManager.LoadScene(
                                    mapData.Value,
                                    LoadSceneMode.Single);
                            }
                            else
                            {
                                // Clients join and load scene
                                if (!NetworkManager.Singleton.IsClient)
                                {
                                    NetworkManager.Singleton.StartClient();
                                }
                                NetworkManager.Singleton.SceneManager.LoadScene(
                                    mapData.Value,
                                    LoadSceneMode.Single);
                            }
                        }
                    }
                }

                // Normal lobby updates
                if (lobby.Players != null &&
                    (playerSlotAssignments == null || lobby.Players.Count != playerSlotAssignments.Count))
                {
                    printplayers(lobby);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating lobby: {ex.Message}");
        }
    }

    public async void createLobby(string lobbyName, int maxPlayers)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = false,
                Player = Getplayer()
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            hostlobby = lobby;
            joinedlobby = hostlobby;
            CameraSwitcher.SwitchCamera(3);
            printplayers(joinedlobby);

            Debug.Log(lobbyName + " is created with " + maxPlayers + " max players. Code: " + lobby.LobbyCode);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }


    public async void ListLobbies()
    {
        try
        {
            foreach (GameObject item in lobbyItems)
            {
                Destroy(item);
            }
            lobbyItems.Clear();
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>{
                    new QueryFilter( QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder ( false,QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("lobbies found : " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                GameObject newLobbyItem = Instantiate(lobbyItemPrefab, contentPanel);
                lobbyItems.Add(newLobbyItem);

                // Update UI
                newLobbyItem.transform.Find("LobbyNameText").GetComponent<TMP_Text>().text = lobby.Name;
                newLobbyItem.transform.Find("PlayerCountText").GetComponent<TMP_Text>().text =
                    $"{lobby.Players.Count} / {lobby.MaxPlayers}";


                Button joinButton = newLobbyItem.transform.Find("JoinButton").GetComponent<Button>();
                joinButton.onClick.AddListener(() => JoinLobbyById(lobby.Id));

            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }

    }
    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            // Use the correct options class for joining by ID
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = Getplayer()
            };

            // Step 2: Join the lobby asynchronously and await the result
            joinedlobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByIdOptions);

            if (joinedlobby != null)
            {
                Debug.Log("Joined lobby: " + lobbyId);

                // Step 3: Update UI after successfully joining the lobby
                LobbyUIManager.UpdateLobbyUIJoin();

                // Step 4: Switch camera view or perform other necessary actions
                CameraSwitcher.SwitchCamera(3);

                // Step 5: Ensure player slots are updated and prefabs are activated
                printplayers(joinedlobby);
            }
            else
            {
                Debug.LogError("Failed to join lobby: Lobby data is null.");
            }
        }
        catch (LobbyServiceException ex)
        {
            // Step 6: Handle any errors that occur during the process
            Debug.LogError($"Error joining lobby: {ex.Message}");
        }
    }



    public async void JoinlobbyByCode(string lobbyCode)
    {
        try
        {

            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = Getplayer()
            };


            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedlobby = lobby;
            Debug.Log("joined lobby with code " + lobbyCode);


            CameraSwitcher.SwitchCamera(3);
            printplayers(joinedlobby);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }

    }

    private Player Getplayer()
    {
        int availableSlot = GetAvailableSlot(); // Find a free slot

        // If no slot is available, handle it (maybe throw an error or return null)
        if (availableSlot == -1)
        {
            Debug.LogError("No available slots!");
            return null; // Handle this appropriately in your code
        }

        // Assign the slot to the player
        playerSlotAssignments[PlayerName] = availableSlot;

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
        {
            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerName) },
            { "PlayerSlot", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, availableSlot.ToString()) }
        }
        };
    }

    // Function to find an available slot
    private int GetAvailableSlot()
    {
        // Log the current playerSlotAssignments to see its state
        Debug.Log("Current playerSlotAssignments: " + string.Join(", ", playerSlotAssignments.Values.Select(v => v.ToString())));

        // Iterate through all possible slots
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            // If this slot isn't in use, return it as available
            if (!playerSlotAssignments.ContainsValue(i))
            {
                Debug.Log($"Slot {i} is available.");
                return i;
            }
        }

        // If no slot is available, return -1
        Debug.LogWarning("No available slots!");
        return -1;
    }



    private async void QuickJoinLobby()
    {
        try
        {

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            joinedlobby = lobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }


    private void printplayers(Lobby lobby)
    {
        Debug.Log($"Updating lobby players. Total: {lobby.Players.Count}");

        // First deactivate all player prefabs
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            playerPrefabs[i].SetActive(false);
        }

        // Clear previous assignments but keep local player's slot if exists
        var tempAssignments = new Dictionary<string, int>();
        string localPlayerId = AuthenticationService.Instance.PlayerId;
        if (playerSlotAssignments.TryGetValue(localPlayerId, out int localPlayerSlot))
        {
            tempAssignments[localPlayerId] = localPlayerSlot;
        }

        playerSlotAssignments = tempAssignments;
        bool[] occupiedSlots = new bool[playerPrefabs.Length];

        // First pass: Restore assigned slots
        foreach (var player in lobby.Players)
        {
            if (playerSlotAssignments.TryGetValue(player.Id, out int assignedSlot))
            {
                if (assignedSlot >= 0 && assignedSlot < playerPrefabs.Length)
                {
                    GameObject prefab = playerPrefabs[assignedSlot];
                    prefab.SetActive(true);
                    occupiedSlots[assignedSlot] = true;

                    // Set name if available
                    if (player.Data.TryGetValue("PlayerName", out var nameData))
                    {
                        TMP_Text nameText = prefab.transform.Find("PlayerNameText")?.GetComponent<TMP_Text>();
                        if (nameText != null)
                        {
                            nameText.text = nameData.Value;
                        }
                    }

                    Debug.Log($"Player {player.Id} remains in slot {assignedSlot}");
                }
            }
        }

        // Second pass: Assign unassigned players
        foreach (var player in lobby.Players)
        {
            if (playerSlotAssignments.ContainsKey(player.Id)) continue;

            int preferredSlot = -1;
            if (player.Data.TryGetValue("PlayerSlot", out var slotData))
            {
                int.TryParse(slotData.Value, out preferredSlot);
            }

            int slotToAssign = -1;

            // Prefer the slot from lobby data if available
            if (preferredSlot >= 0 && preferredSlot < playerPrefabs.Length && !occupiedSlots[preferredSlot])
            {
                slotToAssign = preferredSlot;
            }
            else
            {
                // Otherwise, find the first available
                for (int i = 0; i < playerPrefabs.Length; i++)
                {
                    if (!occupiedSlots[i])
                    {
                        slotToAssign = i;
                        break;
                    }
                }
            }

            if (slotToAssign != -1)
            {
                GameObject prefab = playerPrefabs[slotToAssign];
                prefab.SetActive(true);
                occupiedSlots[slotToAssign] = true;

                playerSlotAssignments[player.Id] = slotToAssign;

                // Set name if available
                if (player.Data.TryGetValue("PlayerName", out var nameData))
                {
                    TMP_Text nameText = prefab.transform.Find("PlayerNameText")?.GetComponent<TMP_Text>();
                    if (nameText != null)
                    {
                        nameText.text = nameData.Value;
                    }
                }

                Debug.Log($"Assigned player {player.Id} to slot {slotToAssign}");
            }
            else
            {
                Debug.LogWarning($"No available slot found for player {player.Id}");
            }
        }
    }


    private async void UpdatePlayerName(string newPlayername)
    {
        try
        {

            PlayerName = newPlayername;
            await LobbyService.Instance.UpdatePlayerAsync(joinedlobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions()
            {
                Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerName) }
            }
            }
                );
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private async void UpdatePlayerPrefab(int prefabIndex)
    {
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            playerPrefabIndices[playerId] = prefabIndex;

            await LobbyService.Instance.UpdatePlayerAsync(joinedlobby.Id, playerId, new UpdatePlayerOptions()
            {
                Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerPrefab", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, prefabIndex.ToString()) },
                { "PlayerSlot", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerSlotAssignments[playerId].ToString()) }
            }
            });
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private async void LeaveLobby()
    {
        try
        {


            await LobbyService.Instance.RemovePlayerAsync(joinedlobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private async void KickPlayer()
    {


        try
        {


            await LobbyService.Instance.RemovePlayerAsync(joinedlobby.Id, joinedlobby.Players[1].Id);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }

    }
    public void cutomize()
    {
        if (joinedlobby == null)
        {
            Debug.Log("Not in a lobby.");
            return;
        }

        string playerId = AuthenticationService.Instance.PlayerId;

        for (int i = 0; i < joinedlobby.Players.Count; i++)
        {
            if (joinedlobby.Players[i].Id == playerId)
            {

                CameraSwitcher.SwitchCamera(i + 4);



                return;
            }
        }

        Debug.Log("Player not found in the lobby.");
    }


    private async void PromoteToPartyLeader(string playerId)
    {
        try
        {

            hostlobby = await LobbyService.Instance.UpdateLobbyAsync(hostlobby.Id, new UpdateLobbyOptions
            {
                HostId = playerId
            });
            joinedlobby = hostlobby;
            printplayers(hostlobby);

        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }
    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedlobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e.Message);
        }
    }
    private async void UpdateReadyStatus(string playerId, bool isReady)
    {
        try
        {

            await LobbyService.Instance.UpdatePlayerAsync(joinedlobby.Id, playerId, new UpdatePlayerOptions()
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady.ToString()) }
                }
            });
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex.Message);
        }
    }
    private void OnPlayerLeft(string playerId)
    {
        if (playerSlotAssignments.ContainsKey(playerId))
        {
            int slot = playerSlotAssignments[playerId];
            playerPrefabs[slot].SetActive(false); // Deactivate the prefab
            playerSlotAssignments.Remove(playerId); // Free up slot
            Debug.Log($"Player left, slot {slot} is now free.");
        }
    }
    //il game bidha

    public bool IsHost()
    {
        return hostlobby != null && hostlobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public async void StartGame()
    {
        if (!IsHost()) return;

        try
        {
            // Update lobby data to indicate game has started
            await LobbyService.Instance.UpdateLobbyAsync(joinedlobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "true") },
                { "Map", new DataObject(DataObject.VisibilityOptions.Member, gameSceneName) }
            }
            });

            // Host-specific initialization
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.StartHost();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start game: {e}");
        }
    }
    private void RegisterNetworkPrefabs()
    {
        // Get the NetworkPrefabs list from NetworkManager
        foreach (var prefab in playerPrefabs)
        {
            NetworkManager.Singleton.AddNetworkPrefab(prefab);
        }
    }
}
