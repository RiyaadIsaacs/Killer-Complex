using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    #region Variable Declarations
    // Movement
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;


    [SerializeField]private bool _isSprinting;

    // Camera
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 120f;
    [SerializeField] private float verticalLookLimit = 80f;

    // Ref to player used for collision and movement
    private CharacterController _controller;

    // Input storage
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    private float _verticalVelocity;

    private float _xRotation;


    #endregion

    #region Input Callbacks
    // Stores directional movement input (WASD)
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    // Stores mouse input
    public void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }

    // toggles sprint state on press (press again to turn off)
    public void OnSprint(InputValue value)
    {
        // toggle sprint on press 
        if (value.isPressed)
        {
            _isSprinting = !_isSprinting; // Toggle sprint state on each press (if its on its now off and if it was off its now on)
        }
    }

    // Attempts to start a jump if grounded
    public void OnJump()
    {
        if (_controller == null)
            return;

        // Only allow jumping when grounded
        if (_controller.isGrounded && _verticalVelocity <= 0f)
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    #endregion


    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            Debug.LogError("PlayerController requires a CharacterController component on the same GameObject.");
        }

        // If no camera is assigned, automatically use the main camera when available
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }


    private void Update()
    {
        // check for controller ref
        if (_controller == null)
            return;

        // Handle looking and movement separately for cleaner and more modular code
        HandleLook();
        HandleMovement();
    }

    // Rotates the camera vertically and the player horizontally for First Person look
    private void HandleLook()
    {
        if (cameraTransform == null)
            return;

        Vector2 look = _lookInput * mouseSensitivity * Time.deltaTime;

        _xRotation -= look.y;
        _xRotation = Mathf.Clamp(_xRotation, -verticalLookLimit, verticalLookLimit);

        // Apply vertical rotation to the camera and horizontal rotation to the player body
        cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * look.x);
    }

    // Moves the player relative to their facing direction, with sprint, gravity and jumping
    private void HandleMovement()
    {
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        // If the player stops moving, disable sprint
        const float stopThreshold = 0.0001f;
        if (move.sqrMagnitude <= stopThreshold)
        {
            _isSprinting = false;
        }

        float targetSpeed = _isSprinting ? sprintSpeed : walkSpeed; // Determine target speed based on if _isSprinting is true

        Vector3 velocity = move * targetSpeed;

        // When grounded, keep the character snapped to the ground plane
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        // Integrate gravity over time and assign to the vertical component
        _verticalVelocity += gravity * Time.deltaTime;
        velocity.y = _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);
    }
}
