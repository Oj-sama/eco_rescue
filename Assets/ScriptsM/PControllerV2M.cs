using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PControllerV2 : NetworkBehaviour
{
    [Header("Network Variables")]
    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes("Player"));

    public NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Components")]
    [SerializeField] private Renderer playerBodyRenderer;
    [SerializeField] private GameObject playerCameraObject;

    private Material playerMaterialClone;
    private const float MoveSpeed = 20f;
    private const float RotationSpeed = 100f;

    private GameObject nearestCollectable;
    public float pickupRange = 2f;
    private bool isdoingsomth = false;
    public BeltInventory inventory;
    public Inventory inv;

    private Vector3 spawnPosition;
    private bool hasMovedFromSpawn = false;
    private const float MovementThreshold = 2.0f;

    #region Network Callbacks
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializePlayerMaterials();
        RegisterValueChangedCallbacks();

        spawnPosition = transform.position;

        if (playerCameraObject != null)
        {
            playerCameraObject.SetActive(IsOwner);
        }
        else
        {
            Debug.LogWarning("Player camera GameObject reference not set!", this);
        }

        if (IsOwner)
        {
            SetRandomColorServerRpc(GenerateRandomColor());
        }
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

                inventory.AddItemToSlot(collectable);
                collectable.Pickup();

                isdoingsomth = false;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        UnregisterValueChangedCallbacks();

        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ReleaseName(playerName.Value.ToString());
        }
    }
    #endregion

    #region Initialization
    private void InitializePlayerMaterials()
    {
        if (playerBodyRenderer == null)
        {
            playerBodyRenderer = GetComponentInChildren<Renderer>();
            if (playerBodyRenderer == null)
            {
                Debug.LogError("Player body renderer not found!", this);
                return;
            }
        }

        playerMaterialClone = new Material(playerBodyRenderer.material);
        playerBodyRenderer.material = playerMaterialClone;
        playerMaterialClone.color = playerColor.Value;
    }

    private void RegisterValueChangedCallbacks()
    {
        playerName.OnValueChanged += OnPlayerNameChanged;
        playerColor.OnValueChanged += OnPlayerColorChanged;
    }

    private void UnregisterValueChangedCallbacks()
    {
        playerName.OnValueChanged -= OnPlayerNameChanged;
        playerColor.OnValueChanged -= OnPlayerColorChanged;
    }
    #endregion

    #region Network Methods
    [ServerRpc]
    private void SetRandomColorServerRpc(Color newColor)
    {
        if (!IsServer) return;
        playerColor.Value = newColor;
    }

    private Color GenerateRandomColor()
    {
        return new Color(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f)
        );
    }
    #endregion

    #region Value Change Handlers
    private void OnPlayerNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        Debug.Log($"[Network] Player name changed from {oldName} to {newName}");
    }

    private void OnPlayerColorChanged(Color oldColor, Color newColor)
    {
        if (playerMaterialClone != null)
        {
            playerMaterialClone.color = newColor;
        }
    }
    #endregion

    #region Gameplay
    private void Update()
    {
        if (!IsOwner) return;
        HandleMovementInput();
        CheckIfMovedFromSpawn();
        FindNearestCollectable();
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal") * Time.deltaTime * RotationSpeed;
        float vertical = Input.GetAxis("Vertical") * Time.deltaTime * MoveSpeed;

        transform.Translate(0, 0, vertical);
        transform.Rotate(0, horizontal, 0);
    }

    private void CheckIfMovedFromSpawn()
    {
        if (hasMovedFromSpawn) return;

        if (Vector3.Distance(transform.position, spawnPosition) > MovementThreshold)
        {
            hasMovedFromSpawn = true;
        }
    }
    #endregion

    #region Public Methods
    public string GetPlayerName() => playerName.Value.ToString();
    #endregion
}