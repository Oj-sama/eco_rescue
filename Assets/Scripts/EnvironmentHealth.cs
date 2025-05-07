using NUnit;
using UnityEngine;
using UnityEngine.UI;  // Required for using UI components

public class EnvironmentHealth : MonoBehaviour
{
    public Slider healthSlider; // Reference to the UI slider
    public float maxHealth = 100f;
    private float currentHealth;
    public GameObject task1;
    public GameObject task2;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Method to decrease health
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Ensure health stays in range
        UpdateHealthUI();
        CheckHealthStatus();
    }

    // Method to increase health
    public void AddHealth(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
        CheckHealthStatus();
    }

    // Update the slider UI
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth; // Normalize health (0 to 1)
        }
    }

    // Check health levels and trigger actions
    private void CheckHealthStatus()
    {
       

        if (currentHealth < 70)
        {
            Debug.Log("🔶 Environment health is at 70! Applying medium damage effects...");
            task2.gameObject.SetActive(true);
            if (currentHealth < 50)
            {
                Debug.Log("⚠️ Environment health is at 50! Triggering a warning effect...");
                // Add your action here (e.g., change color, spawn enemies, etc.)
                task1.gameObject.SetActive(true);
            }
            else
            {
                task1.gameObject.SetActive(false);
            }
        }
        else 
        {
            task2.gameObject.SetActive(false);
        }
    }
}
