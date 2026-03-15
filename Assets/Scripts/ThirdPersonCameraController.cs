using UnityEngine;

/// <summary>
/// Third-person camera orbit controller with zoom clamping and collision prevention.
/// Attach this to the active Camera and assign a player target.
/// </summary>
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.7f, 0f);

    [Header("Orbit")]
    [SerializeField] private float mouseXSensitivity = 150f;
    [SerializeField] private float mouseYSensitivity = 120f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Zoom")]
    [SerializeField] private float defaultDistance = 4.5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float zoomSmoothTime = 0.08f;

    [Header("Collision")]
    [SerializeField] private float collisionProbeRadius = 0.25f;
    [SerializeField] private float collisionBuffer = 0.1f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.04f;

    [Header("Debug")]
    [SerializeField] private bool debugEnabledByDefault = false;

    private float yaw;
    private float pitch;
    private float targetDistance;
    private float currentDistance;
    private float zoomVelocity;
    private Vector3 followVelocity;

    private bool debugEnabled;
    private bool debugOverlay;
    private bool debugDraw;

    private Vector3 anchorPosition;
    private bool cameraBlocked;

    private void Awake()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);

        targetDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        currentDistance = targetDistance;

        debugEnabled = debugEnabledByDefault;
        debugOverlay = debugEnabledByDefault;
        debugDraw = debugEnabledByDefault;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleDebugToggles();
        ReadLookInput();
        ReadZoomInput();

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredAnchor = target.position + targetOffset;
        anchorPosition = Vector3.SmoothDamp(anchorPosition == Vector3.zero ? desiredAnchor : anchorPosition, desiredAnchor, ref followVelocity, followSmoothTime);

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        Vector3 desiredCameraOffset = rotation * new Vector3(0f, 0f, -currentDistance);
        Vector3 desiredPosition = anchorPosition + desiredCameraOffset;
        Vector3 safePosition = ResolveCollision(anchorPosition, desiredPosition);

        transform.position = safePosition;
        transform.rotation = rotation;
    }

    private void ReadLookInput()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) < 0.0001f && Mathf.Abs(mouseY) < 0.0001f)
        {
            return;
        }

        yaw += mouseX * mouseXSensitivity * Time.deltaTime;
        pitch -= mouseY * mouseYSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void ReadZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f)
        {
            return;
        }

        targetDistance -= scroll * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    private Vector3 ResolveCollision(Vector3 from, Vector3 desired)
    {
        Vector3 toCamera = desired - from;
        float distance = toCamera.magnitude;

        if (distance <= 0.0001f)
        {
            cameraBlocked = false;
            return desired;
        }

        Vector3 dir = toCamera / distance;
        if (Physics.SphereCast(from, collisionProbeRadius, dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Max(minDistance, hit.distance - collisionBuffer);
            cameraBlocked = true;
            return from + dir * safeDist;
        }

        cameraBlocked = false;
        return desired;
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
            Debug.Log($"[Camera Debug] Global debug {(debugEnabled ? "enabled" : "disabled")}");
            return;
        }

        if (!debugEnabled)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Alpha3))
        {
            debugOverlay = !debugOverlay;
            Debug.Log($"[Camera Debug] Overlay {(debugOverlay ? "enabled" : "disabled")}");
        }
        else if (Input.GetKey(KeyCode.Alpha4))
        {
            debugDraw = !debugDraw;
            Debug.Log($"[Camera Debug] Draw gizmos {(debugDraw ? "enabled" : "disabled")}");
        }
    }

    private void OnGUI()
    {
        if (!debugEnabled || !debugOverlay)
        {
            return;
        }

        string text =
            "Camera Debug\n" +
            $"Distance: {currentDistance:F2} (target {targetDistance:F2})\n" +
            $"Yaw/Pitch: {yaw:F1} / {pitch:F1}\n" +
            $"Blocked: {cameraBlocked}\n" +
            "F3+Shift: toggle debug, F3+3 overlay, F3+4 gizmos";

        GUI.Box(new Rect(12f, 152f, 360f, 100f), text);
    }

    private void OnDrawGizmos()
    {
        if (!debugEnabled || !debugDraw || target == null)
        {
            return;
        }

        Vector3 from = target.position + targetOffset;
        Gizmos.color = cameraBlocked ? Color.red : Color.green;
        Gizmos.DrawLine(from, transform.position);
        Gizmos.DrawWireSphere(transform.position, collisionProbeRadius);
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}

