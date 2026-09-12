using UnityEngine;
using UnityEngine.InputSystem; // Connects to the new Xbox hardware files

public class DynamicHeadLook : MonoBehaviour
{
    [Header("Neck Rotation Settings")]
    public float lookSpeed = 150f;     // How fast your head turns left/right
    public float maxLookAngle = 65f;   // Your strict 65-degree safety clamp
    public float snapBackSpeed = 8f;   // How fast eyes center after letting go of stick

    private float currentYaw = 0f;     // Tracks your current looking angle
    private XboxControls controls;     // Links to your Xbox mapping asset
    private Vector2 lookInput;

    void Awake()
    {
        // Handshake with your newly saved Xbox controls file layout
        controls = new XboxControls();
    }

    void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    void Update()
    {
        // 1. READ XBOX CONTROLLER AND MOUSE SIMULTANEOUSLY
        // Grabs the physical Right Joystick vector data we just assigned
        if (controls != null)
        {
            lookInput = controls.Gameplay.Look.ReadValue<Vector2>();
        }

        float horizontalInput = lookInput.x + Input.GetAxis("Mouse X");

        // 2. CHECK STICK ENGAGEMENT AND ENFORCE 65-DEGREE LIMIT
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            // Accumulate head panning movement over time
            currentYaw += horizontalInput * lookSpeed * Time.deltaTime;

            // Lock the head from spinning past standard human limits
            currentYaw = Mathf.Clamp(currentYaw, -maxLookAngle, maxLookAngle);
        }
        else
        {
            // SNAP-BACK: Smoothly return head directly forward when stick is idle
            currentYaw = Mathf.Lerp(currentYaw, 0f, Time.deltaTime * snapBackSpeed);
        }

        // 3. APPLY ROTATION: Pivot the lens locally along the Y-axis
        transform.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
    }
}