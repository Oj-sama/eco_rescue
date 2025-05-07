using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public AnimalFoodManager manager;
    public Vector3 position;
    private bool isBeingDestroyed = false;

    public void MarkForDestruction()
    {
        if (isBeingDestroyed) return;

        isBeingDestroyed = true;
        if (manager != null)
        {
            manager.OnFoodEaten(position);
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!isBeingDestroyed && manager != null)
        {
            manager.OnFoodEaten(position);
        }
    }
}