using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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
    public float maxZoom = 25f;     // Furthest distance from target
    public float currentZoom = 20f; // Runtime zoom distance from target

    [Header("Movement Bounds")]
    public Vector2 xBounds = new Vector2(-30f, 30f);
    public Vector2 zBounds = new Vector2(-30f, 30f);
    public bool setBounds = false;

    [Header("Mouse Drag Rotation")]
    private Vector3 lastMousePosition;

    [Header("Edge Scrolling")]
    public int edgeThickness = 10; // in pixels
    public bool moveEdge = false;

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
        HandleMouseDragRotation();
        HandleEdgeScrolling();

        // Always look at target; for arc ball effect around the pivot point
        mCamera.LookAt(target.position);
    }

    public void SnapToCharacter(Transform charTransform)
    {
        if (charTransform == null) return;

        // Move pivot to character's feet
        transform.position = charTransform.position;

        // Ensure the target pivot follows as well
        target.position = charTransform.position;

        // Recalculate camera position based on current zoom
        Vector3 zoomDir = (mCamera.position - target.position).normalized;
        mCamera.position = target.position + zoomDir * currentZoom;

        // Reapply look direction
        mCamera.LookAt(target.position);
    }

    public void SnapToCharacter(Vector3 charTransform)
    {
        if (charTransform == null) return;

        // Ensure the target pivot follows as well
        target.position = charTransform;

        // Recalculate camera position based on current zoom
        Vector3 zoomDir = (mCamera.position - target.position).normalized;
        mCamera.position = target.position + zoomDir * currentZoom;

        // Reapply look direction
        mCamera.LookAt(target.position);
    }


    private Coroutine moveCoroutine;        

    public void LerpToCharacter(Transform charTransform, float duration = 0.5f, Action onComplete = null)
    {
        if (charTransform == null) return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(LerpToTargetPosition(charTransform.position, duration, onComplete));
    }

    public void LerpToCharacter(Vector3 charTransform, float duration = 0.5f, Action onComplete = null)
    {
        if (charTransform == null) return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(LerpToTargetPosition(charTransform, duration, onComplete));
    }

    private IEnumerator LerpToTargetPosition(Vector3 targetPosition, float duration, Action onComplete)
    {
        Vector3 start = transform.position;
        Vector3 end = targetPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            target.position = transform.position;

            // Update camera position to maintain zoom
            Vector3 zoomDir = (mCamera.position - target.position).normalized;
            mCamera.position = target.position + zoomDir * currentZoom;

            mCamera.LookAt(target.position);

            elapsed += Time.deltaTime;
            yield return null;
        }

        onComplete?.Invoke();

        // Final alignment
        transform.position = end;
        target.position = end;
        mCamera.position = target.position + (mCamera.position - target.position).normalized * currentZoom;
        mCamera.LookAt(target.position);

        moveCoroutine = null;
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

        if(setBounds)
            // Clamp the camera's pivots position
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, xBounds.x, xBounds.y),
                transform.position.y,
                Mathf.Clamp(transform.position.z, zBounds.x, zBounds.y)
            );

        // sync target
        target.position = transform.position;
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

    private void HandleMouseDragRotation()
    {
        if (Input.GetMouseButtonDown(2)) // Middle mouse button
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float yaw = delta.x * rotateSpeed * Time.deltaTime;

            target.Rotate(0f, yaw, 0f);
            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleEdgeScrolling()
    {
        // disavble edge scrolling if rotating wth mouse
        if (Input.GetMouseButton(2) || !moveEdge) return;

        Vector3 moveDir = Vector3.zero;
        Vector3 camForward = mCamera.forward;
        Vector3 camRight = mCamera.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x <= edgeThickness)
            moveDir -= camRight;
        else if (mousePos.x >= Screen.width - edgeThickness)
            moveDir += camRight;

        if (mousePos.y <= edgeThickness)
            moveDir -= camForward;
        else if (mousePos.y >= Screen.height - edgeThickness)
            moveDir += camForward;

        if (moveDir != Vector3.zero)
        {
            transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;

            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, xBounds.x, xBounds.y),
                transform.position.y,
                Mathf.Clamp(transform.position.z, zBounds.x, zBounds.y)
            );

            target.position = transform.position;
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
