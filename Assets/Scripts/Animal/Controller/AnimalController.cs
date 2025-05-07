using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;

public class AnimalController : NetworkBehaviour
{
    public AnimalFSM FSM;
    private float GlowRange = 6f;
    public Color glowColor;
    public SkinnedMeshRenderer animalRenderer;
    public bool canLoot = false;
    public Animator Animator;
    public AnimalType animalType;

    [SerializeField]
    public NetworkVariable<float> Health = new NetworkVariable<float>(100f);

    [SerializeField]
    public Slider healthBarSlider;

    public float DetectionRange = 10f;
    private float EatRange = 0.8f;
    public float getEatRange() => EatRange;
    public float PatrolSpeed = 1f;
    public float PatrolZoneRadius = 10f;
    public float EscapeSpeed = 2f;
    public float ChaseSpeed = 2f;
    public int attackDamage = 20;
    public int getattackDamage() => attackDamage;
    private float RotationTime = 2f;
    private bool isMovementStopped = false;
    private Vector3 patrolCenter;
    private GameObject targetPatrolPosition;
    private float savedPatrolSpeed;
    private float savedEscapeSpeed;
    private float savedChaseSpeed;
    public NetworkAnimator NetworkAnimator;

    public List<GameObject> players = new List<GameObject>();

    private void Awake()
    {
        NetworkAnimator = GetComponent<NetworkAnimator>();
        FSM = GetComponent<AnimalFSM>();
        Animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            patrolCenter = transform.position;
            SetNewPatrolPoint();
            FSM.ChangeState(new PatrolState(this));
        }

        Health.OnValueChanged += OnHealthChanged;
        UpdateHealthBar();
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Health.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        UpdateHealthBar();
    }

    private void Update()   
    {
    players.Clear();

    foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
    {
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            players.Add(player);
        }
    }

    foreach (GameObject player in players)
    {
        bool isGlowing = Vector3.Distance(transform.position, player.transform.position) < GlowRange
                         && (FSM.currentState is DeathState);
        EnableGlow(isGlowing);
        canLoot = isGlowing;
    }

        FSM.Update();
        healthBarSlider.gameObject.SetActive(IsPlayerNearby() && !(FSM.currentState is DeathState));

        if (Health.Value <= 0f && !(FSM.currentState is DeathState))
        {
            FSM.ChangeState(new DeathState(this));
            Debug.Log("Animal ready for looting. Press 'E' to loot.");
        }
    }


    private void EnableGlow(bool isGlowing)
    {
        if (animalRenderer != null && animalRenderer.materials.Length > 0)
        {
            foreach (var material in animalRenderer.materials)
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", isGlowing ? glowColor : Color.red);
                    if (isGlowing)
                        material.EnableKeyword("_EMISSION");
                    else
                        material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    public bool IsPlayerNearby()
    {
        foreach (GameObject player in players)
        {
            if (Vector3.Distance(transform.position, player.transform.position) < 20)
                return true;
        }
        return false;
    }

    public bool DetectPlayerAndPredator()
    {
        return DetectObjectInRange("Player") || DetectObjectInRange("Predator");
    }

    public bool DetectPlayerAndPrey()
    {
        return DetectObjectInRange("Player") || DetectObjectInRange("Prey");
    }

    public GameObject GetNearbyFood()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("AnimalFood");
        foreach (GameObject obj in objects)
        {
            if (Vector3.Distance(transform.position, obj.transform.position) < EatRange)
                return obj;
        }
        return null;
    }

    public bool IsPlayerFacingAnimal()
    {
        foreach (GameObject player in players)
        {
            Vector3 directionToAnimal = transform.position - player.transform.position;
            directionToAnimal.y = 0;

            if (Vector3.Dot(player.transform.forward, directionToAnimal.normalized) > 0.1f)
                return true;
        }
        return false;
    }

    public void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = Health.Value / 100f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsServer)
        {
            Health.Value -= damage;
            if (Health.Value <= 0f)
            {
                Health.Value = 0f;
                FSM.ChangeState(new DeathState(this));
            }
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage)
    {
        Health.Value -= damage;
        if (Health.Value <= 0f)
        {
            Health.Value = 0f;
            FSM.ChangeState(new DeathState(this));
        }
    }
    public void Patrol()
    {
        if (!IsServer) return;

        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetPatrolPosition.transform.position.x, 0, targetPatrolPosition.transform.position.z)) < 1f)
        {
            SetNewPatrolPoint();
        }
        NetworkAnimator.SetTrigger("Patrol");
        MoveToTarget(targetPatrolPosition.transform.position, PatrolSpeed);
    }
    public void MoveToTarget(Vector3 targetPosition, float speed)
    {
        if (isMovementStopped) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        targetPosition.y = transform.position.y;

        if (direction == Vector3.zero) return;

        if (IsPathClear(direction))
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * RotationTime);
        }
        else
        {
            Vector3 adjustedDirection = GetAdjustedDirection(direction);

            if (adjustedDirection != Vector3.zero)
            {
                transform.position += adjustedDirection * (speed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(adjustedDirection), Time.deltaTime * RotationTime);
            }
        }
    }
    public void StopMovement()
    {
        isMovementStopped = true;
    }
    public void ResumeMovement()
    {
        isMovementStopped = false;
    }

    public void SaveCurrentSpeeds()
    {
        savedPatrolSpeed = PatrolSpeed;
        savedEscapeSpeed = EscapeSpeed;
        savedChaseSpeed = ChaseSpeed;
    }

    public void RestoreSavedSpeeds()
    {
        PatrolSpeed = savedPatrolSpeed;
        EscapeSpeed = savedEscapeSpeed;
        ChaseSpeed = savedChaseSpeed;
    }
    public void PausePatrol()
    {
        isMovementStopped = true;
    }

    public void ResumePatrol()
    {
        isMovementStopped = false;
    }
    private bool DetectObjectInRange(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);

            if (distance > DetectionRange)
                continue;

            if (IsRayBlockedByGreenZone(obj))
                continue;

            return true;
        }
        return false;
    }
    public bool IsRayBlockedByGreenZone(GameObject target)
    {
        RaycastHit hit;
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.transform.position);

        int layerMask = LayerMask.GetMask("GreenZone");

        return Physics.Raycast(transform.position, directionToTarget, out hit, DetectionRange, layerMask);
    }

    public bool IsPathClear(Vector3 direction)
    {
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("GreenZone");
        int layerMaskTree = LayerMask.GetMask("Tree");
        direction.y = 0;

        if (Physics.Raycast(transform.position, direction, out hit, 6f, layerMaskTree))
        {
            SetNewPatrolPoint();
            return false;
        }

        if (animalType == AnimalType.Predator && Physics.Raycast(transform.position, direction, out hit, 6f, layerMask))
        {
            return false;
        }

        return true;
    }

    public void SetNewPatrolPoint()
    {
        if (targetPatrolPosition != null)
        {
            Destroy(targetPatrolPosition);
        }

        GameObject newPatrolPoint = null;
        bool isValidPatrolPoint = false;
        int maxAttempts = 50;
        int attempts = 0;

        while (!isValidPatrolPoint && attempts < maxAttempts)
        {
            attempts++;

            Vector3 randomDirection = Random.insideUnitSphere * PatrolZoneRadius;
            randomDirection.y = 0;

            GameObject patrolPointsContainer = GameObject.FindGameObjectWithTag("PatrolPoints");

            if (patrolPointsContainer == null)
            {
                Debug.LogError("No GameObject found with the 'PatrolPoints' tag.");
                return;
            }

            newPatrolPoint = new GameObject("PatrolPoint");
            newPatrolPoint.transform.SetParent(patrolPointsContainer.transform);
            newPatrolPoint.transform.position = patrolCenter + randomDirection;

            if (Vector3.Distance(transform.position, newPatrolPoint.transform.position) < 2f)
                continue;

            if (!IsRayBlockedByGreenZone(newPatrolPoint) && !IsNearGreenZone(newPatrolPoint))
            {
                targetPatrolPosition = newPatrolPoint;
                isValidPatrolPoint = true;
            }
            else
            {
                Destroy(newPatrolPoint);
            }
        }

        if (!isValidPatrolPoint)
        {
            Debug.LogWarning("Could not find a valid patrol point after " + maxAttempts + " attempts.");
        }
    }
    private bool IsNearGreenZone(GameObject patrolPoint)
    {
        Collider[] hitColliders = Physics.OverlapSphere(patrolPoint.transform.position, 4f, LayerMask.GetMask("GreenZone"));
        return hitColliders.Length > 0;
    }
    private Vector3 GetAdjustedDirection(Vector3 originalDirection)
    {
        Vector3 flatDirection = new Vector3(originalDirection.x, 0, originalDirection.z).normalized;

        Vector3 leftDirection = Quaternion.Euler(0, -45, 0) * flatDirection;
        Vector3 rightDirection = Quaternion.Euler(0, 45, 0) * flatDirection;
        Vector3 backwardDirection = -flatDirection;

        if (IsPathClear(leftDirection)) return leftDirection;
        if (IsPathClear(rightDirection)) return rightDirection;
        if (IsPathClear(backwardDirection)) return backwardDirection;
        return Vector3.zero;
    }

}
