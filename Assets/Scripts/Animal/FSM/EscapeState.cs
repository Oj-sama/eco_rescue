using UnityEngine;

public class EscapeState : IState
{
    private AnimalController animalController;
    private Animator animator;
    private Vector3 escapeTarget;
    private bool hasEscapeTarget;

    public EscapeState(AnimalController controller)
    {
        animalController = controller;
        animator = controller.Animator;
    }

    public void Enter()
    {
        hasEscapeTarget = false;
        TriggerEscapeAnimation();
        Debug.Log("Entering Escape State");
    }

    public void Exit()
    {
        Debug.Log("Exiting Escape State");
    }

    public void Update()
    {
        if (!hasEscapeTarget)
        {
            GameObject nearestThreat = FindNearestThreat();
            if (nearestThreat != null)
            {
                Vector3 direction = (animalController.transform.position - nearestThreat.transform.position).normalized;
                escapeTarget = animalController.transform.position + direction * 5f;
                escapeTarget.y = animalController.transform.position.y;

                GameObject greenZone = FindNearestGreenZone();
                if (greenZone != null)
                {
                    Vector3 greenZoneDirection = (greenZone.transform.position - animalController.transform.position).normalized;
                    escapeTarget = Vector3.Lerp(escapeTarget, greenZone.transform.position, 0.5f);
                }

                escapeTarget = AvoidOtherAnimals(escapeTarget);
                hasEscapeTarget = true;
            }
        }

        if (hasEscapeTarget)
        {
            animalController.MoveToTarget(new Vector3(escapeTarget.x, animalController.transform.position.y, escapeTarget.z), animalController.EscapeSpeed);
            TriggerEscapeAnimation();

            if (Vector3.Distance(new Vector3(animalController.transform.position.x, 0, animalController.transform.position.z),
                                 new Vector3(escapeTarget.x, 0, escapeTarget.z)) < 0.4f)
            {
                hasEscapeTarget = false;
                if (!animalController.DetectPlayerAndPredator())
                {
                    animalController.FSM.ChangeState(new PatrolState(animalController));
                }
            }
        }
    }

    private GameObject FindNearestThreat()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] predators = GameObject.FindGameObjectsWithTag("Predator");

        float closestDistance = Mathf.Infinity;
        GameObject nearestThreat = null;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(animalController.transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestThreat = player;
            }
        }

        foreach (GameObject predator in predators)
        {
            float distance = Vector3.Distance(animalController.transform.position, predator.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestThreat = predator;
            }
        }

        return nearestThreat;
    }

    private GameObject FindNearestGreenZone()
    {
        GameObject[] greenZones = GameObject.FindGameObjectsWithTag("GreenZone");

        float closestDistance = Mathf.Infinity;
        GameObject nearestGreenZone = null;

        foreach (GameObject greenZone in greenZones)
        {
            float distance = Vector3.Distance(animalController.transform.position, greenZone.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestGreenZone = greenZone;
            }
        }

        return nearestGreenZone;
    }

    private Vector3 AvoidOtherAnimals(Vector3 target)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] predators = GameObject.FindGameObjectsWithTag("Predator");

        GameObject[] allAnimals = new GameObject[players.Length + predators.Length];
        players.CopyTo(allAnimals, 0);
        predators.CopyTo(allAnimals, players.Length);

        foreach (GameObject animal in allAnimals)
        {
            Vector3 directionToAnimal = animal.transform.position - animalController.transform.position;
            float distanceToAnimal = Vector3.Distance(animalController.transform.position, animal.transform.position);

            if (distanceToAnimal < 10f)
            {
                target -= directionToAnimal.normalized * 3f;
            }
        }
        return target;
    }

    private void TriggerEscapeAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Escape");
        }
    }
}
