using UnityEngine;

public class PatrolState : IState
{
    private AnimalController animalController;
    private Animator animator;

    public PatrolState(AnimalController controller)
    {
        animalController = controller;
        animator = controller.Animator;
    }

    public void Enter()
    {
        animator.SetTrigger("Patrol");
    }
    public void Exit()
    {
    }

    public void Update()
    {
        animalController.Patrol();

        if (animalController.animalType == AnimalType.Predator && animalController.DetectPlayerAndPrey())
        {
            animalController.FSM.ChangeState(new ChaseState(animalController));
            return;
        }
        if (animalController.animalType == AnimalType.Prey && animalController.DetectPlayerAndPredator())
        {
            animalController.FSM.ChangeState(new EscapeState(animalController));
            return;
        }
        if (animalController.GetNearbyFood() && !animalController.DetectPlayerAndPredator())
        {
            animalController.FSM.ChangeState(new EatState(animalController));
            return;
        }
    }
}
