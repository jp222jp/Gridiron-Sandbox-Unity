using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerPhysicsController : MonoBehaviour
{
    public static bool isMenuOpen = false;

    [Header("🏈 Context Core Options")]
    public bool isUserControlled = false;
    public string databasePlayerId = "DEFAULT_ID";
    public Transform secureBallHoldingAnchor;

    [Header("📐 Architectural Physical Trajectory Settings")]
    [Tooltip("Target vertical jumping leap distance capacity.")]
    public float structuralJumpHeightInchesFallback = 36.0f;

    [Header("🏎️ Steering Matrix Variables")]
    [Tooltip("Maximum open-field sprint speed for this baseline player athlete archetype.")]
    public float topSprintSpeedYards = 7.5f;
    [Tooltip("Maximum turning speed on the spot when pulling back or carving lanes.")]
    public float maxAgilityTurnSpeed = 190f;
    [Tooltip("Enforced deadzone to completely eliminate ghost drifting, endless spinning, and strafe twisting.")]
    public float analogDeadzoneFilterThreshold = 0.20f;

    public LayerMask groundCheckLayerMask;

    private bool isGrounded = true;

    // 🔥 THE SYNCHRONIZED CORE SYSTEM FLAG:
    // Exposes a pristine state handle that outside AI scripts and the ball engine read cleanly!
    [HideInInspector] public bool isCarryingBall = false;
    private GridironBallPhysics activeCarriedBallInstance = null;

    // Component Cache
    private PlayerAttributes attributes;
    private PlayerAnatomyRig anatomyRig;
    private PlayerBiomechanicalFailure failureEngine;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Original Xbox Mappings Framework Asset Instance
    private XboxControls hardwareControls;
    private Vector2 moveInput = Vector2.zero;
    private bool isStrafePressed = false;
    private Vector3 targetedIntentVector = Vector3.zero;
    private float targetLocomotionSpeed = 0f;
    private float nextCognitiveTickTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Rule #4 Compliance: Keep Y vertical free for leaps, lock X/Z tip-over rotators
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;

        hardwareControls = new XboxControls();
    }

    void OnEnable()
    {
        if (hardwareControls != null) hardwareControls.Gameplay.Enable();
    }

    void OnDisable()
    {
        if (hardwareControls != null) hardwareControls.Gameplay.Disable();
    }

    void Start()
    {
        attributes = GetComponent<PlayerAttributes>();
        anatomyRig = GetComponent<PlayerAnatomyRig>();
        failureEngine = GetComponent<PlayerBiomechanicalFailure>();
    }

    void Update()
    {
        // 🛡️ The Unified Shield Gate Protection Loop
        if (isMenuOpen) return;
        if (GridironMatchManager.Instance != null &&
            GridironMatchManager.Instance.generalCondition == GeneralMatchCondition.PausedSequence) return;

        if (!isUserControlled) return;

        if (hardwareControls != null)
        {
            Vector2 rawStick = hardwareControls.Gameplay.Move.ReadValue<Vector2>();
            isStrafePressed = hardwareControls.Gameplay.Strafe.IsPressed();

            if (rawStick.magnitude > analogDeadzoneFilterThreshold)
            {
                moveInput = rawStick;
            }
            else
            {
                moveInput = Vector2.zero;
                if (rb != null) rb.angularVelocity = Vector3.zero;
            }

            // STOPS STRAFE TWIST DRIFT:
            if (rb != null)
            {
                if (isStrafePressed)
                {
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                }
            }

            if (hardwareControls.Gameplay.Jump.triggered) ExecuteAthleticTakeoff();
        }

        // UNIFIED BALL HANDLING HARDWARE TRIGGER GATES:
        if (Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame)
        {
            if (!isCarryingBall)
            {
                EvaluateFootballPickupReachZone();
            }
            else
            {
                ExecuteCleanFootballDrop();
            }
        }

        // 🔥 STATE-BASED CARRIAGE OVERRIDE:
        // Position the ball precisely over the helmet/shoulder line while sprint-turning
        if (isCarryingBall && activeCarriedBallInstance != null)
        {
            activeCarriedBallInstance.transform.position = transform.position + (Vector3.up * 1.3f) + (transform.forward * 0.2f);
            activeCarriedBallInstance.transform.rotation = transform.rotation;
        }

        if (Time.time >= nextCognitiveTickTime)
        {
            SnapshotUserMovementIntent();
            nextCognitiveTickTime = Time.time + (1.0f / 60f);
        }
    }

    void FixedUpdate()
    {
        if (isMenuOpen || (GridironMatchManager.Instance != null &&
            GridironMatchManager.Instance.generalCondition == GeneralMatchCondition.PausedSequence)) return;

        ExecuteVolumeOverlapGroundCheck();
        ExecuteLocomotionSimulation();
    }

    private void SnapshotUserMovementIntent()
    {
        if (moveInput.sqrMagnitude < 0.01f)
        {
            targetedIntentVector = Vector3.zero;
            targetLocomotionSpeed = 0f;
            return;
        }

        float inputMagnitude = moveInput.magnitude;
        float scaledMagnitude = Mathf.Clamp01((inputMagnitude - analogDeadzoneFilterThreshold) / (1.0f - analogDeadzoneFilterThreshold));

        if (isStrafePressed)
        {
            targetedIntentVector = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            targetLocomotionSpeed = topSprintSpeedYards * 0.75f * scaledMagnitude;
        }
        else
        {
            if (moveInput.y >= -0.15f)
            {
                float turnExecutionMultiplier = Mathf.Abs(moveInput.x);
                float targetRotationYaw = moveInput.x * maxAgilityTurnSpeed * turnExecutionMultiplier * Time.deltaTime;
                transform.Rotate(0f, targetRotationYaw, 0f, Space.World);

                float speedDampenerCurve = Mathf.Lerp(1.0f, 0.70f, Mathf.Abs(moveInput.x));
                targetLocomotionSpeed = topSprintSpeedYards * scaledMagnitude * speedDampenerCurve;

                targetedIntentVector = transform.forward;
            }
            else
            {
                targetedIntentVector = Vector3.zero;
                targetLocomotionSpeed = 0f;

                float spinHeadingDirection = 1.0f;

                if (Mathf.Abs(moveInput.x) > 0.05f)
                {
                    spinHeadingDirection = Mathf.Sign(moveInput.x);
                }

                float pivotVelocity = maxAgilityTurnSpeed * scaledMagnitude * spinHeadingDirection;
                transform.Rotate(0f, pivotVelocity * Time.deltaTime, 0f, Space.World);
            }
        }
    }

    private void ExecuteLocomotionSimulation()
    {
        if (!isGrounded) return;

        if (targetedIntentVector.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, new Vector3(0f, rb.linearVelocity.y, 0f), 12f * Time.fixedDeltaTime);
            return;
        }

        if (rb.IsSleeping()) rb.WakeUp();

        Vector3 targetMoveVelocity = targetedIntentVector * targetLocomotionSpeed;
        rb.linearVelocity = new Vector3(targetMoveVelocity.x, rb.linearVelocity.y, targetMoveVelocity.z);
    }

    private void ExecuteVolumeOverlapGroundCheck()
    {
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();

        Vector3 capsuleBottomWorldCenter = transform.TransformPoint(capsuleCollider.center) - new Vector3(0f, capsuleCollider.height * 0.5f, 0f);
        Vector3 boxCenter = capsuleBottomWorldCenter + Vector3.up * 0.02f;
        float radius = capsuleCollider != null ? capsuleCollider.radius : 0.5f;

        Vector3 halfExtents = new Vector3(radius * 1.1f, 0.12f, radius * 1.1f);
        isGrounded = Physics.CheckBox(boxCenter, halfExtents, transform.rotation, groundCheckLayerMask);
    }

    private void ExecuteAthleticTakeoff()
    {
        if (isGrounded)
        {
            float g = Mathf.Abs(Physics.gravity.y);
            float targetHeightMeters = structuralJumpHeightInchesFallback * 0.0254f;
            float targetVelocityY = Mathf.Sqrt(2f * g * targetHeightMeters);

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, targetVelocityY, rb.linearVelocity.z);
            isGrounded = false;
            return;
        }

        ExecuteMidAirProximityPushOff();
    }

    private void ExecuteMidAirProximityPushOff()
    {
        float scanRadius = 1.2f; float scanHeight = 2.0f;
        Vector3 pointBottom = transform.position;
        Vector3 pointTop = transform.position + Vector3.up * scanHeight;

        Collider[] interceptedColliders = Physics.OverlapCapsule(pointBottom, pointTop, scanRadius);

        foreach (Collider victim in interceptedColliders)
        {
            if (victim.gameObject == this.gameObject) continue;

            PlayerPhysicsController opponentScript = victim.GetComponent<PlayerPhysicsController>();
            Rigidbody opponentRb = victim.GetComponent<Rigidbody>();

            if (opponentScript != null && opponentRb != null)
            {
                Vector3 directionToOpponent = (victim.transform.position - transform.position);
                directionToOpponent.y = 0f; directionToOpponent.Normalize();

                Vector3 facingHeadingForward = transform.forward; facingHeadingForward.y = 0f; facingHeadingForward.Normalize();
                float alignmentDotProduct = Vector3.Dot(facingHeadingForward, directionToOpponent);

                if (alignmentDotProduct < 0.1736f) continue;

                if (opponentScript.isGrounded)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 7.5f, rb.linearVelocity.z);
                }
                else
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 6.0f, rb.linearVelocity.z);
                    opponentRb.linearVelocity = new Vector3(opponentRb.linearVelocity.x, -4.5f, opponentRb.linearVelocity.z);
                }
                break;
            }
        }
    }

    private void EvaluateFootballPickupReachZone()
    {
        GridironBallPhysics footballAsset = FindAnyObjectByType<GridironBallPhysics>();
        if (footballAsset == null) return;

        float distanceToBall = Vector3.Distance(transform.position, footballAsset.transform.position);

        if (distanceToBall <= 2.2f)
        {
            Vector3 directionToBall = (footballAsset.transform.position - transform.position);
            directionToBall.y = 0f; directionToBall.Normalize();

            Vector3 facemaskHeadingForward = transform.forward; facemaskHeadingForward.y = 0f; facemaskHeadingForward.Normalize();
            float spatialAlignmentDot = Vector3.Dot(facemaskHeadingForward, directionToBall);

            if (spatialAlignmentDot < 0.0f) return;

            bool instantCaptureAllowed = true;
            if (GridironMatchManager.Instance != null && GridironMatchManager.Instance.specificPhase == SpecificMatchPhase.OngoingPlay)
            {
                if (Random.Range(0f, 100f) > 25f) return;
            }

            if (instantCaptureAllowed)
            {
                Rigidbody ballRb = footballAsset.GetComponent<Rigidbody>();
                Collider ballCollider = footballAsset.GetComponent<Collider>();

                if (ballRb != null)
                {
                    ballRb.isKinematic = true;
                    ballRb.linearVelocity = Vector3.zero;
                    ballRb.angularVelocity = Vector3.zero;
                }

                if (ballCollider != null && capsuleCollider != null)
                {
                    Physics.IgnoreCollision(capsuleCollider, ballCollider, true);
                }

                if (secureBallHoldingAnchor == null) secureBallHoldingAnchor = this.transform;
                footballAsset.transform.parent = secureBallHoldingAnchor;

                // 🔥 ESTABLISH THE SHAKING BRIDGE:
                // Cache the memory pointers and notify the system that this player now commands the ball state!
                activeCarriedBallInstance = footballAsset;
                isCarryingBall = true;

                Debug.Log("[BALL SYSTEM] State assigned to carrier. Communication bridge connected.");
            }
        }
    }

    private void ExecuteCleanFootballDrop()
    {
        if (activeCarriedBallInstance == null) return;

        Collider ballCollider = activeCarriedBallInstance.GetComponent<Collider>();
        Transform ballTransform = activeCarriedBallInstance.transform;
        ballTransform.parent = null;

        Rigidbody ballRb = activeCarriedBallInstance.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.isKinematic = false;
            Vector3 releaseForce = (transform.forward * 3f) + (Vector3.up * 1.5f);
            ballRb.linearVelocity = rb != null ? rb.linearVelocity + releaseForce : releaseForce;
        }

        if (capsuleCollider != null && ballCollider != null)
        {
            Physics.IgnoreCollision(capsuleCollider, ballCollider, false);
        }

        // 🔥 BREAK THE COMM BRIDGE CLEANLY:
        isCarryingBall = false;
        activeCarriedBallInstance = null;

        Debug.Log("[BALL SYSTEM] Dropped. State flag reset to false.");
    }
}