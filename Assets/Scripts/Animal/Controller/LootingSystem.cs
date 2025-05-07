using Unity.Netcode;
using UnityEngine;

public class LootingSystem : MonoBehaviour
{
    public GameObject[] lootPrefabs;
    public Transform[] lootPoints;
    private Bank bank;
    private AnimalController animalController;

    private void Start()
    {
        animalController = GetComponent<AnimalController>();

        if (NetworkManager.Singleton.IsServer)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                FindBank();
            }
            else
            {
                NetworkManager.Singleton.OnServerStarted += FindBank;
            }
        }
    }

    private void FindBank()
    {
        GameObject bankObject = GameObject.FindGameObjectWithTag("Bank");
        if (bankObject != null)
        {
             bank = bankObject.GetComponent<Bank>();
        }
        else
        {
            Debug.LogError("Bank object with tag 'Bank' not found in the scene!");
        }

        NetworkManager.Singleton.OnServerStarted -= FindBank;
    }

    private void Update()
    {
        if (animalController.canLoot && Input.GetKeyDown(KeyCode.E) &&
            animalController.FSM.currentState is DeathState && animalController.IsPlayerFacingAnimal())
        {
            SpawnLoot();
            Destroy(gameObject);
        }
    }

    private void SpawnLoot()
    {
        foreach (var point in lootPoints)
        {
            try
            {
                var randomIndex = Random.Range(0, lootPrefabs.Length);
                var selectedLootPrefab = lootPrefabs[randomIndex];

                if (bank != null)
                {
                    bank.GiveMoney();
                }

                Instantiate(selectedLootPrefab, point.position + Vector3.up * 0.6f, Quaternion.identity);
            }
            catch
            {
                Debug.Log("Warning suppressed for loot instantiation.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            animalController.canLoot = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            animalController.canLoot = false;
        }
    }
}