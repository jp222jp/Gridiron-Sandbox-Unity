using UnityEngine;
using System.IO;

public class PlayerDatabaseImporter : MonoBehaviour
{
    [System.Serializable]
    public class JSONPlayerRow
    {
        public string databasePlayerID;
        public string playerName;
        public float mass;
        public float armLengthInches;
        public float shoulderWidthInches;
        public float hipWidthInches;
        public float targetJumpHeight;
        public float targetAirPushHeight;
        public float upperBodyStrength;
        public float coreStrength;
        public float lowerBodyStrength;
        public float quickTwitchExplosiveness;
        public float reactiveAgility;
        public float jointFlexibilityROM;
        public float cleatFrictionCoefficient;
        public float calculationsPerSecondX;
        public int spatialResolutionRegions;
        public float schematicRecognition;
        public float playbookFamiliarity;
        public float mentalToughnessPoise;
        public float injuryVulnerabilityCushion;
        public float injuryProbabilityScale;
        public float routeSharpnessBias;
    }

    [System.Serializable]
    public class JSONDatabaseWrapper
    {
        public JSONPlayerRow[] players;
    }

    public static void LoadPlayerDataFromSpreadsheet(PlayerAttributes stats, Rigidbody rb, PlayerBiomechanicalFailure health)
    {
        string fileName = "player_database.json";
        string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[DATA PIPELINE ERROR] Could not find the spreadsheet file at path: {fullPath}");
            return;
        }

        string jsonRawText = File.ReadAllText(fullPath);
        JSONDatabaseWrapper database = JsonUtility.FromJson<JSONDatabaseWrapper>(jsonRawText);

        if (database == null || database.players == null)
        {
            Debug.LogError("[DATA PIPELINE ERROR] JSON document parsing failed. Structural array wrapper alignment mismatch.");
            return;
        }

        // Clean up our search target ID into absolute UPPERCASE to ignore human spelling caps drops
        string searchTargetID = stats.databasePlayerID.Trim().ToUpper();

        JSONPlayerRow matchedRow = null;
        foreach (var row in database.players)
        {
            if (row.databasePlayerID != null)
            {
                // FORCE CASE-INSENSITIVE HANDSHAKE: Converts row keys to uppercase for a perfect secure check
                string checkRowID = row.databasePlayerID.Trim().ToUpper();

                if (checkRowID == searchTargetID)
                {
                    matchedRow = row;
                    break;
                }
            }
        }

        if (matchedRow != null)
        {
            stats.playerNameDisplay = matchedRow.playerName;

            if (rb != null)
            {
                rb.mass = matchedRow.mass;
            }

            stats.armLengthInches = matchedRow.armLengthInches;
            stats.shoulderWidthInches = matchedRow.shoulderWidthInches;
            stats.hipWidthInches = matchedRow.hipWidthInches;
            stats.targetJumpHeight = matchedRow.targetJumpHeight;
            stats.targetAirPushHeight = matchedRow.targetAirPushHeight;
            stats.upperBodyStrength = matchedRow.upperBodyStrength;
            stats.coreStrength = matchedRow.coreStrength;
            stats.lowerBodyStrength = matchedRow.lowerBodyStrength;
            stats.quickTwitchExplosiveness = matchedRow.quickTwitchExplosiveness;
            stats.reactiveAgility = matchedRow.reactiveAgility;
            stats.jointFlexibilityROM = matchedRow.jointFlexibilityROM;
            stats.cleatFrictionCoefficient = matchedRow.cleatFrictionCoefficient;
            stats.calculationsPerSecondX = matchedRow.calculationsPerSecondX;
            stats.spatialResolutionRegions = matchedRow.spatialResolutionRegions;
            stats.schematicRecognition = matchedRow.schematicRecognition;
            stats.playbookFamiliarity = matchedRow.playbookFamiliarity;
            stats.mentalToughnessPoise = matchedRow.mentalToughnessPoise;

            if (health != null)
            {
                health.injuryVulnerabilityCushion = matchedRow.injuryVulnerabilityCushion;
                health.injuryProbabilityScale = matchedRow.injuryProbabilityScale;
                health.routeSharpnessBias = matchedRow.routeSharpnessBias;
            }

            Debug.Log($"[DATA PIPELINE SUCCESS] Applied parameters for: {stats.playerNameDisplay} ({stats.databasePlayerID}). Core mass locked at {rb.mass}kg. Jump Height forced to {stats.targetJumpHeight}m.");
        }
        else
        {
            Debug.LogWarning($"[DATA PIPELINE WARNING] No entry in 'player_database.json' matched the identifier: '{searchTargetID}'. Verification failed.");
        }
    }
}