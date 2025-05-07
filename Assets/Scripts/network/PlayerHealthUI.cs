using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode; 
using System.Collections.Generic;

public class SharedHealthUIManager : MonoBehaviour
{
    [System.Serializable]
    public class HealthSlot
    {
        public GameObject panel;                 // The entire panel GameObject
        public TextMeshProUGUI playerNameText;   // Text displaying player name
        public Slider healthSlider;              // Slider for health
        public bool isUsed = false;              // Internal flag
    }

    public List<HealthSlot> slots; // Assign 3 in the Inspector

    public void AssignPlayerToSlot(string playerName, int maxHealth, NetworkVariable<int> health)
    {
        foreach (var slot in slots)
        {
            if (!slot.isUsed)
            {
                slot.isUsed = true;
                slot.panel.SetActive(true);
                slot.playerNameText.text = playerName;
                slot.healthSlider.maxValue = maxHealth;
                slot.healthSlider.value = health.Value;

                // Listen to health changes
                health.OnValueChanged += (oldVal, newVal) => {
                    slot.healthSlider.value = newVal;
                };

                return;
            }
        }

        Debug.LogWarning("No available shared health slot!");
    }
}
