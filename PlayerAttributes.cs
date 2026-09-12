using UnityEngine;

public class PlayerAttributes : MonoBehaviour
{
    [Header("0. Spreadsheet Identity Synchronization Key")]
    public string databasePlayerID = "PLAYER_01";
    public string playerNameDisplay = "Unknown Rookie";

    [Header("1. Biomechanical Mass & Stature")]
    public float armLengthInches = 34.5f;
    public float shoulderWidthInches = 22.0f;
    public float hipWidthInches = 18.0f;
    public float targetJumpHeight = 1.3f;
    public float targetAirPushHeight = 1.0f;

    [Header("2. Kinetic Chain Strength Profiles (0.0 to 1.0)")]
    [Range(0.1f, 1f)] public float upperBodyStrength = 0.75f;
    [Range(0.1f, 1f)] public float coreStrength = 0.70f;
    [Range(0.1f, 1f)] public float lowerBodyStrength = 0.80f;

    [Header("3. Neuromuscular Quick-Twitch Engine (0.0 to 1.0)")]
    [Range(0.1f, 1f)] public float quickTwitchExplosiveness = 0.80f;
    [Range(0.1f, 1f)] public float reactiveAgility = 0.75f;

    [Header("4. Joint & Turf Interaction Physics")]
    [Range(0.1f, 1f)] public float jointFlexibilityROM = 0.70f;
    public float cleatFrictionCoefficient = 0.65f;

    [Header("5. Pre-Play & Post-Snap Cognition")]
    public float calculationsPerSecondX = 8f;
    public int spatialResolutionRegions = 6;
    [Range(0.1f, 1f)] public float schematicRecognition = 0.75f;
    [Range(0.1f, 1f)] public float playbookFamiliarity = 0.85f;

    [Header("6. Psychological Stability")]
    [Range(0.1f, 1f)] public float mentalToughnessPoise = 0.80f;
}