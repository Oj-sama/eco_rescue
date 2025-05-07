using UnityEngine;
using System.Collections;

public class AttackState : IState
{
    private AnimalController animalController;
    private Animator animator;
    private GameObject target;
    private bool hasDamaged = false;
    private bool isCoolingDown = false;

    public AttackState(AnimalController controller, GameObject attackTarget)
    {
        animalController = controller;
        animator = controller.Animator;
        target = attackTarget;
    }

    public void Enter()
    {
        Debug.Log("Entering Attack State");

        animator.SetTrigger("Attack");
        animator.SetBool("isMoving", false);
        hasDamaged = false;
        isCoolingDown = false;

        animalController.StopMovement();
    }

    public void Exit()
    {
        Debug.Log("Exiting Attack State");
        animator.ResetTrigger("Attack");
        animator.SetBool("isMoving", true);
        isCoolingDown = false;

        animalController.ResumeMovement();
    }

    public void Update()
    {
        if (target == null || !IsTargetValid(target))
        {
            Debug.Log("No valid target, switching to Patrol State");
            animalController.FSM.ChangeState(new PatrolState(animalController));
            return;
        }

        if (IsTargetDead(target))
        {
            Debug.Log("Target is dead, switching to Patrol State");
            target.tag = "Dead";
            animalController.FSM.ChangeState(new PatrolState(animalController));
            return;
        }

        if (!hasDamaged && IsWithinAttackRange(target))
        {
            ApplyDamage(target);
            hasDamaged = true;

            if (!isCoolingDown)
            {
                animalController.StartCoroutine(DelayBeforeChasing());
            }
        }
        else if (!isCoolingDown)
        {
            Debug.Log("Cooldown finished, chasing again.");
            animalController.FSM.ChangeState(new ChaseState(animalController));
        }
    }

    private IEnumerator DelayBeforeChasing()
    {
        isCoolingDown = true;
        yield return new WaitForSeconds(1f);
        isCoolingDown = false;

        if (target != null && IsWithinAttackRange(target))
        {
            Debug.Log("Cooldown over, chasing target.");
            animalController.FSM.ChangeState(new ChaseState(animalController));
        }
        else
        {
            Debug.Log("Target moved out of range, switching to Patrol.");
            animalController.FSM.ChangeState(new PatrolState(animalController));
        }
    }



    private bool IsTargetValid(GameObject target)
    {
        return target.GetComponent<AnimalController>() != null || target.GetComponent<playerController>() != null;
    }

    private bool IsTargetDead(GameObject target)
    {
        var targetAnimal = target.GetComponent<AnimalController>();
        var targetPlayer = target.GetComponent<PlayerHealthAndStamina>();

        return (targetAnimal != null && targetAnimal.Health.Value <= 0f) ||
               (targetPlayer != null && targetPlayer.currentHealth.Value <= 0f);
    }

    private bool IsWithinAttackRange(GameObject target)
    {
        return Vector3.Distance(animalController.transform.position, target.transform.position) <= 3f;
    }

    private void ApplyDamage(GameObject target)
    {
        int attackDamage = animalController.getattackDamage();

        var targetAnimal = target.GetComponent<AnimalController>();
        var targetPlayer = target.GetComponent<PlayerHealthAndStamina>();

        if (targetAnimal != null)
        {
            targetAnimal.TakeDamage(attackDamage);
            Debug.Log($"Target animal damaged by {attackDamage}. Remaining Health: {targetAnimal.Health}");
        }
        else if (targetPlayer != null)
        {
            targetPlayer.TakeDamageServerRpc(attackDamage);
            Debug.Log($"Target player damaged by {attackDamage}. Remaining Health: {targetPlayer.currentHealth}");
        }
    }
}
