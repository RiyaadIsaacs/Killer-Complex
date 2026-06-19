using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public const string MouseSensitivityPrefKey = "MouseSensitivityMultiplier";
    public const float DefaultMouseSensitivity = 40f;
    public const float MinSensitivityMultiplier = 0.1f;
    public const float MaxSensitivityMultiplier = 2f;
    public const float DefaultSensitivityMultiplier = 1f;
    // Movement
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;


    [SerializeField]private bool isSprinting;

    public bool IsSprinting => isSprinting;

    // Camera
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = DefaultMouseSensitivity;
    [SerializeField] private float verticalLookLimit = 80f;

    // Interaction
    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [Tooltip("If set, interact ray uses this transform's position and forward (e.g. empty child at eye height). Otherwise uses the camera transform.")]
    [SerializeField] private Transform interactRayOriginOverride;
    [Tooltip("Scene view only: line drawn after each interact attempt.")]
    [SerializeField] private bool drawInteractRayDebug = true;
    [SerializeField] private float interactRayDebugDuration = 2f;

    // Ref to player used for collision and movement
    private CharacterController controller;

    // Input storage
    private Vector2 moveInput;
    private Vector2 lookInput;

    private float verticalVelocity;

    private float _xRotation;

    #region Input Callbacks
    // Stores directional movement input (WASD)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Stores mouse input
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    // toggles sprint state on press (press again to turn off)
    public void OnSprint(InputValue value)
    {
        // toggle sprint on press 
        if (value.isPressed)
        {
            isSprinting = !isSprinting; // Toggle sprint state on each press (if its on its now off and if it was off its now on)
        }
    }

    // Attempts to start a jump if grounded
    public void OnJump()
    {
        if (controller == null)
            return;

        // Only allow jumping when grounded
        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // PlayerInput SendMessages always passes InputValue (never CallbackContext). For Button actions it only sends on performed.
    public void OnInteract(InputValue value)
    {
        TryInteract();
    }
    #endregion


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController requires a CharacterController component on the same GameObject.");
        }

        // If no camera is assigned, automatically use the main camera when available
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (GetComponent<InteractPromptHud>() == null)
            gameObject.AddComponent<InteractPromptHud>();

        if (GetComponent<PlayerMovementAudio>() == null)
            gameObject.AddComponent<PlayerMovementAudio>();

        ApplySavedMouseSensitivity();
    }

    public static float ClampSensitivityMultiplier(float multiplier) =>
        Mathf.Clamp(multiplier, MinSensitivityMultiplier, MaxSensitivityMultiplier);

    public static float GetSavedSensitivityMultiplier() =>
        ClampSensitivityMultiplier(PlayerPrefs.GetFloat(MouseSensitivityPrefKey, DefaultSensitivityMultiplier));

    public static void SaveSensitivityMultiplier(float multiplier) =>
        PlayerPrefs.SetFloat(MouseSensitivityPrefKey, ClampSensitivityMultiplier(multiplier));

    public void ApplySavedMouseSensitivity()
    {
        mouseSensitivity = DefaultMouseSensitivity * GetSavedSensitivityMultiplier();
    }

    private void Update()
    {
        // check for controller ref
        if (controller == null)
            return;

        // Handle looking and movement separately for cleaner and more modular code
        HandleLook();
        HandleMovement();
        UpdateInteractPrompt();
    }

    // Rotates the camera vertically and the player horizontally for First Person look
    private void HandleLook()
    {
        if (cameraTransform == null)
            return;

        Vector2 look = lookInput * mouseSensitivity * Time.deltaTime;

        _xRotation -= look.y;
        _xRotation = Mathf.Clamp(_xRotation, -verticalLookLimit, verticalLookLimit);

        // Apply vertical rotation to the camera and horizontal rotation to the player body
        cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * look.x);
    }

    // Moves the player relative to their facing direction, with sprint, gravity and jumping
    private void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // If the player stops moving, disable sprint
        const float stopThreshold = 0.0001f;
        if (move.sqrMagnitude <= stopThreshold)
        {
            isSprinting = false;
        }

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed; // Determine target speed based on if isSprinting is true

        Vector3 velocity = move * targetSpeed;

        // When grounded, keep the character snapped to the ground plane
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Integrate gravity over time and assign to the vertical component
        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    // Casts a short ray from the camera forward and invokes Interact() on hit objects
    private void TryInteract()
    {
        if (!TryGetInteractRay(out var hit))
            return;

        if (drawInteractRayDebug)
        {
            Transform srcDbg = GetInteractRayTransform(out _, out _);
            Debug.DrawLine(srcDbg.position, hit.point, Color.green, interactRayDebugDuration, false);
        }

        // Collider may be on a child mesh while InteractDoor sits on a parent pivot.
        hit.transform.SendMessageUpwards("Interact", SendMessageOptions.DontRequireReceiver);
    }

    bool TryGetInteractRay(out RaycastHit hit)
    {
        hit = default;
        var origin = GetInteractRayTransform(out var useViewportRay, out var camForViewport);

        Ray ray;
        if (useViewportRay && camForViewport != null)
            ray = camForViewport.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        else
            ray = new Ray(origin.position, origin.forward);

        var rayEnd = ray.origin + ray.direction * interactDistance;

        if (!Physics.Raycast(ray, out hit, interactDistance, interactableLayers))
        {
            if (drawInteractRayDebug)
                Debug.DrawLine(ray.origin, rayEnd, Color.red, interactRayDebugDuration, false);
            return false;
        }

        return true;
    }

    /// <summary>World-space origin for interaction; prefers viewport-center ray from gameplay <see cref="Camera"/> when available.</summary>
    Transform GetInteractRayTransform(out bool useViewportCenterRay, out Camera viewportCamera)
    {
        useViewportCenterRay = false;
        viewportCamera = null;

        if (interactRayOriginOverride != null)
            return interactRayOriginOverride;

        if (cameraTransform != null)
        {
            viewportCamera = cameraTransform.GetComponent<Camera>();
            if (viewportCamera != null)
            {
                useViewportCenterRay = true;
                return cameraTransform;
            }

            return cameraTransform;
        }

        viewportCamera = Camera.main;
        if (viewportCamera != null)
        {
            useViewportCenterRay = true;
            return viewportCamera.transform;
        }

        return transform;
    }

    void UpdateInteractPrompt()
    {
        var hud = InteractPromptHud.Instance;
        if (hud == null)
            return;

        if (!TryGetInteractRay(out var hit))
            return;

        if (!InteractPromptResolver.TryResolve(hit, out var text, out var worldPos))
            return;

        hud.Offer(text, worldPos, 10);
    }
}
