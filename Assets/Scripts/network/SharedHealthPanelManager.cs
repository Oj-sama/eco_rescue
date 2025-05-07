using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SharedHealthDisplay : MonoBehaviour
{
    [System.Serializable]
    public class SharedHealthBar
    {
        public GameObject panel;
        public TextMeshProUGUI playerNameText;
        public Slider healthSlider;
        public bool isUsed;
    }

    public List<SharedHealthBar> healthBars = new List<SharedHealthBar>();
    public static SharedHealthDisplay Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AssignToSharedPanel(PlayerHealthAndStamina player)
    {
        foreach (var bar in healthBars)
        {
            if (!bar.isUsed)
            {
                bar.isUsed = true;
                bar.panel.SetActive(true);
                bar.playerNameText.text = $"Player {player.OwnerClientId}";
                bar.healthSlider.maxValue = player.maxHealth;
                bar.healthSlider.value = player.currentHealth.Value;

                player.currentHealth.OnValueChanged += (oldVal, newVal) => {
                    bar.healthSlider.value = newVal;
                };
                break;
            }
        }
    }
}