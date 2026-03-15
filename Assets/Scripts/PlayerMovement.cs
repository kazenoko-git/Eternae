using UnityEngine;

/// <summary>
/// Rigidbody-based third-person movement controller with optional debug instrumentation.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 28f;
    [SerializeField] private float airControlMultiplier = 0.45f;
    [SerializeField] private float turnSpeed = 15f;

    [Header("Jump and Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravityMultiplier = 2.3f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.26f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundedStickForce = 5f;

    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;

    [Header("Debug Controls")]
    [SerializeField] private bool debugEnabledByDefault = false;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpQueued;
    private bool sprintHeld;

    private bool debugEnabled;
    private bool debugOverlay;
    private bool debugDraw;

    private readonly float gravity = -9.81f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        debugEnabled = debugEnabledByDefault;
        debugOverlay = debugEnabledByDefault;
        debugDraw = debugEnabledByDefault;
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ReadInput();
        CheckGroundStatus();
        HandleDebugToggles();
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyJumpAndGravity();
        RotateCharacter();
    }

    private void ReadInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump"))
        {
            jumpQueued = true;
        }
    }

    private void CheckGroundStatus()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyHorizontalMovement()
    {
        Vector3 desiredDirection = GetCameraRelativeDirection(moveInput);
        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = desiredDirection * targetSpeed;

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 velocityDelta = targetVelocity - currentHorizontal;

        float accel = desiredDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
        if (!isGrounded)
        {
            accel *= airControlMultiplier;
        }

        Vector3 accelerationStep = Vector3.ClampMagnitude(velocityDelta, accel * Time.fixedDeltaTime);
        rb.AddForce(accelerationStep, ForceMode.VelocityChange);

        if (isGrounded && desiredDirection.sqrMagnitude <= 0.001f)
        {
            rb.AddForce(Vector3.down * groundedStickForce, ForceMode.Acceleration);
        }
    }

    private void ApplyJumpAndGravity()
    {
        if (jumpQueued)
        {
            if (isGrounded)
            {
                float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Vector3 v = rb.linearVelocity;
                v.y = jumpVelocity;
                rb.linearVelocity = v;
                isGrounded = false;
                hasAirJump = true;
            }
            else if (hasAirJump)
            {
                float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Vector3 v = rb.linearVelocity;
                v.y = jumpVelocity;
                rb.linearVelocity = v;
                hasAirJump = false;
            }
        }

        jumpQueued = false;

        float gravityScale = isGrounded && rb.linearVelocity.y <= 0f ? 1f : gravityMultiplier;
        rb.AddForce(Vector3.up * gravity * gravityScale, ForceMode.Acceleration);
    }

    private void RotateCharacter()
    {
        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        Quaternion smoothed = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothed);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (right * input.x + forward * input.y).normalized;
    }

    private void HandleDebugToggles()
    {
        if (!Input.GetKeyDown(KeyCode.F3))
        {
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            debugEnabled = !debugEnabled;
            debugOverlay = debugEnabled;
            debugDraw = debugEnabled;
            Debug.Log($"[Movement Debug] Global debug {(debugEnabled ? "enabled" : "disabled")}");
            return;
        }

        if (!debugEnabled)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Alpha1))
        {
            debugOverlay = !debugOverlay;
            Debug.Log($"[Movement Debug] Overlay {(debugOverlay ? "enabled" : "disabled")}");
        }
        else if (Input.GetKey(KeyCode.Alpha2))
        {
            debugDraw = !debugDraw;
            Debug.Log($"[Movement Debug] Draw gizmos {(debugDraw ? "enabled" : "disabled")}");
        }
    }

    private void OnGUI()
    {
        if (!debugEnabled || !debugOverlay)
        {
            return;
        }

        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        string text =
            "Movement Debug\n" +
            $"Grounded: {isGrounded}\n" +
            $"Air Jump: {hasAirJump}\n" +
            $"Speed: {horizontal.magnitude:F2}\n" +
            $"Velocity: {rb.linearVelocity:F2}\n" +
            $"Input: {moveInput:F2}\n" +
            "F3+Shift: toggle debug, F3+1 overlay, F3+2 gizmos";

        GUI.Box(new Rect(12f, 12f, 360f, 152f), text);
    }

    private void OnDrawGizmos()
    {
        if (!debugEnabled || !debugDraw)
        {
            return;
        }

        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (rb != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position + Vector3.up * 1.2f;
            Gizmos.DrawLine(start, start + rb.linearVelocity);
        }
    }

    public bool IsGrounded => isGrounded;
    public float CurrentSpeed => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
}
