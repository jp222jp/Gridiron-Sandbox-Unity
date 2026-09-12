using UnityEngine;

public class PlayerAnatomyRig : MonoBehaviour
{
    // Cached runtime joint reference node tracking tracks
    public Transform leftHandProxy { get; private set; }
    public Transform rightHandProxy { get; private set; }
    public Transform leftShoulderProxy { get; private set; }
    public Transform rightShoulderProxy { get; private set; }
    public Transform leftHipProxy { get; private set; }
    public Transform rightHipProxy { get; private set; }
    public Transform leftFootProxy { get; private set; }

    public void InitializeAndScaleRig(PlayerAttributes stats)
    {
        // Trace and link the nested empty object folders
        leftShoulderProxy = transform.Find("Proxy_Shoulder_Left");
        rightShoulderProxy = transform.Find("Proxy_Shoulder_Right");
        if (leftShoulderProxy != null) leftHandProxy = leftShoulderProxy.Find("Proxy_Elbow_Left")?.Find("Proxy_Hand_Left");
        if (rightShoulderProxy != null) rightHandProxy = rightShoulderProxy.Find("Proxy_Elbow_Right")?.Find("Proxy_Hand_Right");

        leftHipProxy = transform.Find("Proxy_Hip_Left");
        rightHipProxy = transform.Find("Proxy_Hip_Right");
        if (leftHipProxy != null) leftFootProxy = leftHipProxy.Find("Proxy_Knee_Left")?.Find("Proxy_Foot_Left");

        // Mathematical conversion translation: inches to metric grid constants
        float inchesToMeters = 0.0254f;
        float halfShoulderMeters = (stats.shoulderWidthInches * 0.5f) * inchesToMeters;
        float halfHipMeters = (stats.hipWidthInches * 0.5f) * inchesToMeters;
        float armLengthMeters = stats.armLengthInches * inchesToMeters;

        // Force positions to lock precisely to the player attributes measurements
        if (leftShoulderProxy != null) leftShoulderProxy.localPosition = new Vector3(-halfShoulderMeters, 0.5f, 0f);
        if (rightShoulderProxy != null) rightShoulderProxy.localPosition = new Vector3(halfShoulderMeters, 0.5f, 0f);
        if (leftHandProxy != null) leftHandProxy.localPosition = new Vector3(0f, 0f, armLengthMeters);
        if (rightHandProxy != null) rightHandProxy.localPosition = new Vector3(0f, 0f, armLengthMeters);

        // Dynamic Joint Flexibility ROM Adjustment: Stance height depth scales based on flexibility
        float stanceHeightOffset = -0.2f * (2.0f - stats.jointFlexibilityROM);
        if (leftHipProxy != null) leftHipProxy.localPosition = new Vector3(-halfHipMeters, stanceHeightOffset, 0f);
        if (rightHipProxy != null) rightHipProxy.localPosition = new Vector3(halfHipMeters, stanceHeightOffset, 0f);
    }
}