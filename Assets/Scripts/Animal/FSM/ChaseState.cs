using UnityEngine;

public class ChaseState : IState
{
    private AnimalController animalController;
    private Animator animator;
    private GameObject target;
    private bool isChaseCooldownActive = false;
    private float chaseCooldownTime = 60f;
    private float chaseCooldownTimer = 0f;

    private float maxChaseDistance = 18f;

    public ChaseState(AnimalController controller)
    {
        animalController = controller;
        animator = controller.Animator;
    }

    public void Enter()
    {
        animator.SetTrigger("Chase");
        target = FindNearestTarget();
    }

    public void Exit()
    {
        animator.ResetTrigger("Chase");
    }

    public void Update()
    {
        if (target == null)
        {
            target = FindNearestTarget();
        }

        if (target != null)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Chase"))
            {
                animator.SetTrigger("Chase");
            }

            if (Vector3.Distance(animalController.transform.position, target.transform.position) > maxChaseDistance)
            {
                animalController.FSM.ChangeState(new PatrolState(animalController));
                return;
            }

            if (Vector3.Distance(animalController.transform.position, target.transform.position) <= 3f)
            {
                animalController.FSM.ChangeState(new AttackState(animalController, target));
                return;
            }

            if (isChaseCooldownActive)
            {
                chaseCooldownTimer += Time.deltaTime;
                if (chaseCooldownTimer >= chaseCooldownTime)
                {
                    isChaseCooldownActive = false;
                    chaseCooldownTimer = 0f;
                }
                else
                {
                    return;
                }
            }

            if (animalController.IsPathClear((target.transform.position - animalController.transform.position).normalized))
            {
                animalController.MoveToTarget(target.transform.position, animalController.ChaseSpeed);
            }
            else
            {
                isChaseCooldownActive = true;
                animalController.FSM.ChangeState(new PatrolState(animalController));
            }
        }
        else
        {
            animalController.FSM.ChangeState(new PatrolState(animalController));
        }
    }

    private GameObject FindNearestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] preyAnimals = GameObject.FindGameObjectsWithTag("Prey");

        float closestDistance = Mathf.Infinity;
        GameObject nearestTarget = null;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(animalController.transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestTarget = player;
            }
        }

        foreach (GameObject prey in preyAnimals)
        {
            float distance = Vector3.Distance(animalController.transform.position, prey.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestTarget = prey;
            }
        }
        return nearestTarget;
    }
}