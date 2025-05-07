using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class playerController : NetworkBehaviour
{
    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float backwardSpeed = 3f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public Transform cameraTransform;
    public float pickupRange = 2f;
    public BeltInventory inventory;
    private GameObject currentWeapon;
    public GameObject cinemachine;

    [Header("References")]
    public CharacterController controller;
    public Animator animator;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting = false;
    private bool isdoingsomth = false;
    private GameObject nearestCollectable;
    private bool isAiming = false;
    public GameObject Croshair;
    public Inventory inv;

    private void Start()
    {
        if (!IsOwner)
        {
            controller.enabled = false;
            cinemachine.SetActive(false);
            return;
        }

        if (inventory == null) inventory = GetComponent<BeltInventory>();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (isdoingsomth) return;

        if (currentWeapon != null)
        {
            if (currentWeapon.GetComponent<WeaponBase>().itemType == ItemType.ranged)
            {
                if (Input.GetButtonDown("Fire2")) StartAiming();
                else if (Input.GetButtonUp("Fire2")) StopAiming();
                if (isAiming && Input.GetButtonDown("Fire1")) UseWeapon();
            }
            else
            {
                Croshair.SetActive(false);
                if (Input.GetButtonDown("Fire1")) UseWeapon();
            }
        }

        if (Input.GetKeyDown(KeyCode.G)) DropWeapon();

        FindNearestCollectable();
        if (Input.GetKeyDown(KeyCode.E)) TryPickupItem();

        handlemovement();
    }

    void handlemovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * vertical + camRight * horizontal).normalized;

        float speed = moveSpeed;
        if (vertical > 0) speed = isSprinting ? runSpeed : moveSpeed;
        else if (vertical < 0) speed = backwardSpeed;

        if (moveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            controller.Move(moveDirection * speed * Time.deltaTime);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayTriggerAnimServerRpc("Jump");
        }

        if (Input.GetKey(KeyCode.LeftShift) && vertical > 0) StartSprinting();
        else StopSprinting();

        UpdateAnimStatesServerRpc(moveDirection.magnitude, isGrounded);
    }

    public bool IsSprinting() => isSprinting;

    public void StopSprinting()
    {
        isSprinting = false;
        PlayBoolAnimServerRpc("IsSprinting", false);
    }

    private void StartSprinting()
    {
        isSprinting = true;
        PlayBoolAnimServerRpc("IsSprinting", true);
    }

    void FindNearestCollectable()
    {
        GameObject[] collectables = GameObject.FindGameObjectsWithTag("Collectable");
        nearestCollectable = null;
        float closestDistance = pickupRange;

        foreach (GameObject collectable in collectables)
        {
            float distance = Vector3.Distance(transform.position, collectable.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestCollectable = collectable;
            }
        }
    }

    void TryPickupItem()
    {
        if (nearestCollectable != null)
        {
            Collectable collectable = nearestCollectable.GetComponent<Collectable>();
            if (collectable != null)
            {
                isdoingsomth = true;
                PlayTriggerAnimServerRpc("collect");
                StartCoroutine(TriggerPickupWithDelay(collectable));
            }
        }
    }

    IEnumerator TriggerPickupWithDelay(Collectable collectable)
    {
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        float delay = Mathf.Max(0, animationLength - 1f);
        yield return new WaitForSeconds(delay);
        inventory.AddItemToSlot(collectable);
        collectable.Pickup();
        isdoingsomth = false;
    }

    void UseWeapon()
    {
        if (currentWeapon != null)
        {
            WeaponBase weaponScript = currentWeapon.GetComponent<WeaponBase>();
            if (weaponScript != null)
            {
                if (weaponScript.itemType == ItemType.melee)
                {
                    PlayTriggerAnimServerRpc("MeleeAttack");
                }
                else if (weaponScript.itemType == ItemType.ranged)
                {
                    PlayTriggerAnimServerRpc("BowAttack");
                }

                weaponScript.StartAttack();
            }
        }
    }

    void DropWeapon()
    {
        if (currentWeapon != null)
        {
            inventory.DropSelectedItem();
            Destroy(currentWeapon);
            currentWeapon = null;
        }
    }

    public void SetCurrentWeapon(GameObject weapon)
    {
        currentWeapon = weapon;
    }

    private void StartAiming()
    {
        cinemachine.GetComponent<CameraSwitcher>().StartAiming();
        Croshair.SetActive(true);
        isAiming = true;
        PlayBoolAnimServerRpc("IsAiming", true);
    }

    private void StopAiming()
    {
        cinemachine.GetComponent<CameraSwitcher>().StopAiming();
        Croshair.SetActive(false);
        isAiming = false;
        PlayBoolAnimServerRpc("IsAiming", false);
    }

    // --- Server/Client RPCs for animation syncing ---

    [ServerRpc]
    void PlayTriggerAnimServerRpc(string triggerName)
    {
        PlayTriggerAnimClientRpc(triggerName);
    }

    [ClientRpc]
    void PlayTriggerAnimClientRpc(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    [ServerRpc]
    void PlayBoolAnimServerRpc(string param, bool state)
    {
        PlayBoolAnimClientRpc(param, state);
    }

    [ClientRpc]
    void PlayBoolAnimClientRpc(string param, bool state)
    {
        animator.SetBool(param, state);
    }

    [ServerRpc]
    void UpdateAnimStatesServerRpc(float speed, bool isGrounded)
    {
        UpdateAnimStatesClientRpc(speed, isGrounded);
    }

    [ClientRpc]
    void UpdateAnimStatesClientRpc(float speed, bool isGrounded)
    {
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsMoving", speed > 0);
        animator.SetBool("IsGrounded", isGrounded);
    }
}
