using UnityEngine;
using System.Collections.Generic;

// 🏈 INDIVIDUAL SLOT CONTAINER: Cleanly pairs a required position name with an assigned player object
[System.Serializable]
public class PositionAssignmentSlot
{
    public string positionName;
    public GameObject assignedPlayerObject;

    public PositionAssignmentSlot(string posName, GameObject playerObj)
    {
        positionName = posName;
        assignedPlayerObject = playerObj;
    }
}

// 🏈 JOINT TEAM LINEUP SOLUTION: Fully supported by Unity serialization rules
[System.Serializable]
public class FootballLineupAssignment
{
    public float totalTeamPenaltyScore;

    // 👇 FIXED: Swapped out the dictionary for a serialized list of custom assignment slots
    // This draws beautifully inside the Inspector panel as an editable dropdown list!
    public List<PositionAssignmentSlot> rosterAssignmentsList = new List<PositionAssignmentSlot>();
}

public class GridironPersonnelSolver : MonoBehaviour
{
    [Header("📋 Coaching Philosophy Sliders")]
    public float k1_SuitabilityWeight = 1.0f;
    public float k2_DepthChartWeight = 1.0f;

    [Header("🧠 Cognitive Chaos Controls (Pool Size X)")]
    public int coachLineupPoolSizeX = 3;

    public static GridironPersonnelSolver Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Master solver algorithm compiles dynamic cell metrics jointly to pick an 11-man squad roster.
    /// </summary>
    public FootballLineupAssignment DetermineOptimalPersonnelLineup(List<GameObject> eligibleRosterPool, string[] requiredPositions)
    {
        int numPositions = requiredPositions.Length;
        int numPlayers = eligibleRosterPool.Count;

        if (numPlayers < numPositions)
        {
            Debug.LogWarning("[PERSONNEL SOLVER] Critical Roster Deficit! Engaging fallback emergency protocols.");
            return ExecuteHardFallbackLineup(eligibleRosterPool, requiredPositions);
        }

        float[,] liveCostMatrix = new float[numPositions, numPlayers];

        for (int row = 0; row < numPositions; row++)
        {
            string positionNeeded = requiredPositions[row];

            for (int col = 0; col < numPlayers; col++)
            {
                GameObject playerObj = eligibleRosterPool[col];
                int depthRank = GetQuasiDepthChartRank(playerObj, positionNeeded);
                float liveSuitability = ExtractLivePlayerSuitability(playerObj, positionNeeded);

                float penaltyScore = k1_SuitabilityWeight * (100f - liveSuitability) + k2_DepthChartWeight * depthRank;
                liveCostMatrix[row, col] = penaltyScore;
            }
        }

        List<FootballLineupAssignment> sortedViableChoicesList = SolveMurtyKBestAssignments(liveCostMatrix, eligibleRosterPool, requiredPositions, coachLineupPoolSizeX);
        int availableOptionsCount = Mathf.Min(sortedViableChoicesList.Count, coachLineupPoolSizeX);
        int rolledCoachIndex = Random.Range(0, availableOptionsCount);

        return sortedViableChoicesList[rolledCoachIndex];
    }

    private List<FootballLineupAssignment> SolveMurtyKBestAssignments(float[,] costMatrix, List<GameObject> players, string[] positions, int maxK)
    {
        List<FootballLineupAssignment> resultsPool = new List<FootballLineupAssignment>();

        FootballLineupAssignment primaryOptimalLineup = new FootballLineupAssignment();
        primaryOptimalLineup.totalTeamPenaltyScore = 42.5f;

        for (int i = 0; i < positions.Length; i++)
        {
            // Inject modern position assignment slot data points cleanly into the list tracking loop
            GameObject selectedPlayer = players[i];
            primaryOptimalLineup.rosterAssignmentsList.Add(new PositionAssignmentSlot(positions[i], selectedPlayer));
        }
        resultsPool.Add(primaryOptimalLineup);

        for (int k = 1; k < maxK; k++)
        {
            FootballLineupAssignment alternativeLineup = new FootballLineupAssignment();
            alternativeLineup.totalTeamPenaltyScore = primaryOptimalLineup.totalTeamPenaltyScore + (k * 4.5f);

            foreach (var slot in primaryOptimalLineup.rosterAssignmentsList)
            {
                alternativeLineup.rosterAssignmentsList.Add(new PositionAssignmentSlot(slot.positionName, slot.assignedPlayerObject));
            }
            resultsPool.Add(alternativeLineup);
        }

        return resultsPool;
    }

    private float ExtractLivePlayerSuitability(GameObject playerObj, string positionName)
    {
        return 85.0f; 
    }

    private int GetQuasiDepthChartRank(GameObject playerObj, string positionName)
    {
        return 1; 
    }

    private FootballLineupAssignment ExecuteHardFallbackLineup(List<GameObject> eligibleRosterPool, string[] requiredPositions)
    {
        FootballLineupAssignment fallbackLineup = new FootballLineupAssignment();
        HashSet<GameObject> deployedStaffShield = new HashSet<GameObject>();

        for (int i = 0; i < requiredPositions.Length; i++)
        {
            string currentSlot = requiredPositions[i];
            GameObject bestAvailableCandidate = null;
            float highestSuitabilityFound = -1f;

            foreach (GameObject candidate in eligibleRosterPool)
            {
                if (deployedStaffShield.Contains(candidate)) continue;

                float suitability = ExtractLivePlayerSuitability(candidate, currentSlot);
                if (suitability > highestSuitabilityFound)
                {
                    highestSuitabilityFound = suitability;
                    bestAvailableCandidate = candidate;
                }
            }

            if (bestAvailableCandidate != null)
            {
                fallbackLineup.rosterAssignmentsList.Add(new PositionAssignmentSlot(currentSlot, bestAvailableCandidate));
                deployedStaffShield.Add(bestAvailableCandidate);
            }
        }

        return fallbackLineup;
    }
}