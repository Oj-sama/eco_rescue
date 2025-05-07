using UnityEngine;

public class cameracontroller : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform player; // Reference to the player's transform
    public float distance = 5f; // Distance from the player
    public float height = 2f; // Height of the camera
    public float rotationSpeed = 5f; // Speed of camera rotation
    public float zoomSpeed = 5f; // Speed of camera zoom
    public float minDistance = 2f; // Minimum zoom distance
    public float maxDistance = 10f; // Maximum zoom distance
    public bool lockCursor = true; // Lock cursor for better control

    private float yaw = 0f; // Horizontal rotation
    private float pitch = 0f; // Vertical rotation
    private Vector3 offset;

    void Start()
    {
        // Lock and hide cursor
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Calculate the initial offset
        offset = new Vector3(0, height, -distance);

        // Initialize rotation
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void LateUpdate()
    {
        // Handle camera zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Get mouse input for rotation
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -40f, 80f); // Prevent flipping

        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculate desired position
        offset = new Vector3(0, 0, -distance);
        Vector3 desiredPosition = player.position + rotation * offset;

        // Move camera smoothly
        transform.position = Vector3.Lerp(transform.position, desiredPosition + Vector3.up * height, Time.deltaTime * rotationSpeed);
        transform.LookAt(player.position + Vector3.up * height);
    }
}
