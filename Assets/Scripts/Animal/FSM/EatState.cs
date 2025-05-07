using UnityEngine;
using Unity.Netcode;

public class EatState : IState
{
    private AnimalController animalController;
    private Animator animator;
    private float eatingDuration = 6f;
    private float eatingTimer = 0f;
    private GameObject nearbyFood = null;
    private bool isEating = false;

    public EatState(AnimalController controller)
    {
        animalController = controller;
        animator = controller.Animator;
    }

    public void Enter()
    {
        if (isEating) return;
        isEating = true;

        Debug.Log("Entering Eat State");
        animalController.PausePatrol();

        nearbyFood = animalController.GetNearbyFood();
        if (nearbyFood != null)
        {
            if (animalController.IsServer)
            {
                TriggerEatAnimationClientRpc();
            }
            eatingTimer = 0f;
        }
        else
        {
            animalController.FSM.ChangeState(new PatrolState(animalController));
        }
    }

    [ClientRpc]
    public void TriggerEatAnimationClientRpc()
    {
        animalController.NetworkAnimator.SetTrigger("Eat");
    }

    public void Exit()
    {
        Debug.Log("Exiting Eat State");
        animalController.ResumePatrol();
        isEating = false;
    }

    public void Update()
    {
        if (nearbyFood != null)
        {
            HandleMovingToFood();
        }

        eatingTimer += Time.deltaTime;

        if (eatingTimer >= eatingDuration * 0.8f)
        {
            if (animalController.IsServer)
            {
                DestroyFood();
            }
        }

        if (eatingTimer >= eatingDuration)
        {
            EndEating();
        }
    }

    private void HandleMovingToFood()
    {
        if (nearbyFood == null) return;

        Vector3 directionToFood = nearbyFood.transform.position - animalController.transform.position;
        if (directionToFood.magnitude > animalController.getEatRange())
        {
            animalController.MoveToTarget(nearbyFood.transform.position, animalController.PatrolSpeed);
        }
    }

    private void DestroyFood()
    {
        if (nearbyFood != null)
        {
            NetworkObject foodNetworkObject = nearbyFood.GetComponent<NetworkObject>();
            if (foodNetworkObject != null)
            {
                foodNetworkObject.Despawn(true);
            }
            nearbyFood = null;
        }
    }

    private void EndEating()
    {
        if (animalController.IsServer)
        {
            animalController.Health.Value = Mathf.Min(animalController.Health.Value + 10f, 100f);
            animalController.FSM.ChangeState(new PatrolState(animalController));
        }
    }
}