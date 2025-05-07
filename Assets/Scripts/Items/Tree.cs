using UnityEngine;

public class treescript : MonoBehaviour
{
    public float health = 100f;
    private float currentHealth;
    public GameObject woodPrefab; // Prefab for the wood item dropped when the tree is chopped
    void Start()
    {
        currentHealth = health;
    }
    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        currentHealth -= damage;
        // Apply scratch marks or damage effect at the hit point
        ApplyScratch(hitPoint);

        if (currentHealth <= 0)
        {
            // Tree falls or gets destroyed
            ChopDown();
        }
    }

    private void ChopDown()
    {
        // Drop wood items
        if (woodPrefab != null)
        {
            Instantiate(woodPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the tree
        Destroy(gameObject);
    }
    void ApplyScratch(Vector3 hitPoint)
    {
        // Place a scratch mark based on damage or hit point
    }
}