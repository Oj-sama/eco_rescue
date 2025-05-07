using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class ProximityLootSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public List<Transform> spawnPoints = new List<Transform>();
    public List<GameObject> lootPrefabs = new List<GameObject>();
    public float spawnRadius = 5f;
    public float respawnCooldown = 5f; // Time in seconds before loot can respawn

    private bool hasSpawnedAll = false;

    void Update()
    {
        // Allow all clients (owners and others) to trigger looting
        if (IsPlayerNearby() && Input.GetKeyDown(KeyCode.E) && !hasSpawnedAll)
        {
            hasSpawnedAll = true;
            StartCoroutine(SpawnAndRequestDespawn());
        }
    }

    bool IsPlayerNearby()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= spawnRadius)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator SpawnAndRequestDespawn()
    {
        // Request server to spawn loot objects
        RequestSpawnLootServerRpc();
        yield return new WaitForSeconds(2f); // Give some time before despawn

        // Request despawn from server after loot is spawned
        RequestDespawnServerRpc();

        // Wait for a cooldown before allowing loot to be spawned again
        yield return new WaitForSeconds(respawnCooldown);
        hasSpawnedAll = false;
    }

    [ServerRpc(RequireOwnership = false)] // This allows any client to request loot spawn
    void RequestSpawnLootServerRpc(ServerRpcParams rpcParams = default)
    {
        SpawnLootAtAllPoints();
    }

    void SpawnLootAtAllPoints()
    {
        if (lootPrefabs.Count == 0 || spawnPoints.Count == 0)
        {
            Debug.LogWarning("Missing loot prefabs or spawn points!");
            return;
        }

        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject loot = lootPrefabs[Random.Range(0, lootPrefabs.Count)];
            GameObject spawned = Instantiate(loot, spawnPoint.position, spawnPoint.rotation);

            NetworkObject netObj = spawned.GetComponent<NetworkObject>();
            if (netObj != null && IsServer)
            {
                // Ensure that the spawned loot is visible to all clients by spawning it on the server
                netObj.Spawn();
            }

            Debug.Log("Loot spawned at " + spawnPoint.name);
        }
    }

    [ServerRpc(RequireOwnership = false)] // Allow clients to trigger this RPC too
    void RequestDespawnServerRpc(ServerRpcParams rpcParams = default)
    {
        // Handle despawning on the server
        if (IsServer)
        {
            // Find and despawn loot objects that are networked
            NetworkObject[] lootObjects = GetComponentsInChildren<NetworkObject>();
            foreach (var lootObject in lootObjects)
            {
                lootObject.Despawn();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
