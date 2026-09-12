using UnityEngine;

public class PlayerBiomechanicalFailure : MonoBehaviour
{
    [Header("Dynamic Tendencies & Tinkering")]
    [Range(0.1f, 1f)]
    [Tooltip("1.0 = Jagged hard angle cuts. 0.2 = Rounded circle route paths.")]
    public float routeSharpnessBias = 0.85f;
    public float bodyStrainAccumulator = 0f;

    [Header("Custom Risk Tuning Framework")]
    [Tooltip("How much fatigue strain cushioning the athlete has before injury risks open up. Higher = more durable.")]
    public float injuryVulnerabilityCushion = 45f;

    [Range(0f, 5f)]
    [Tooltip("Scales the frequency of soft-tissue failures. Set to 0 to completely disable injuries during testing!")]
    public float injuryProbabilityScale = 1.0f;

    public bool isInjured { get; private set; } = false;

    private Rigidbody rb;
    private PlayerAttributes stats;

    public void SetUpDependencies(Rigidbody playerRb, PlayerAttributes playerStats)
    {
        rb = playerRb;
        stats = playerStats;
    }

    public void ProcessKineticStrain(float turnInputAxis, float fixedDeltaTime)
    {
        if (isInjured || stats == null) return;

        // Force Load: Mass × Turn Velocity × Sharpness Choices
        float frameStrain = (rb.mass * 0.01f) * routeSharpnessBias * Mathf.Abs(turnInputAxis);
        frameStrain *= (1.0f + stats.quickTwitchExplosiveness * 0.3f);
        bodyStrainAccumulator += frameStrain;

        EvaluateConditionalFailure(frameStrain);
    }

    public void CoolDownStrain(float fixedDeltaTime)
    {
        if (isInjured) return;
        bodyStrainAccumulator = Mathf.MoveTowards(bodyStrainAccumulator, 0f, fixedDeltaTime * 6f);
    }

    private void EvaluateConditionalFailure(float currentFrameStrain)
    {
        if (stats == null || isInjured) return;

        // 1. CONDITIONAL FOOTING SLIP MATRIX
        float controlThreshold = (stats.coreStrength * 0.5f + stats.lowerBodyStrength * 0.2f + stats.cleatFrictionCoefficient * 0.3f);
        float conditionalSlipChance = (currentFrameStrain / controlThreshold) * 0.04f;

        if (Random.value < conditionalSlipChance)
        {
            Debug.LogWarning($"[PHYSICAL FAILURE] Cleats lost friction! SLIP EVENT triggered.");
            rb.AddForce(transform.right * Random.Range(-5f, 5f), ForceMode.VelocityChange);
        }

        // 2. CONDITIONAL INJURY STRAIN MATRIX (With Risk Tuning Multiplier)
        float fatigueFloorThreshold = injuryVulnerabilityCushion * (1.0f + stats.mentalToughnessPoise * 0.2f);

        if (bodyStrainAccumulator > fatigueFloorThreshold && injuryProbabilityScale > 0f)
        {
            // The risk is conditioned on your fatigue and scaled directly by your slider frequency
            float conditionalInjuryChance = (bodyStrainAccumulator - fatigueFloorThreshold) * 0.0015f * injuryProbabilityScale;

            if (Random.value < conditionalInjuryChance)
            {
                TriggerStructuralInjury();
            }
        }
    }

    private void TriggerStructuralInjury()
    {
        isInjured = true;
        rb.constraints = RigidbodyConstraints.None; // Collapse capsule cylinder upright constraints
        Debug.LogError($"[BIOMECHANICAL FAILURE] Soft-tissue muscle fiber failure! Non-contact strain executed. Press Xbox Back Button to heal.");
    }

    public void ForceInstantHealAndReset()
    {
        if (!isInjured && bodyStrainAccumulator == 0f) return;

        isInjured = false;
        bodyStrainAccumulator = 0f;

        // Restore pristine rigid column vertical constraint boundaries
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); // Snap straight up

        Debug.Log("[TRAINING ROOM RESCUE] Medical training team cleared player! Kinetic strain and injury status completely wiped clean.");
    }

    public void InjectJumpStrain()
    {
        if (isInjured) return;
        bodyStrainAccumulator += 5f;
    }
}