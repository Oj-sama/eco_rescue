using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class ThrowBall : NetworkBehaviour
{
    public GameObject ballPrefab;
    public Transform throwPoint;
    public float throwForce = 45f;
    public int damageAmount = 10;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(1))
        {
            ThrowBallServerRpc();
        }
    }

    [ServerRpc]
    void ThrowBallServerRpc()
    {
        GameObject ball = Instantiate(ballPrefab, throwPoint.position, throwPoint.rotation);

        BallDamageHandler damageHandler = ball.AddComponent<BallDamageHandler>();
        damageHandler.Initialize(damageAmount, gameObject);

        NetworkObject ballNetworkObject = ball.GetComponent<NetworkObject>();
        ballNetworkObject.Spawn(true);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(throwPoint.forward * throwForce, ForceMode.Impulse);
        }

        StartCoroutine(DestroyBallAfterTime(ballNetworkObject, 5f));
    }

    private IEnumerator DestroyBallAfterTime(NetworkObject ballNetworkObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ballNetworkObject != null && ballNetworkObject.IsSpawned)
        {
            ballNetworkObject.Despawn(true);
        }
    }

    private class BallDamageHandler : NetworkBehaviour
    {
        private int damage;
        private GameObject owner;
        private bool hasDamaged;

        public void Initialize(int damageAmount, GameObject ownerObject)
        {
            damage = damageAmount;
            owner = ownerObject;
            hasDamaged = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || hasDamaged) return;

            GameObject target = collision.gameObject;
            if (target == owner) return;

            if (IsValidTarget(target) && !IsTargetDead(target))
            {
                ApplyDamage(target);
                hasDamaged = true;

                NetworkObject ballNetworkObject = GetComponent<NetworkObject>();
                if (ballNetworkObject != null && ballNetworkObject.IsSpawned)
                {
                    ballNetworkObject.Despawn(true);
                }
            }
        }

        private bool IsValidTarget(GameObject target)
        {
            return target.GetComponent<AnimalController>() != null ||
                   target.GetComponent<playerController>() != null ||
                   target.GetComponent<PlayerHealthAndStamina>() != null;
        }

        private bool IsTargetDead(GameObject target)
        {
            var animal = target.GetComponent<AnimalController>();
            var player = target.GetComponent<PlayerHealthAndStamina>();

            return (animal != null && animal.Health.Value <= 0f) ||
                   (player != null && player.currentHealth.Value <= 0f);
        }

        private void ApplyDamage(GameObject target)
        {
            var animal = target.GetComponent<AnimalController>();
            var player = target.GetComponent<PlayerHealthAndStamina>();

            if (animal != null)
            {
                animal.TakeDamage(damage);
                Debug.Log($"Ball hit animal for {damage} damage");
            }
            else if (player != null)
            {
                player.TakeDamageServerRpc(damage);
                Debug.Log($"Ball hit player for {damage} damage");
            }
        }
    }
}