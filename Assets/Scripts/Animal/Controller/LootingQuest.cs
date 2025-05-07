using UnityEngine;

public class LootingQuest : MonoBehaviour
{
    public GameObject[] lootPrefabs02;
    public Transform[] lootPoints02;





    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) )
        {
            SpawnLoot02();
            Destroy(gameObject);
        }
    }

    private void SpawnLoot02()
    {
        foreach (var point in lootPoints02)
        {
            try
            {
                var randomIndex = Random.Range(0, lootPrefabs02.Length);
                var selectedLootPrefab = lootPrefabs02[randomIndex];
               
                Instantiate(selectedLootPrefab, point.position + Vector3.up * 0.6f, Quaternion.identity);
            }
            catch
            {
                Debug.Log("Warning suppressed for loot instantiation.");
            }
        }
    }

}
