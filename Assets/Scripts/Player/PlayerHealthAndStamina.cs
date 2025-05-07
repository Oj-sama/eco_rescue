using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthAndStamina : NetworkBehaviour
{
    [Header("UI")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider thirstSlider;
    public Slider hungerSlider;
    public Slider armorSlider;
    public Animator playerAnimator;

    public playerController playerController;

    // Networked player stats
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    public NetworkVariable<float> currentStamina = new NetworkVariable<float>();
    public NetworkVariable<float> currentThirst = new NetworkVariable<float>();
    public NetworkVariable<float> currentHunger = new NetworkVariable<float>();
    public NetworkVariable<int> currentArmor = new NetworkVariable<int>();

    // Limits
    public int maxHealth = 100;
    public float maxStamina = 100f;
    public float staminaDepletionRate = 10f;
    public float staminaRegenRate = 5f;
    public float maxThirst = 100f;
    public float maxHunger = 100f;
    public int maxArmor = 100;
    public PlayerUIPanel assignedPanel;

    // Internal
    private bool isHealthCritical = false;

    private void Start()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            currentStamina.Value = maxStamina;
            currentThirst.Value = maxThirst;
            currentHunger.Value = maxHunger;
            currentArmor.Value = maxArmor;
        }

        SetupSliders();

        // Subscribe to value changes
        currentHealth.OnValueChanged += (_, _) => UpdateSliders();
        currentStamina.OnValueChanged += (_, _) => UpdateSliders();
        currentThirst.OnValueChanged += (_, _) => UpdateSliders();
        currentHunger.OnValueChanged += (_, _) => UpdateSliders();
        currentArmor.OnValueChanged += (_, _) => UpdateSliders();
    }
    

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Assign a panel only for the local player
            assignedPanel = UIManager.Instance.AssignPanelToPlayer(OwnerClientId);

            if (assignedPanel != null)
            {
                healthSlider = assignedPanel.healthSlider;
                staminaSlider = assignedPanel.staminaSlider;
                thirstSlider = assignedPanel.thirstSlider;
                hungerSlider = assignedPanel.hungerSlider;
                armorSlider = assignedPanel.armorSlider;

                SetupSliders();
                UpdateSliders();
            }
        }

        // Keep syncing logic alive even for non-owners (if you want enemy health display)
        currentHealth.OnValueChanged += (_, _) => UpdateSliders();
        currentStamina.OnValueChanged += (_, _) => UpdateSliders();
        currentThirst.OnValueChanged += (_, _) => UpdateSliders();
        currentHunger.OnValueChanged += (_, _) => UpdateSliders();
        currentArmor.OnValueChanged += (_, _) => UpdateSliders();
    }


    void Update()
    {
        if (!IsOwner) return;

        HandleStamina();
        HandleThirstAndHunger();
    }

    private void SetupSliders()
    {
        if (healthSlider != null) healthSlider.maxValue = maxHealth;
        if (staminaSlider != null) staminaSlider.maxValue = maxStamina;
        if (thirstSlider != null) thirstSlider.maxValue = maxThirst;
        if (hungerSlider != null) hungerSlider.maxValue = maxHunger;
        if (armorSlider != null) armorSlider.maxValue = maxArmor;
    }

    private void UpdateSliders()
    {
        if (!IsOwner) return;

        if (healthSlider != null) healthSlider.value = currentHealth.Value;
        if (staminaSlider != null) staminaSlider.value = currentStamina.Value;
        if (thirstSlider != null) thirstSlider.value = currentThirst.Value;
        if (hungerSlider != null) hungerSlider.value = currentHunger.Value;
        if (armorSlider != null) armorSlider.value = currentArmor.Value;
    }

    // Called by others to damage this player
    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        if (currentArmor.Value > 0)
        {
            int remainingDamage = Mathf.Max(damage - currentArmor.Value, 0);
            currentArmor.Value = Mathf.Max(currentArmor.Value - damage, 0);
            damage = remainingDamage;
        }

        currentHealth.Value -= damage;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        if (currentHealth.Value <= 20)
        {
            isHealthCritical = true;
        }
        else
        {
            isHealthCritical = false;
        }

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // Add death behavior here (e.g., respawn logic)
    }

    private void HandleStamina()
    {
        if (playerController == null) return;

        float delta = Time.deltaTime;
        float stamina = currentStamina.Value;

        if (playerController.IsSprinting())
        {
            stamina -= staminaDepletionRate * delta;
        }
        else
        {
            stamina += staminaRegenRate * delta;
        }

        stamina = Mathf.Clamp(stamina, 0, maxStamina);
        currentStamina.Value = stamina;

        if (stamina <= 0f)
        {
            playerController.StopSprinting();
        }
    }

    private void HandleThirstAndHunger()
    {
        float delta = Time.deltaTime;

        currentThirst.Value -= 1f * delta;
        currentHunger.Value -= 1f * delta;

        currentThirst.Value = Mathf.Clamp(currentThirst.Value, 0, maxThirst);
        currentHunger.Value = Mathf.Clamp(currentHunger.Value, 0, maxHunger);

        if (currentThirst.Value <= 0 || currentHunger.Value <= 0)
        {
            TakeDamageServerRpc(5); // Lose health if starving or dehydrated
        }
    }

    [ServerRpc]
    public void DrinkServerRpc(float amount)
    {
        currentThirst.Value = Mathf.Clamp(currentThirst.Value + amount, 0, maxThirst);
    }

    [ServerRpc]
    public void EatServerRpc(float amount)
    {
        currentHunger.Value = Mathf.Clamp(currentHunger.Value + amount, 0, maxHunger);
    }

    [ServerRpc]
    public void RefillArmorServerRpc(int amount)
    {
        currentArmor.Value = Mathf.Clamp(currentArmor.Value + amount, 0, maxArmor);
    }

    public void StartGrabAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsGrabbing", true);
        }
    }

    public void StopGrabAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsGrabbing", false);
        }
    }
}
