using UnityEngine;

public class Projectile : MonoBehaviour
{
    public delegate void HitEvent(Vector3 hitPoint, GameObject target);
    public event HitEvent OnHit;
    public float dmg;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Prey")|| collision.gameObject.CompareTag("Predator"))
        {
            // Check if the animal has a script with a GetDamage() function
            AnimalController animal = collision.gameObject.GetComponent<AnimalController>();
            if (animal != null)
            {
                animal.TakeDamage(dmg);
            }

            // Trigger the hit event
            OnHit?.Invoke(collision.contacts[0].point, collision.gameObject);

            // Destroy the projectile after hitting
            Destroy(gameObject);
        }
    }
}
