using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

public class CollisionManager : NetworkBehaviour
{
    [SerializeField]
    private NetworkVariable<int> health = new NetworkVariable<int>(20);
    private TextMeshPro healthText;
    private bool isDead = false;
    private HashSet<GameObject> damagedByBalls = new HashSet<GameObject>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        healthText = GetComponentInChildren<TextMeshPro>();
        UpdateHealthText();

        health.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        health.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthText();

        if (newHealth <= 0 && !isDead)
        {
            OnPlayerDeath();
        }
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = health.Value.ToString();
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            health.Value -= damage;
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int damage)
    {
        health.Value -= damage;
    }

    private void OnPlayerDeath()
    {
        isDead = true;

        if (IsServer)
        {
            string playerName = GetComponent<PControllerV2>().playerName.Name;
            Debug.Log($"{playerName} has been destroyed.");

            ShowDeathMessageClientRpc($"{playerName} has been destroyed.");

            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [ClientRpc]
    private void ShowDeathMessageClientRpc(string message)
    {
        Debug.Log(message);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ball") && health.Value > 0)
        {
            if (!damagedByBalls.Contains(collision.gameObject))
            {
                TakeDamage(1);
                damagedByBalls.Add(collision.gameObject);

                NetworkObject ballNetworkObject = collision.gameObject.GetComponent<NetworkObject>();
                if (ballNetworkObject != null)
                {
                    ballNetworkObject.Despawn(true);
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ball"))
        {
            damagedByBalls.Remove(collision.gameObject);
        }
    }
}