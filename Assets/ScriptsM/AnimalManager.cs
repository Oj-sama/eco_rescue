using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Proyecto26;

public class AnimalManager : NetworkBehaviour
{
    public GameObject[] animalPrefabs;
    public float respawnDelay = 10f;

    private Dictionary<GameObject, GameObject> spawnPointToAnimalMap = new();
    private Dictionary<GameObject, float> spawnPointToRespawnTimeMap = new();

    private Dictionary<GameObject, AnimalData> animalToDataMap = new();

    private List<AnimalData> spawnedAnimals = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer || !IsNetworkManagerListening()) return;

        spawnPointToAnimalMap = new Dictionary<GameObject, GameObject>();
        spawnPointToRespawnTimeMap = new Dictionary<GameObject, float>();

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("AnimalSpawnPoint");
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No animal spawn points found in the scene.");
            return;
        }

        foreach (GameObject spawnPoint in spawnPoints)
        {
            SpawnAnimal(spawnPoint);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            Debug.Log("Network despawn - saving animals");
            DebugAnimalState();
            SaveAnimalsToDatabase();
            CleanupAnimals();
        }
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsServer || !IsNetworkManagerListening()) return;

        foreach (var kvp in new Dictionary<GameObject, GameObject>(spawnPointToAnimalMap))
        {
            GameObject spawnPoint = kvp.Key;
            GameObject animal = kvp.Value;

            if (animal == null)
            {
                if (!spawnPointToRespawnTimeMap.ContainsKey(spawnPoint))
                {
                    spawnPointToRespawnTimeMap[spawnPoint] = Time.time + respawnDelay;
                }
                else if (Time.time >= spawnPointToRespawnTimeMap[spawnPoint])
                {
                    SpawnAnimal(spawnPoint);
                    spawnPointToRespawnTimeMap.Remove(spawnPoint);
                }
            }
        }
    }
    [ContextMenu("Debug Animal State")]
    public void DebugAnimalState()
    {
        Debug.Log($"Current animal state:");
        Debug.Log($"Total spawn points: {spawnPointToAnimalMap.Count}");
        Debug.Log($"Total animals to save: {spawnedAnimals.Count}");

        foreach (var animal in spawnedAnimals)
        {
            Debug.Log($"- {animal.attributes.name} (Health: {animal.attributes.health}, Name: {animal.attributes.name})");
        }
    }
    private void SpawnAnimal(GameObject spawnPoint)
    {
        if (!IsNetworkManagerListening())
        {
            Debug.LogWarning("Spawn attempted while NetworkManager isn't listening.");
            return;
        }

        if (animalPrefabs.Length == 0)
        {
            Debug.LogError("No animal prefabs assigned.");
            return;
        }

        GameObject animalPrefab = animalPrefabs[UnityEngine.Random.Range(0, animalPrefabs.Length)];
        GameObject animal = Instantiate(animalPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

        NetworkObject networkObject = animal.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true);
            spawnPointToAnimalMap[spawnPoint] = animal;

            var animalController = animal.GetComponent<AnimalController>();
            if (animalController != null)
            {
                string prefabName = animalPrefab.name;
                var newAnimal = new AnimalData
                {
                    attributes = new AnimalAttributes
                    {
                        name = prefabName,
                        health = (int)animalController.Health.Value,
                        locale = "en-US",
                        animalType = animalController.animalType.ToString()
                    }
                };

                // Add directly to spawnedAnimals list
                spawnedAnimals.Add(newAnimal);
                animalToDataMap[animal] = newAnimal;

                // Subscribe to health changes
                animalController.Health.OnValueChanged += (float oldValue, float newValue) =>
                {
                    UpdateAnimalHealth(animal, (int)newValue);
                };

                Debug.Log($"Spawned and tracked animal: {newAnimal.attributes.name} (Health: {newAnimal.attributes.health}, Type: {newAnimal.attributes.animalType})");
            }
        }
        else
        {
            Debug.LogError("Animal prefab is missing NetworkObject component.");
            Destroy(animal);
        }
    }
    private void UpdateAnimalHealth(GameObject animal, int newHealth)
    {
        if (animalToDataMap.TryGetValue(animal, out AnimalData animalData))
        {
            animalData.attributes.health = newHealth;
            Debug.Log($"Updated health for {animalData.attributes.name} to {newHealth}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestManualSaveServerRpc()
    {
        if (!IsServer) return;
        SaveAnimalsToDatabase(false); // Save without clearing animals
    }

    private void SaveAnimalsToDatabase(bool removeAfterSave = true)
    {
        Debug.Log("Starting SaveAnimalsToDatabase...");

        if (!StrapiComponent.Instance.IsAuthenticated)
        {
            Debug.LogError("Cannot save animals: User is not authenticated.");
            return;
        }

        if (spawnedAnimals.Count == 0)
        {
            Debug.LogWarning("No animals to save - spawnedAnimals list is empty");
            return;
        }

        Debug.Log($"Attempting to save {spawnedAnimals.Count} animals...");

        // Create a copy of the list for iteration while maintaining references
        var animalsToSave = new List<AnimalData>(spawnedAnimals);

        foreach (var animal in animalsToSave)
        {
            try
            {
                // Enhanced ID validation
                bool isUpdate = animal.id > 0;
                string method = isUpdate ? "PUT" : "POST";
                string url = isUpdate ?
                    $"{StrapiComponent.Instance.BaseURL}api/animals/{animal.id}" :
                    $"{StrapiComponent.Instance.BaseURL}api/animals";

                Debug.Log($"Processing animal - ID: {animal.id}, " +
                         $"Name: {animal.attributes.name}, " +
                         $"Health: {animal.attributes.health}");

                var requestBody = new
                {
                    data = new
                    {
                        name = animal.attributes.name,
                        health = animal.attributes.health,
                        users = new List<int> { StrapiComponent.Instance.AuthenticatedUser.id },
                        locale = animal.attributes.locale,
                        animalType = animal.attributes.animalType
                    }
                };

                string jsonString = JsonConvert.SerializeObject(requestBody);
                Debug.Log($"Prepared JSON: {jsonString}");

                var request = new RequestHelper
                {
                    Uri = url,
                    Method = method,
                    BodyString = jsonString,
                    Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", $"Bearer {StrapiComponent.Instance.GetJwtToken()}" }
                },
                    EnableDebug = true
                };

                RestClient.Post(request)
                    .Then(response =>
                    {
                        Debug.Log($"Success response: {response.Text}");

                        var responseData = JsonConvert.DeserializeObject<StrapiAnimalResponse>(response.Text);
                        if (responseData?.data != null)
                        {
                            // Update the original reference in spawnedAnimals
                            var originalAnimal = spawnedAnimals.Find(a =>
                                a.id == animal.id ||  // For updates
                                (a.id == 0 && a == animal)  // For new creations
                            );

                            if (originalAnimal != null)
                            {
                                originalAnimal.id = responseData.data.id;
                                Debug.Log($"Updated ID for {originalAnimal.attributes.name} to {originalAnimal.id}");
                            }
                        }

                        if (removeAfterSave)
                        {
                            spawnedAnimals.Remove(animal);
                        }
                    })
                    .Catch(err =>
                    {
                        Debug.LogError($"Failed to save animal {animal.attributes.name}");
                        if (err is RequestException webErr)
                        {
                            Debug.LogError($"Status code: {webErr.StatusCode}");
                            Debug.LogError($"Response: {webErr.Response}");
                            HandleSaveError(webErr, animal);
                        }
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while saving animal {animal.attributes.name}: {ex.Message}");
            }
        }
    }
    private void HandleSaveError(RequestException error, AnimalData animal)
    {
        Debug.LogError($"Error message: {error.Message}");
        Debug.LogError($"Status code: {error.StatusCode}");
        Debug.LogError($"Response: {error.Response}");

        // Handle 404 errors by converting to POST if needed
        if (error.StatusCode == 404 && animal.id > 0)
        {
            Debug.Log($"Attempting to recreate deleted animal {animal.id}");
            animal.id = 0;
            SaveAnimalsToDatabase();
        }
        // Handle authentication errors
        else if (error.StatusCode == 401 || error.StatusCode == 403)
        {
            Debug.LogError("Authentication failed! Please re-login.");
            // Trigger UI notification or authentication flow here
            ShowAuthenticationErrorUI();
        }
    }

    // Add this new method to handle UI notifications
    private void ShowAuthenticationErrorUI()
    {
        // Implement your UI notification logic here
        Debug.LogError("Show login screen or display error message to user");

        // Example: If you have a UI manager
        // UIManager.Instance.ShowLoginScreen();
        // or
        // UIManager.Instance.DisplayErrorMessage("Session expired - please relogin");
    }

    // Add these new methods for data integrity
    private AnimalData FindOriginalAnimal(AnimalData searchAnimal)
    {
        return spawnedAnimals.Find(a =>
            a.id == searchAnimal.id ||  // Match by ID
            (a.id == 0 && a == searchAnimal)  // Match new animals by reference
        );
    }
    // Add this context menu for testing in-editor
    [ContextMenu("Manual Save Animals")]
    private void ManualSaveTest()
    {
        if (IsServer) RequestManualSaveServerRpc();
    }

    private void CleanupAnimals()
    {
        foreach (var animal in spawnPointToAnimalMap.Values)
        {
            if (animal != null)
            {
                NetworkObject netObj = animal.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
                Destroy(animal);
            }
        }
        spawnPointToAnimalMap.Clear();
        spawnPointToRespawnTimeMap.Clear();
        animalToDataMap.Clear();
    }

    private bool IsNetworkManagerListening()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }
}

[System.Serializable]
public class AnimalData
{
    public int id;
    public AnimalAttributes attributes;
}

[System.Serializable]
public class AnimalAttributes
{
    public string name;
    public int health;
    public List<int> users;
    public string locale;
    public string animalType; 
}

[System.Serializable]
public class StrapiAnimalResponse
{
    public AnimalData data;
    public object meta;
}

[System.Serializable]
public class StrapiAnimalRequest
{
    public AnimalAttributes data;
}

[System.Serializable]
public class AnimalResponse : AnimalData
{
    public string documentId;
}

[System.Serializable]
public class AnimalResponseAttributes : AnimalAttributes
{
    public string documentId;
}

[System.Serializable]
public class StrapiResponse<T>
{
    public List<T> data;
}

[System.Serializable]
public class StrapiPostData<T>
{
    public T data;
}