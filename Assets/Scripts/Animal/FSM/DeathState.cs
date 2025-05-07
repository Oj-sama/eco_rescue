using UnityEngine;

public class DeathState : IState
{
    private AnimalController animalController;
    private Animator animator;

    public DeathState(AnimalController controller)
    {
        animalController = controller;
        animator = controller.Animator;
    }

    public void Enter()
    {
        animator.SetTrigger("Death");
        Debug.Log("Animal has died. Entering Death State.");
    }

    public void Exit() { }

    public void Update()
    {
    }
}
