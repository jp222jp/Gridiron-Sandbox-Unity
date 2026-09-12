using UnityEngine;

public class SidelineCameraTracker : MonoBehaviour
{
    [Header("Tracking Targets")]
    public Transform playerTarget;   // Link your player capsule here
    public Transform footballTarget; // Link your football sphere here

    [Header("Dynamic Zoom Engine")]
    public float minFieldOfView = 30f;    // Max Zoom-In (When player is right next to the ball)
    public float maxFieldOfView = 65f;    // Max Zoom-Out (When player is far downfield)
    public float distanceMaxThreshold = 40f; // Distance (meters) at which maximum zoom-out is reached
    public float zoomSmoothSpeed = 5f;   // How smoothly the camera lens expands/retracts

    private Camera sidelineCamera;

    void Start()
    {
        // Automatically fetch the Camera properties attached to this object
        sidelineCamera = GetComponent<Camera>();
    }

    // LateUpdate is standard for camera systems; it runs AFTER player movement physics 
    // finishes updating, completely eliminating high-speed frame stuttering or jittering.
    void LateUpdate()
    {
        // Safety Gate: Don't execute calculations if the targets are missing
        if (playerTarget == null || footballTarget == null || sidelineCamera == null) return;

        // 1. ROTATIONAL TRACKING: Force the camera to pivot its neck and stare at the player
        transform.LookAt(playerTarget);

        // 2. MATHEMATICAL DISTANCE: Calculate the straight line vector length between player and ball
        float currentDistance = Vector3.Distance(playerTarget.position, footballTarget.position);

        // 3. ZOOM RATIO CALCULATION: Translate distance into a clean 0.0 to 1.0 percentage scale
        float zoomPercentage = Mathf.Clamp01(currentDistance / distanceMaxThreshold);

        // 4. LENS INTERPOLATION: Find the target Field of View angle based on that percentage
        float targetFOV = Mathf.Lerp(minFieldOfView, maxFieldOfView, zoomPercentage);

        // 5. SMOOTH APPLY: Blend the current camera view into the target view over time
        sidelineCamera.fieldOfView = Mathf.Lerp(sidelineCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSmoothSpeed);
    }
}