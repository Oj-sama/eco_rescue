using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AnimalFoodManager : NetworkBehaviour
{
    public List<GameObject> foodPrefabs;
    public float respawnTime = 10f;
    public int maxFoodPerZone = 8;
    public int minFoodPerZone = 3;
    public float minSpawnDistance = 2f;

    private List<Transform> foodZones;

    // Track food spawn positions
    private NetworkList<Vector3> spawnedFoodPositions;
    private Dictionary<Vector3, GameObject> activeFood = new Dictionary<Vector3, GameObject>();

    private void Awake()
    {
        // Initialize NetworkList
        spawnedFoodPositions = new NetworkList<Vector3>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("AnimalFoodManager: OnNetworkSpawn() called on server!");
            FindFoodZones();
            SpawnFoodServerRpc();
        }
        else
        {
            Debug.Log("Client joined, syncing food positions...");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            spawnedFoodPositions.Clear();
            activeFood.Clear();
        }
    }

    void FindFoodZones()
    {
        foodZones = new List<Transform>();
        GameObject[] zones = GameObject.FindGameObjectsWithTag("animalFoodZone");

        foreach (GameObject zone in zones)
        {
            foodZones.Add(zone.transform);
        }

        Debug.Log($"Total food zones found: {foodZones.Count}");
    }

    [ServerRpc]
    void SpawnFoodServerRpc()
    {
        foreach (Transform zone in foodZones)
        {
            List<Vector3> spawnedPositions = new List<Vector3>();
            int foodToSpawn = Random.Range(minFoodPerZone, maxFoodPerZone + 1);

            for (int i = 0; i < foodToSpawn; i++)
            {
                Vector3 spawnPosition;
                int attempts = Mathf.Max(20, (int)(10 * minSpawnDistance));

                do
                {
                    spawnPosition = zone.position + new Vector3(
                        Random.Range(-3f * minSpawnDistance, 3f * minSpawnDistance),
                        0,
                        Random.Range(-3f * minSpawnDistance, 3f * minSpawnDistance)
                    );
                    attempts--;
                } while (!IsPositionValid(spawnPosition, spawnedPositions) && attempts > 0);

                if (attempts == 0)
                {
                    spawnPosition = zone.position + new Vector3(
                        Random.Range(-3f, 3f),
                        0,
                        Random.Range(-3f, 3f)
                    );
                }

                spawnedFoodPositions.Add(spawnPosition);
                SpawnFoodAtPositionServerRpc(spawnPosition);
                spawnedPositions.Add(spawnPosition);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnFoodAtPositionServerRpc(Vector3 position)
    {
        if (foodPrefabs.Count == 0) return;

        GameObject randomFoodPrefab = foodPrefabs[Random.Range(0, foodPrefabs.Count)];

        GameObject foodObject = Instantiate(randomFoodPrefab, position, Quaternion.identity);

        NetworkObject foodNetworkObject = foodObject.GetComponent<NetworkObject>();
        if (foodNetworkObject == null)
        {
            foodNetworkObject = foodObject.AddComponent<NetworkObject>();
        }
        foodNetworkObject.Spawn(true);

        FoodItem foodItem = foodObject.AddComponent<FoodItem>();
        foodItem.manager = this;
        foodItem.position = position;

        activeFood[position] = foodObject;
        Debug.Log($"Food spawned at {position}");
    }

    public void OnFoodEaten(Vector3 position)
    {
        if (!IsServer) return;

        if (activeFood.TryGetValue(position, out GameObject foodObject) && foodObject != null)
        {
            NetworkObject foodNetworkObject = foodObject.GetComponent<NetworkObject>();
            if (foodNetworkObject != null && foodNetworkObject.IsSpawned)
            {
                foodNetworkObject.Despawn(true);
            }
            activeFood.Remove(position);
        }

        StartCoroutine(RespawnFoodCoroutine(position));
    }

    private IEnumerator RespawnFoodCoroutine(Vector3 position)
    {
        yield return new WaitForSeconds(respawnTime);

        if (!activeFood.ContainsKey(position) || activeFood[position] == null)
        {
            SpawnFoodAtPositionServerRpc(position);
        }
    }

    bool IsPositionValid(Vector3 position, List<Vector3> spawnedPositions)
    {
        foreach (Vector3 pos in spawnedPositions)
        {
            if (Vector3.Distance(pos, position) < minSpawnDistance)
            {
                return false;
            }
        }
        return true;
    }
}