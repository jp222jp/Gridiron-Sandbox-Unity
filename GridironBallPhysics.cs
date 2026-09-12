using UnityEngine;

// 🎯 THE STRATEGIC KICKING/PUNTING SPIN DIALS
public enum FootballSpinType
{
    CoordinatedSpiral, // Normal tight spiral: Low chaos, heavy forward slide skip
    Backspin,          // End-over-end reverse: Damped forward energy, bounces straight up or backward
    AussieDropLeft,    // Sideways hook left: Forcefully cuts left on turf contact
    AussieDropRight,   // Sideways hook right: Forcefully cuts right on turf contact
    Knuckleball        // Zero spin: Maximal chaos bucket size, extreme erratic tracking
}

public class GridironBallPhysics : MonoBehaviour
{
    [Header("🏈 Erratic Bounce Parameters")]
    [Range(0f, 100f)] public float baseIrregularBounceProbability = 65f;
    public float maxBounceImpulseForce = 4.5f;

    [Header("🌀 Low-Speed Roll Wobble Settings")]
    public float rollWobbleIntensity = 1.8f;
    public float rollThresholdVelocity = 3.5f;

    [Header("🎛️ Active Play Strategic Spin Stance")]
    [Tooltip("The spin type selected pre-play for a kicking or punting scenario.")]
    public FootballSpinType activeSpinType = FootballSpinType.CoordinatedSpiral;

    [HideInInspector] public float activeBallSpinChaosModifier = 1.0f;

    private Rigidbody rb;
    private bool isTouchingGround = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 👇 FIXED: Absolute frame-zero safety shield!
        // If a player is carrying the ball (it's kinematic), instantly abort execution
        // and do not let any background torque or velocity overrides fire!
        if (rb == null || rb.isKinematic) return;

        // Ignore direct player mesh collision tracking
        if (collision.collider.name.Contains("Player") || collision.collider.name.Contains("Dummy")) return;

        float impactMagnitude = collision.relativeVelocity.magnitude;

        if (impactMagnitude > rollThresholdVelocity)
        {
            float dynamicChaosMultiplier = CalculateSpinChaosScale();
            float dynamicCalculatedProbability = baseIrregularBounceProbability * activeBallSpinChaosModifier * dynamicChaosMultiplier;
            dynamicCalculatedProbability = Mathf.Clamp(dynamicCalculatedProbability, 5f, 95f);

            if (Random.Range(0f, 100f) <= dynamicCalculatedProbability)
            {
                ExecuteErraticPointedTipBounce(impactMagnitude);
            }
            else
            {
                ExecuteStrategicDirectionalSpinBounce(impactMagnitude);
            }
        }
    }

    private float CalculateSpinChaosScale()
    {
        // Dynamic Chaos Scaling: Tight spirals are stabilized (0.3x chaos), 
        // while dead-air knuckleballs expand the chaos envelope dramatically (1.4x chaos)
        switch (activeSpinType)
        {
            case FootballSpinType.CoordinatedSpiral: return 0.35f;
            case FootballSpinType.Backspin:          return 0.70f;
            case FootballSpinType.AussieDropLeft:   return 0.80f;
            case FootballSpinType.AussieDropRight:  return 0.80f;
            case FootballSpinType.Knuckleball:       return 1.45f;
            default:                                 return 1.0f;
        }
    }

    private void ExecuteStrategicDirectionalSpinBounce(float impactForce)
    {
        // Smoothly extract the ball's current horizontal path traveling vectors
        Vector3 flatForwardVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 flatForwardDir = flatForwardVelocity.normalized;
        Vector3 flatRightDir = Vector3.Cross(Vector3.up, flatForwardDir).normalized;

        float redirectPower = Mathf.Min(impactForce * 0.35f, maxBounceImpulseForce * 0.8f);

        Vector3 strategicForceVector = Vector3.zero;

        // 📐 APPLICATION OF INTENTIONAL SPIN DIRECTION LAWS:
        switch (activeSpinType)
        {
            case FootballSpinType.CoordinatedSpiral:
                // Normal Spiral: Slingshots linearly forward along its current path with a smooth skip
                strategicForceVector = flatForwardDir * 1.2f + Vector3.up * 0.3f;
                Debug.Log("[SPIN SYSTEM] Spiral Slide! Preserving linear downfield speed.");
                break;

            case FootballSpinType.Backspin:
                // Backspin: Counter-acts forward energy completely, driving the vector BACKWARD or straight up
                strategicForceVector = (-flatForwardDir * 0.8f) + (Vector3.up * 1.1f);
                Debug.Log("[SPIN SYSTEM] Backspin Bite! Stopping forward drift, kicking backward.");
                break;

            case FootballSpinType.AussieDropLeft:
                // Aussie Left: Displaces forward velocity, shearing the ball sharply to the left boundary line
                strategicForceVector = (flatForwardDir * 0.4f) + (-flatRightDir * 1.0f) + (Vector3.up * 0.4f);
                Debug.Log("[SPIN SYSTEM] Aussie Drop Left! Shearing sharply toward left sideline.");
                break;

            case FootballSpinType.AussieDropRight:
                // Aussie Right: Displaces forward velocity, shearing the ball sharply to the right boundary line
                strategicForceVector = (flatForwardDir * 0.4f) + (flatRightDir * 1.0f) + (Vector3.up * 0.4f);
                Debug.Log("[SPIN SYSTEM] Aussie Drop Right! Shearing sharply toward right sideline.");
                break;

            case FootballSpinType.Knuckleball:
                // Knuckleball: Bypasses the predictable glide completely, forcing a standard random kick
                ExecuteErraticPointedTipBounce(impactForce);
                return;
        }

        // Inject the intentional strategic force vector into the physical Rigidbody stream
        rb.AddForce(strategicForceVector * redirectPower, ForceMode.VelocityChange);
    }

    private void ExecuteErraticPointedTipBounce(float impactForce)
    {
        float activeKickStrength = Mathf.Min(impactForce * 0.45f, maxBounceImpulseForce);

        float randomAngleRad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 randomHorizontalDirection = new Vector3(Mathf.Sin(randomAngleRad), 0f, Mathf.Cos(randomAngleRad));
        Vector3 combinedRandomImpulse = (randomHorizontalDirection * 1.0f) + (Vector3.up * Random.Range(0.3f, 0.8f));

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(combinedRandomImpulse.normalized * activeKickStrength, ForceMode.VelocityChange);

        Vector3 randomTorque = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), Random.Range(-20f, 20f));
        rb.AddTorque(randomTorque * activeKickStrength, ForceMode.VelocityChange);

        Debug.Log($"[BALL PHYSICS] Chaos Tip Strike Intercepted via Spin Mode: {activeSpinType}");
    }

    void FixedUpdate()
    {
        // 🛡️ EXIT GATE: Absolute physics safety shield
        if (rb == null || rb.isKinematic) return;

        float currentSpeed = rb.linearVelocity.magnitude;
        CheckIfBallIsRollingOnTurf();

        if (isTouchingGround && currentSpeed > 0.1f && currentSpeed <= rollThresholdVelocity)
        {
            float wobbleWave = Mathf.Sin(Time.time * 15f);

            if (wobbleWave > 0.7f || wobbleWave < -0.7f)
            {
                Vector3 subtleWobbleVector = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                float scaledWobbleForce = (currentSpeed / rollThresholdVelocity) * rollWobbleIntensity;

                float chaosScale = (activeSpinType == FootballSpinType.CoordinatedSpiral) ? 0.2f : 1.0f;
                rb.AddTorque(subtleWobbleVector * scaledWobbleForce * activeBallSpinChaosModifier * chaosScale, ForceMode.Acceleration);
            }
        }
    }

    private void CheckIfBallIsRollingOnTurf()
    {
        isTouchingGround = Physics.Raycast(transform.position, Vector3.down, 0.3f);
    }
}