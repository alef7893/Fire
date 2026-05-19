using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerCameraController : MonoBehaviour
{
    public float moveSpeed = 4.0f;
    public float sprintMultiplier = 1.5f;
    public float mouseSensitivity = 2.0f;
    public float minPitch = -75.0f;
    public float maxPitch = 75.0f;
    public float gravity = -20.0f;
    public bool lockCursorOnStart = true;

    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        pitch = NormalizePitch(transform.eulerAngles.x);

        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        UpdateCursorLock();
        UpdateLook();
        UpdateMovement();
    }

    private void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    private void UpdateLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
        transform.Rotate(Vector3.up * mouseX, Space.World);
        transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0.0f);
    }

    private void UpdateMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 movement = right * horizontal + forward * vertical;

        if (movement.sqrMagnitude > 1.0f)
        {
            movement.Normalize();
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;
        movement *= speed;

        if (characterController.isGrounded && verticalVelocity < 0.0f)
        {
            verticalVelocity = -2.0f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        characterController.Move(movement * Time.deltaTime);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private float NormalizePitch(float angle)
    {
        return angle > 180.0f ? angle - 360.0f : angle;
    }
}
