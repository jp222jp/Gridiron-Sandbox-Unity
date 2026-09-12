using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPhysicsController : MonoBehaviour
{
    // 🛠️ THE HUD SELECTION WINDOW GLOBAL SHIELD
    public static bool isMenuOpen = false;

    [Header("Control State Toggle")]
    public bool isUserControlled = true;
    public float baseRunSpeed = 8f;

    [Header("Athletic Inertia Parameters")]
    public float backwardTurnSpeed = 220f;
    public float lateralTurnSpeed = 190f;
    public bool isRightHanded = true;

    [Header("Perimeter Layers & Envelopes")]
    public LayerMask groundLayer;
    public float raycastDistance = 1.20f; // Cushion for smooth stadium ramp stepping
    public LayerMask playerLayer;

    // 👁️ THE VISION CONE MATRIX:
    // This variable strictly governs BOTH jumping push-off limits and football pickups!
    [Range(0f, 360f)] public float reachForwardArcAngle = 160f;

    [Header("🦘 Core Jumping Baselines")]
    public float standardJumpVelocity = 5.5f;
    public float pushOffAirborneVelocity = 6.0f;
    public float pushOffGroundedVelocity = 7.5f;
    public float downwardRecoilImpulse = 4.5f;
    public float proximityRadiusYards = 1.2f;
    public float proximityHeightYards = 2.0f;
    [Range(0f, 1f)] public float midAirControlDamping = 0.35f;

    [Header("🏟️ Relative Agility Multipliers")]
    public float sprintForwardScale = 1.0f;
    public float sprintLateralScale = 0.70f;
    public float sprintBackwardScale = 0.55f;
    public float strafeUniformScale = 0.75f;

    [Header("🏉 Football Possession Mechanics")]
    [Tooltip("Drop your physical Football primitive sphere object slot here in the inspector.")]
    public GameObject footballObject;

    [Tooltip("Maximum horizontal/vertical distance (in yards) the player can reach to secure the ball.")]
    public float ballReachZoneRadiusYards = 2.5f;

    [Range(0f, 100f)] public float activePlayPickupSuccessRate = 25f;

    private bool isCarryingBall = false;
    private Rigidbody ballRigidbody;

    private Rigidbody rb;
    public bool isGrounded { get; private set; } = false;

    private XboxControls controls;
    private Vector2 moveInput;
    private bool isStrafing = false;
    private float targetHeadingDegrees = 0f;

    private PlayerAttributes stats;
    private PlayerAnatomyRig anatomy;
    private PlayerBiomechanicalFailure health;

    private float telemetryTimer = 0f;
    private float optimizationTimer = 0f;
    private Vector3 optimizedTargetVector = Vector3.zero;
    private int frameDelayCountdown = 0;

    void Awake()
    {
        if (isUserControlled) controls = new XboxControls();

        stats = GetComponent<PlayerAttributes>();
        anatomy = GetComponent<PlayerAnatomyRig>();
        health = GetComponent<PlayerBiomechanicalFailure>();
        rb = GetComponent<Rigidbody>();

        if (stats != null && health != null && rb != null)
        {
            PlayerDatabaseImporter.LoadPlayerDataFromSpreadsheet(stats, rb, health);
        }
    }

    void OnEnable()
    {
        if (isUserControlled && controls != null) controls.Gameplay.Enable();
    }

    void OnDisable()
    {
        if (isUserControlled && controls != null) controls.Gameplay.Disable();
    }

    void Start()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        targetHeadingDegrees = transform.eulerAngles.y;

        if (stats != null && anatomy != null) anatomy.InitializeAndScaleRig(stats);
        if (stats != null && health != null) health.SetUpDependencies(rb, stats);

        if (footballObject != null)
        {
            ballRigidbody = footballObject.GetComponent<Rigidbody>();
        }

        CalculateNeuralReactionDelay();
    }

    void CalculateNeuralReactionDelay()
    {
        if (stats == null) return;
        float cognitiveCompetence = stats.schematicRecognition * 0.5f + stats.playbookFamiliarity * 0.5f;
        frameDelayCountdown = Mathf.RoundToInt(35f * (1.0f - cognitiveCompetence));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame))
        {
            if (health != null) health.ForceInstantHealAndReset();
        }

        if (health != null && health.isInjured) return;
        if (isMenuOpen) return;

        // Listens for Left Stick Click to trigger manual reach check attempts
        if (Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame)
        {
            EvaluateFootballPickupReachZone();
        }

        // Solid Keyboard Test Hook (Tap 'G' to pick up / drop)
        if (Input.GetKeyDown(KeyCode.G))
        {
            EvaluateFootballPickupReachZone();
        }

        if (isUserControlled && controls != null)
        {
            moveInput = controls.Gameplay.Move.ReadValue<Vector2>();
            isStrafing = controls.Gameplay.Strafe.IsPressed();

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) ||
               (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame))
            {
                ExecuteJumpLogic();
            }

            telemetryTimer += Time.deltaTime;
            if (telemetryTimer >= 0.25f) { telemetryTimer = 0f; OutputTelemetryToLog(); }
        }
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        bool raycastHitGround = Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, groundLayer);

        if (raycastHitGround && (hit.collider.name.Contains("Chalk") || hit.collider.name.Contains("Overlay") || hit.collider.name.Contains("Digit")))
        {
            raycastHitGround = false;
        }

        isGrounded = raycastHitGround;

        if (health != null && health.isInjured) return;
        if (isMenuOpen)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (frameDelayCountdown > 0)
        {
            frameDelayCountdown--;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isUserControlled && stats != null)
        {
            float optimizationInterval = 1.0f / stats.calculationsPerSecondX;
            optimizationTimer += Time.fixedDeltaTime;

            if (optimizationTimer >= optimizationInterval)
            {
                optimizationTimer = 0f;
                optimizedTargetVector = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }

            float dynamicAgilityModifier = EvaluateBiomechanicalAgilityScale(optimizedTargetVector, isStrafing);
            float activeMovementSpeed = baseRunSpeed * dynamicAgilityModifier;

            if (isGrounded)
            {
                float dynamicTurnSpeed = lateralTurnSpeed * (stats.coreStrength * 0.6f + stats.lowerBodyStrength * 0.4f);
                if (health != null) dynamicTurnSpeed *= (1.0f + health.routeSharpnessBias * 0.4f);

                if (isStrafing)
                {
                    if (optimizedTargetVector.magnitude > 0.1f)
                    {
                        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

                        Vector3 customDriveDirection = (flatForward * optimizedTargetVector.z) + (flatRight * optimizedTargetVector.x);
                        Vector3 worldRelativeVelocity = customDriveDirection.normalized * activeMovementSpeed;

                        rb.linearVelocity = new Vector3(worldRelativeVelocity.x, rb.linearVelocity.y, worldRelativeVelocity.z);
                    }
                    else
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.1f, rb.linearVelocity.y, rb.linearVelocity.z * 0.1f);
                    }
                    transform.rotation = Quaternion.Euler(0f, targetHeadingDegrees, 0f);
                }
                else
                {
                    if (moveInput.y < -0.85f && Mathf.Abs(moveInput.x) < 0.15f)
                    {
                        float pivotDirection = isRightHanded ? 1.0f : -1.0f;
                        targetHeadingDegrees += pivotDirection * backwardTurnSpeed * Time.fixedDeltaTime;
                    }
                    else if (Mathf.Abs(moveInput.x) > 0.1f)
                    {
                        targetHeadingDegrees += moveInput.x * dynamicTurnSpeed * Time.fixedDeltaTime;
                    }

                    targetHeadingDegrees = Mathf.Repeat(targetHeadingDegrees, 360f);
                    transform.rotation = Quaternion.Euler(0f, targetHeadingDegrees, 0f);

                    if (moveInput.y > 0.1f)
                    {
                        float explosiveLaunchSpeed = activeMovementSpeed * (0.8f + stats.quickTwitchExplosiveness * 0.2f);
                        Vector3 relativeForwardVelocity = transform.forward * moveInput.y * explosiveLaunchSpeed;
                        rb.linearVelocity = new Vector3(relativeForwardVelocity.x, rb.linearVelocity.y, relativeForwardVelocity.z);
                    }
                    else
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.1f, rb.linearVelocity.y, rb.linearVelocity.z * 0.1f);
                    }
                }
            }
            else
            {
                Vector3 currentAirVelocity = rb.linearVelocity;

                if (optimizedTargetVector.magnitude > 0.1f)
                {
                    Vector3 airForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                    Vector3 airRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                    Vector3 desiredAirDirection = (airForward * optimizedTargetVector.z) + (airRight * optimizedTargetVector.x);

                    Vector3 targetedAirVelocity = desiredAirDirection.normalized * activeMovementSpeed;

                    float airX = Mathf.MoveTowards(currentAirVelocity.x, targetedAirVelocity.x, activeMovementSpeed * midAirControlDamping * Time.fixedDeltaTime);
                    float airZ = Mathf.MoveTowards(currentAirVelocity.z, targetedAirVelocity.z, activeMovementSpeed * midAirControlDamping * Time.fixedDeltaTime);

                    rb.linearVelocity = new Vector3(airX, currentAirVelocity.y, airZ);
                }
                else
                {
                    rb.linearVelocity = new Vector3(currentAirVelocity.x, currentAirVelocity.y, currentAirVelocity.z);
                }
            }
        }
    }

    private void EvaluateFootballPickupReachZone()
    {
        if (footballObject == null)
        {
            Debug.LogWarning("[GAMEPLAY] Cannot pick up ball: 'Football Object' slot is unassigned in the Inspector!");
            return;
        }

        if (isCarryingBall)
        {
            DropFootballOnTurf();
            return;
        }

        Vector3 playerChestCenter = transform.position + (Vector3.up * 1.0f);
        float distanceToBall = Vector3.Distance(playerChestCenter, footballObject.transform.position);

        Debug.Log($"[possession] Distance check evaluated: {distanceToBall:F2} yards away. Max Reach: {ballReachZoneRadiusYards} yards.");

        if (distanceToBall <= ballReachZoneRadiusYards)
        {
            // 📐 VECTOR ARC SHIELD: Filters out ball pickup clicks if the ball rests outside our 160-degree chest sightlines
            Vector3 directionToBall = (footballObject.transform.position - transform.position).normalized;
            directionToBall.y = 0f;
            Vector3 forwardHeading = transform.forward;
            forwardHeading.y = 0f;

            float dotProductProduct = Vector3.Dot(forwardHeading, directionToBall);
            float halfArcAngleDegrees = reachForwardArcAngle * 0.5f;
            float targetCosThreshold = Mathf.Cos(halfArcAngleDegrees * Mathf.Deg2Rad);

            if (dotProductProduct < targetCosThreshold)
            {
                Debug.Log($"[POSSESSION] Pickup Denied! Football is behind player's {reachForwardArcAngle}° forward vision arc profile.");
                return;
            }

            if (GridironMatchManager.Instance != null)
            {
                if (GridironMatchManager.Instance.specificPhase == SpecificMatchPhase.OngoingPlay)
                {
                    float randomRoll = Random.Range(0f, 100f);
                    if (randomRoll > activePlayPickupSuccessRate)
                    {
                        Debug.Log($"[POSSESSION] FUMBLE MUFFED! Recovery roll failed ({randomRoll:F1} > {activePlayPickupSuccessRate}%).");
                        return;
                    }
                }
            }

            SecureFootballPossessionSuccess();
        }
    }

    private void SecureFootballPossessionSuccess()
    {
        // Launches the safe, single-frame delay routine to bypass the collision frame clash entirely
        StartCoroutine(ExecuteSafeDelayedPickupSequence());
    }

    private System.Collections.IEnumerator ExecuteSafeDelayedPickupSequence()
    {
        isCarryingBall = true;

        // 1. Wait until the absolute end of the current frame so Unity fully finishes processing the active collision frame
        yield return new WaitForEndOfFrame();

        // 2. Safely disarm the physical shell after physics threads have closed
        Collider ballCollider = footballObject != null ? footballObject.GetComponent<Collider>() : null;
        if (ballCollider != null)
        {
            ballCollider.enabled = false;
        }

        // 3. Set kinematic mode safely now that no active collision loops are calculating forces
        if (ballRigidbody != null)
        {
            ballRigidbody.isKinematic = true;
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
        }

        // 4. Secure the parent attachment link smoothly
        footballObject.transform.parent = this.transform;

        // Tight local placement coordinates clear of your capsule physics lines
        footballObject.transform.localPosition = new Vector3(0.35f, 0.50f, 0.60f);
        footballObject.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        Debug.Log("[GAMEPLAY] Football successfully SECURED via single-frame collision delay bypass!");
    }

    private void DropFootballOnTurf()
    {
        isCarryingBall = false;
        footballObject.transform.parent = null;

        // 1. Re-arm collider shell instantly for turf fumbles
        Collider ballCollider = footballObject != null ? footballObject.GetComponent<Collider>() : null;
        if (ballCollider != null)
        {
            ballCollider.enabled = true;
        }

        // 2. Restore environment physics impacts
        if (ballRigidbody != null)
        {
            ballRigidbody.isKinematic = false;
            ballRigidbody.linearVelocity = transform.forward * 4.5f + Vector3.up * 1.5f;
        }

        Debug.Log("[GAMEPLAY] Football dropped back onto turf.");
    }

    private float EvaluateBiomechanicalAgilityScale(Vector3 moveVector, bool strafeIsActive)
    {
        if (moveVector.magnitude < 0.05f) return strafeIsActive ? strafeUniformScale : sprintForwardScale;
        if (strafeIsActive) return strafeUniformScale;

        float forwardFactor = Mathf.Max(0f, moveVector.z);
        float backwardFactor = Mathf.Max(0f, -moveVector.z);
        float lateralFactor = Mathf.Abs(moveVector.x);

        float calculatedSprintScale = 0f;
        calculatedSprintScale += forwardFactor * sprintForwardScale;
        calculatedSprintScale += backwardFactor * sprintBackwardScale;
        calculatedSprintScale += lateralFactor * sprintLateralScale;

        return Mathf.Clamp(calculatedSprintScale / (forwardFactor + backwardFactor + lateralFactor), sprintBackwardScale, sprintForwardScale);
    }

    private void ExecuteJumpLogic()
    {
        float dynamicJumpAgilityModifier = EvaluateBiomechanicalAgilityScale(optimizedTargetVector, isStrafing);
        float activeVerticalJumpPower = standardJumpVelocity * dynamicJumpAgilityModifier;

        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, activeVerticalJumpPower, rb.linearVelocity.z);
            isGrounded = false;
        }
        else
        {
            Vector3 pointBottom = transform.position;
            Vector3 pointTop = transform.position + (Vector3.up * proximityHeightYards);
            Collider[] objectsInZone = Physics.OverlapCapsule(pointBottom, pointTop, proximityRadiusYards, playerLayer);

            foreach (Collider col in objectsInZone)
            {
                if (col.gameObject == this.gameObject) continue;

                // 📐 CROWD SCREEN FILTER: Sequentially scans ALL nearby capsules. 
                // It dynamically pushes off the FIRST valid body it detects passing the 160-degree vision face line.
                Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                directionToTarget.y = 0f;
                Vector3 currentForward = transform.forward;
                currentForward.y = 0f;

                float targetDot = Vector3.Dot(currentForward, directionToTarget);
                float halfArcAngleDegrees = reachForwardArcAngle * 0.5f;
                float allowedCosThreshold = Mathf.Cos(halfArcAngleDegrees * Mathf.Deg2Rad);

                if (targetDot < allowedCosThreshold) continue; // Instantly skips this body if they are in our blindspot, checking the next player!

                Rigidbody targetRb = col.GetComponent<Rigidbody>();
                PlayerPhysicsController opponentScript = col.GetComponent<PlayerPhysicsController>();

                if (targetRb != null)
                {
                    bool isOpponentGrounded = true;
                    if (opponentScript != null) isOpponentGrounded = opponentScript.isGrounded;

                    if (isOpponentGrounded)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, pushOffGroundedVelocity * dynamicJumpAgilityModifier, rb.linearVelocity.z);
                    }
                    else
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, pushOffAirborneVelocity * dynamicJumpAgilityModifier, rb.linearVelocity.z);
                        targetRb.linearVelocity = new Vector3(targetRb.linearVelocity.x, -downwardRecoilImpulse, targetRb.linearVelocity.z);
                    }
                    break;
                }
            }
        }
    }

    private void OutputTelemetryToLog()
    {
        if (health == null || stats == null) return;
        Debug.Log($"[TELEMETRY CORE] Biomechanical Health Connected | Brain Updates X: {stats.calculationsPerSecondX}/sec");
    }
}