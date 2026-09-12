using UnityEngine;
using System.IO;

public enum OfficialCrewRole { Referee, Umpire, DownJudge, LineJudge, FieldJudge, SideJudge, BackJudge }

[System.Serializable]
public class OfficialProfileData
{
    public string official_id;
    public string title;
    public string archetype_focus;
    public float visual_fov_angle;
    public float sight_distance_yards;

    public float holding_recognition_index;
    public float pass_interference_recognition_index;
    public float roughing_passer_recognition_index;
    public float play_clock_awareness_reaction_time;

    public float movement_speed;
    public float acceleration;
    public float agility;
}

[System.Serializable]
public class OfficialDatabaseWrapper
{
    public OfficialProfileData[] officials;
}

public class GridironOfficialAgent : MonoBehaviour
{
    [Header("📋 Official Identity Mapping")]
    public OfficialCrewRole crewRole = OfficialCrewRole.Referee;

    // 🛠️ THE HIGHLIGHT SELECTION FILTER:
    // Type an ID here (like OFF_R_01 or OFF_R_02) to force this specific official capsule
    // to load that unique database row! Leave it blank to automatically pick the first matching title.
    [Tooltip("Type a specific ID from the JSON file to pick that referee crew profile. Leave blank for default.")]
    public string targetOfficialID = "";

    [Space(5)]
    public OfficialProfileData attributes = new OfficialProfileData();

    [Header("🔄 Automated Target Focus")]
    public Transform activeFocusTarget;

    [Header("🔍 FOV Projection Arc Controls")]
    public bool showVisionConesInGame = true;
    public bool useMaximumSightDistanceForTesting = false;
    public Color visionConeColor = new Color(0f, 1f, 1f, 0.4f);

    private Rigidbody rb;
    private LineRenderer fovLineRenderer;
    private bool hasPositionedPreSnap = false;
    private const int ARC_RESOLUTION_SEGMENTS = 20;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        fovLineRenderer = gameObject.GetComponent<LineRenderer>();
        if (fovLineRenderer == null) fovLineRenderer = gameObject.AddComponent<LineRenderer>();

        fovLineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        fovLineRenderer.startWidth = 0.12f;
        fovLineRenderer.endWidth = 0.12f;
        fovLineRenderer.positionCount = ARC_RESOLUTION_SEGMENTS + 2;
        fovLineRenderer.loop = true;

        LoadAttributesFromJSONDatabase();
    }

    void Start()
    {
        if (activeFocusTarget == null)
        {
            PlayerPhysicsController player = FindAnyObjectByType<PlayerPhysicsController>();
            if (player != null) activeFocusTarget = player.transform;
        }

        AutoPositionOfficialRelativeToLineOfScrimmage();
    }

    void FixedUpdate()
    {
        if (GridironMatchManager.Instance != null)
        {
            var currentPhase = GridironMatchManager.Instance.specificPhase;
            if (currentPhase == SpecificMatchPhase.Huddle || currentPhase == SpecificMatchPhase.PreSnap)
            {
                if (!hasPositionedPreSnap)
                {
                    AutoPositionOfficialRelativeToLineOfScrimmage();
                    hasPositionedPreSnap = true;
                }
            }
            else if (currentPhase == SpecificMatchPhase.OngoingPlay)
            {
                hasPositionedPreSnap = false;
                SimulateLiveAvoidanceAndTracking();
            }
        }

        RepaintInGameVisionCones();
    }

    private void LoadAttributesFromJSONDatabase()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "official_database.json");

        if (File.Exists(filePath))
        {
            string rawJsonText = File.ReadAllText(filePath);
            OfficialDatabaseWrapper database = JsonUtility.FromJson<OfficialDatabaseWrapper>(rawJsonText);

            if (database != null && database.officials != null)
            {
                string targetTitleString = crewRole.ToString();

                foreach (OfficialProfileData parsedOfficial in database.officials)
                {
                    // 🛠️ ADVANCED TARGET ID RESOLUTION ROUTINE:
                    // If you typed a specific ID into the inspector box, we look for an exact matching string!
                    if (!string.IsNullOrEmpty(targetOfficialID))
                    {
                        if (parsedOfficial.official_id.ToLower() == targetOfficialID.ToLower())
                        {
                            attributes = parsedOfficial;
                            Debug.Log($"[DATABASE] Explicitly loaded selected profile row: {attributes.official_id}");
                            return;
                        }
                    }
                    else
                    {
                        // Fallback behavior: If no ID was typed, grab the first one that matches the Crew Title
                        if (parsedOfficial.title.ToLower() == targetTitleString.ToLower())
                        {
                            attributes = parsedOfficial;
                            Debug.Log($"[DATABASE] Default selection assigned profile row: {attributes.official_id}");
                            return;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[DATABASE] official_database.json not found at {filePath}. Loading internal backup baselines.");
            LoadDefaultProfileBaselines();
        }
    }

    public void AutoPositionOfficialRelativeToLineOfScrimmage()
    {
        if (GridironMatchManager.Instance == null) return;

        float losZ = GridironMatchManager.Instance.lineOfScrimmageZ;
        float halfWidthYards = 53.33f * 0.5f;

        Vector3 targetSpawnPosition = new Vector3(0f, 1f, losZ);

        switch (crewRole)
        {
            case OfficialCrewRole.Referee: targetSpawnPosition = new Vector3(12f, 1f, losZ - 12f); break;
            case OfficialCrewRole.Umpire: targetSpawnPosition = new Vector3(-3f, 1f, losZ + 10f); break;
            case OfficialCrewRole.DownJudge: targetSpawnPosition = new Vector3(-halfWidthYards, 1f, losZ); break;
            case OfficialCrewRole.LineJudge: targetSpawnPosition = new Vector3(halfWidthYards, 1f, losZ); break;
            case OfficialCrewRole.FieldJudge: targetSpawnPosition = new Vector3(-halfWidthYards + 2f, 1f, losZ + 20f); break;
            case OfficialCrewRole.SideJudge: targetSpawnPosition = new Vector3(halfWidthYards - 2f, 1f, losZ + 20f); break;
            case OfficialCrewRole.BackJudge: targetSpawnPosition = new Vector3(0f, 1f, losZ + 25f); break;
        }

        transform.position = targetSpawnPosition;
        Vector3 losCenterPoint = new Vector3(0f, transform.position.y, losZ);
        transform.LookAt(losCenterPoint);
    }

    private void SimulateLiveAvoidanceAndTracking()
    {
        if (activeFocusTarget == null) return;

        Vector3 lookDirectionTarget = new Vector3(activeFocusTarget.position.x, transform.position.y, activeFocusTarget.position.z);
        transform.LookAt(lookDirectionTarget);

        float distanceToTarget = Vector3.Distance(transform.position, activeFocusTarget.position);
        if (distanceToTarget < 4.0f && rb != null)
        {
            Vector3 escapeDirection = (transform.position - activeFocusTarget.position).normalized;
            escapeDirection.y = 0f;
            rb.linearVelocity = escapeDirection * (attributes.movement_speed * attributes.agility * 0.5f);
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * attributes.acceleration);
        }
    }

    private void RepaintInGameVisionCones()
    {
        if (fovLineRenderer == null) return;

        if (!showVisionConesInGame)
        {
            fovLineRenderer.enabled = false;
            return;
        }

        fovLineRenderer.enabled = true;
        fovLineRenderer.startColor = visionConeColor;
        fovLineRenderer.endColor = visionConeColor;

        float activeProjectionLength = useMaximumSightDistanceForTesting ? attributes.sight_distance_yards : 3.0f;
        Vector3 groundOriginPoint = new Vector3(transform.position.x, 0.05f, transform.position.z);
        
        fovLineRenderer.SetPosition(0, groundOriginPoint);

        float horizontalFovRange = attributes.visual_fov_angle;
        float startingAngleOffset = -horizontalFovRange * 0.5f;
        float angleStepIncrement = horizontalFovRange / ARC_RESOLUTION_SEGMENTS;
        float currentFacingYawDegrees = transform.eulerAngles.y;

        for (int i = 0; i <= ARC_RESOLUTION_SEGMENTS; i++)
        {
            float targetSliceAngle = currentFacingYawDegrees + startingAngleOffset + (i * angleStepIncrement);
            float radianConversion = targetSliceAngle * Mathf.Deg2Rad;

            float calculatedX = transform.position.x + Mathf.Sin(radianConversion) * activeProjectionLength;
            float calculatedZ = transform.position.z + Mathf.Cos(radianConversion) * activeProjectionLength;

            Vector3 computedArcVertex = new Vector3(calculatedX, 0.05f, calculatedZ);
            fovLineRenderer.SetPosition(i + 1, computedArcVertex);
        }
    }

    private void LoadDefaultProfileBaselines()
    {
        attributes.official_id = "OFF_GEN_00";
        attributes.title = "Official Crew Member";
        attributes.archetype_focus = "General";
        attributes.visual_fov_angle = 100f; 
        attributes.sight_distance_yards = 22f;
        
        attributes.holding_recognition_index = 0.5f;
        attributes.pass_interference_recognition_index = 0.5f;
        attributes.roughing_passer_recognition_index = 0.5f;
        attributes.play_clock_awareness_reaction_time = 0.25f;

        attributes.movement_speed = 5.5f;
        attributes.acceleration = 4.0f;
        attributes.agility = 0.8f;
    }
}