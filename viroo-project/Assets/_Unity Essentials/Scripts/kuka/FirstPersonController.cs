using UnityEngine;

public class ThirdPersonLikeController : MonoBehaviour
{
    public float moveSpeed = 5.0f;       // Horizontal movement speed
    public float lookSensitivity = 2.0f; // Mouse sensitivity
    public float verticalMoveSpeed = 3.0f; // Vertical movement speed (for Q and E)

    private float verticalRotation = 0f;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked; // Lock the mouse cursor
    }

    void Update()
    {
        // --- Mouse Rotation ---
        float horizontalRotation = Input.GetAxis("Mouse X") * lookSensitivity;
        transform.Rotate(0f, horizontalRotation, 0f);

        verticalRotation -= Input.GetAxis("Mouse Y") * lookSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f); // Apply vertical rotation to the camera
        }

        // --- Movement Based on Camera's Direction ---
        float verticalInput = Input.GetAxis("Vertical");     // W/S
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D

        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0f; // Flatten the forward direction to avoid moving vertically
        cameraForward.Normalize();

        Vector3 cameraRight = playerCamera.transform.right;
        cameraRight.y = 0f; // Flatten the right direction to avoid moving vertically
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * verticalInput + cameraRight * horizontalInput;

        // --- Vertical Movement with Q and E ---
        if (Input.GetKey(KeyCode.E))
        {
            moveDirection += Vector3.up; // Move up when E is pressed
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            moveDirection += Vector3.down; // Move down when Q is pressed
        }

        moveDirection.Normalize();

        // Move the character using the CharacterController component
        GetComponent<CharacterController>().Move(moveDirection * moveSpeed * Time.deltaTime);
    }
}
