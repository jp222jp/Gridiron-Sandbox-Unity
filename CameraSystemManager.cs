using UnityEngine;

public class CameraSystemManager : MonoBehaviour
{
    [Header("Camera Rig Links")]
    public Camera sidelineCamera;
    public Camera playerPOVCamera;

    void Start()
    {
        // Start the game looking through the sideline broadcast view
        sidelineCamera.enabled = true;
        playerPOVCamera.enabled = false;
    }

    void Update()
    {
        // Pressing the 'C' key on your laptop keyboard instantly swaps perspectives!
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Swap the active state of both cameras simultaneously
            sidelineCamera.enabled = !sidelineCamera.enabled;
            playerPOVCamera.enabled = !playerPOVCamera.enabled;

            Debug.Log("[CAMERA SWAP] Toggling perspective view.");
        }
    }
}