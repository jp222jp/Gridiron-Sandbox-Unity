using UnityEngine;

public class GridironCameraController : MonoBehaviour
{
    [Header("🎥 Camera Target Assignment")]
    public Camera sidelineCamera;
    public Camera playerPOVCamera;
    public Transform customFootballTarget;

    [Header("🔍 Gameplay Zoom Settings (Synchronized)")]
    public float defaultFOV = 35f;

    [Header("🤖 Pre-Snap Automation")]
    public bool autoReSpotToBallYard = true;
    public float sidelineDistanceWidth = 35f;
    public float scaffoldingElevationHeight = 12f;

    [Header("🔄 Fixed Rotation Overrides")]
    public float cameraPitchTilt = 18.5f;
    public float cameraYawPan = 90f;

    private float currentSidelineFOV;

    void Start()
    {
        currentSidelineFOV = defaultFOV;
        if (sidelineCamera != null) sidelineCamera.fieldOfView = currentSidelineFOV;
        if (playerPOVCamera != null) playerPOVCamera.fieldOfView = 75f;
    }

    void FixedUpdate()
    {
        // Smoothly position and focus look angles exclusively using target physics ticks
        if (autoReSpotToBallYard && customFootballTarget != null && sidelineCamera != null)
        {
            float targetBallYardZ = customFootballTarget.position.z;
            sidelineCamera.transform.position = new Vector3(-sidelineDistanceWidth, scaffoldingElevationHeight, targetBallYardZ);
            sidelineCamera.transform.rotation = Quaternion.Euler(cameraPitchTilt, cameraYawPan, 0f);
        }
    }

    void Update()
    {
        if (sidelineCamera == null) return;

        // Smoothly glide the lens field-of-view directly to whatever number is set inside your selection window slider
        currentSidelineFOV = Mathf.Lerp(sidelineCamera.fieldOfView, defaultFOV, Time.deltaTime * 8f);
        sidelineCamera.fieldOfView = currentSidelineFOV;
    }
}