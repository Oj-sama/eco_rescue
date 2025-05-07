using Unity.VisualScripting;
using UnityEngine;
public enum ItemType
{
    melee,
    ranged
}

public  class WeaponBase : MonoBehaviour
{
   

    public bool canDamageTree = false;  // Whether this weapon can damage the tree
    public bool canDamageAnimal = false; // Whether this weapon can damage animals
    public float damageAmount = 100f;    // Damage value
    private bool isAttacking = false;
    public ItemType itemType;

    //ranged
    public Transform firePoint;            // The point from which the projectile is fired
    public float projectileSpeed = 20f;
    public float attackRange = 1.5f;
    public GameObject projectilePrefab;

    public LayerMask damageableLayer;
    public GameObject damageEffectPrefab;
    public float damagetoenv;
    public GameObject env;
    


    public virtual void ApplyDamage(GameObject target, Vector3 hitPoint)
    {
        if (itemType == ItemType.melee)
        {
            if (target.CompareTag("Tree") && canDamageTree)
            {
                treescript treeScript = target.GetComponent<treescript>();
                if (treeScript != null)
                {
                    treeScript.TakeDamage(damageAmount, hitPoint);
                    SpawnDamageEffect(hitPoint);
                }
            }
            else if (target.CompareTag("Animal") && canDamageAnimal)
            {

            }
        }
    }

    // Start the attack (to be overridden)
    public void StartAttack()
    {
        if (itemType == ItemType.melee)
        {
            Collider[] hitObjects = Physics.OverlapSphere(transform.position, attackRange);

            foreach (Collider hitObject in hitObjects)
            {
                if (hitObject.CompareTag("Tree"))
                {
                    Debug.Log("massit chajra");
                    ApplyDamage(hitObject.gameObject, hitObject.transform.position);
                    EnvironmentHealth ennnn= env.GetComponent<EnvironmentHealth>();
                    ennnn.TakeDamage(damagetoenv);
                }
            }
        }
        if (itemType == ItemType.ranged)
        {
            if (projectilePrefab != null && firePoint != null)
            {
                // 1️⃣ Raycast from the center of the screen
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                RaycastHit hit;

                Vector3 targetPoint;
                if (Physics.Raycast(ray, out hit, 100f))
                {
                    // 2️⃣ If we hit something, shoot at that point
                    targetPoint = hit.point;
                }
                else
                {
                    // 3️⃣ If we don't hit anything, shoot in the forward direction
                    targetPoint = ray.GetPoint(100f);
                }

                // 4️⃣ Calculate direction
                Vector3 direction = (targetPoint - firePoint.position).normalized;

                // 5️⃣ Instantiate projectile and shoot
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
                projectile.transform.rotation *= Quaternion.Euler(90, 0, 0);

                Projectile pr = projectile.GetComponent<Projectile>(); // Corrected
                // Adjust rotation if necessary (e.g., if the projectile's forward axis is not Z)
                // projectile.transform.rotation *= Quaternion.Euler(90, 0, 0); // Example adjustment
                pr.dmg=damageAmount;
                Debug.Log("massit chajra");
    
                EnvironmentHealth ennnn = env.GetComponent<EnvironmentHealth>();
                ennnn.TakeDamage(damagetoenv);
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * projectileSpeed; // Use velocity to apply movement
                }

                // Debug: Draw a line to visualize the direction
                Debug.DrawLine(firePoint.position, targetPoint, Color.red, 2f);
            }
        



    }




    }


    // Stop the attack (to be overridden)
    public virtual void StopAttack()
    {
        isAttacking = false;
    }
    public void FireWeapon()
    {
        Debug.Log("Firing projectile...");
        Instantiate(projectilePrefab, transform.position, transform.rotation);
    }
    private void SpawnDamageEffect(Vector3 hitPoint)
    {
        // Instantiate the damage effect at the hitPoint (you need to assign a prefab in the inspector)
        GameObject damageEffect = Instantiate(damageEffectPrefab, hitPoint, Quaternion.identity);

        // Make the damage effect face the camera (or player) direction
        Camera mainCamera = Camera.main; // Get the main camera reference
        if (mainCamera != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - hitPoint;
            directionToCamera.y = 0f; // We usually want to ignore the vertical component for rotation

            // Make the effect face the camera's direction
            Quaternion rotation = Quaternion.LookRotation(directionToCamera);
            damageEffect.transform.rotation = rotation;
        }

        // Optionally, destroy the effect after a certain amount of time (to clean up)
        Destroy(damageEffect, 2f); // Adjust the lifetime of the effect as needed
    }
}
