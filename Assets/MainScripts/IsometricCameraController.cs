using UnityEngine;

/// <summary>
/// Controls an isometric-style camera with smooth movement, rotation, and zoom.
/// Designed for perspective projection for a tactical RPGs.
/// Attach this script to the camera pivot GameObject.
/// </summary>
public class IsometricCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform mCamera;      // Reference to the actual camera object
    public Transform target;       // The camera's focal point (point that is on the surface of the terrain - for smooth rotation and hierarchy)

    [Header("Movement")]
    public float moveSpeed = 10f;  // Speed at which the pivot (target) moves

    [Header("Rotation")]
    public float rotateSpeed = 90f; // Speed of rotation (degrees per second)

    [Header("Zoom")]
    public float zoomSpeed = 5f;    // Speed of zooming (scroll sensitivity)
    public float minZoom = 5f;      // Closest distance to target
    public float maxZoom = 20f;     // Furthest distance from target
    private float currentZoom = 5f; // Runtime zoom distance from target

    void Start()
    {
        // Initialize zoom distance based on starting position
        currentZoom = Vector3.Distance(mCamera.position, target.position);
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        // Ensure camera starts looking at target
        mCamera.LookAt(target.position);
    }

    void Update()
    {
        HandleZoom();
        HandleMovement();
        HandleRotation();

        // Always look at target; for arc ball effect around the pivot point
        mCamera.LookAt(target.position);
    }

    /// <summary>
    /// Handles movement of the camera pivot using WASD input,
    /// relative to the camera's current orientation (XZ plane only).
    /// </summary>
    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows

        // Get camera's orientation on the horizontal plane
        Vector3 camForward = mCamera.forward;
        Vector3 camRight = mCamera.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement direction in world space
        Vector3 moveDirection = (camForward * vertical + camRight * horizontal).normalized;

        // Move the pivot - and not the camera itself
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Handles rotation of the target pivot using Q/E keys.
    /// </summary>
    void HandleRotation()
    {
        float rotationInput = 0f;

        if (Input.GetKey(KeyCode.Q)) rotationInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotationInput = 1f;

        if (rotationInput != 0f)
        {
            // Rotate the target GameObject around its Y axis
            target.Rotate(0f, rotationInput * rotateSpeed * Time.deltaTime, 0f);
        }
    }

    /// <summary>
    /// Handles zooming the camera in and out using the mouse scroll wheel,
    /// by adjusting the distance between the camera and the target.
    /// </summary>
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Get direction from target to camera
            Vector3 zoomDir = (mCamera.position - target.position).normalized;

            // Modify zoom level
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            // Update camera position
            mCamera.position = target.position + zoomDir * currentZoom;
        }
    }
}
