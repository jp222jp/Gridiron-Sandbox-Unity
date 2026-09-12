using UnityEngine;
using System.Collections.Generic;

// 📋 THE RULEBOOK REPORT CARD: Returned by the validator to feed feedback to gameplay or editor screens
[System.Serializable]
public struct FormationLegalityReport
{
    public bool isLegal;
    public string feedbackMessage;

    public FormationLegalityReport(bool legal, string message)
    {
        isLegal = legal;
        feedbackMessage = message;
    }
}

// 🏈 UNIVERSAL INTERMEDIARY STRUCT: Translates live capsule coordinates or editor dot coordinates 
// into a unified format that the independent rulebook can evaluate anywhere.
[System.Serializable]
public struct RuleCheckPlayerNode
{
    public Vector3 localPosition;
    public bool isEligibleReceiverType;
    public Vector3 designedOrLiveVelocity; // Handles live physics velocities OR designed editor motion arrows
    public int playerID;
    public bool isDivingOrSliding; // Track locomotion state for kicker roughing filters
    public bool isTouchingDefender; // Track active live player collision flags

    public RuleCheckPlayerNode(Vector3 pos, bool eligibleType, Vector3 vel, int id, bool diving, bool touching)
    {
        localPosition = pos;
        isEligibleReceiverType = eligibleType;
        designedOrLiveVelocity = vel;
        playerID = id;
        isDivingOrSliding = diving;
        isTouchingDefender = touching;
    }
}

// 🏈 LIVE TRACKER: Manages the custom time-based forced out-of-bounds state loops per play down
public class ReceiverOutOfBoundsTracker
{
    public int playerID;
    public bool isIllegalTouchingActive = false;
    public float disengagementTimer = 0f;
    private bool wasInBoundsLastFrame = true;

    public void UpdateReceiverState(bool currentlyOutOfBounds, bool currentlyTouchingDefender, float deltaTime)
    {
        // Once shoes step back onto the legal grass turf, reset the active countdown timer
        if (!currentlyOutOfBounds)
        {
            disengagementTimer = 0f;
            wasInBoundsLastFrame = true;
            return;
        }

        // If illegal touching has already been permanently locked on for this play, lock execution
        if (isIllegalTouchingActive) return;

        // UNFORCED EXIT MATRIX: If they just crossed the boundary line this frame
        if (wasInBoundsLastFrame && currentlyOutOfBounds)
        {
            wasInBoundsLastFrame = false;

            // If they went out of bounds and were NOT being hit or touched by a defender, 
            // it's a voluntary exit. Arm the illegal touch state to '1' instantly for the rest of the play!
            if (!currentlyTouchingDefender)
            {
                isIllegalTouchingActive = true;
                Debug.LogWarning($"[RULE BOOK] ILLEGAL TOUCHING ARMED: Player ID {playerID} voluntarily stepped out of bounds.");
                return;
            }
        }

        // FORCED OUT OF BOUNDS ROUTINE: They were pushed out by contact
        if (currentlyTouchingDefender)
        {
            // Reset the countdown timer back to zero as long as contact is active outside
            disengagementTimer = 0f;
        }
        else
        {
            // The moment contact breaks, start ticking the 1-second countdown window you specified
            disengagementTimer += deltaTime;

            // THE TIME GATE: If they take longer than 1.0 second to get back in bounds, flip foul tracking state to 1
            if (disengagementTimer >= 1.0f)
            {
                isIllegalTouchingActive = true;
                Debug.LogWarning($"[RULE BOOK] ILLEGAL TOUCHING ARMED: Player ID {playerID} failed to re-enter bounds within 1 second after contact disengagement.");
            }
        }
    }

    /// <summary>
    /// PENALTY GATE TRIGGER: Called when the receiver physically contacts the football object mesh.
    /// </summary>
    public bool EvaluateBallContactTouchFoul()
    {
        if (isIllegalTouchingActive)
        {
            Debug.LogError("[REFEREE WHISTLE] 🚩 PENALTY: Illegal Touching! Receiver touched the football after going out of bounds illegally.");
            return true; // Flag thrown!
        }
        return false; // Clean catch allowed
    }
}

public static class GridironRuleBook
{
    // Dictionary to manage state loops across multiple active receiver options downfield
    public static Dictionary<int, ReceiverOutOfBoundsTracker> activeReceiverTrackers = new Dictionary<int, ReceiverOutOfBoundsTracker>();

    // =========================================================================
    // 📁 GROUP 1: STATIC FORMATION LEGALITY (Shared: Live Game & Play Developer Screen)
    // =========================================================================
    #region Static Formation Legality

    public static FormationLegalityReport EvaluateStaticFormation(List<RuleCheckPlayerNode> offenseNodes, float scrimmageZ)
    {
        if (offenseNodes.Count != 11)
        {
            return new FormationLegalityReport(false, $"ILLEGAL FORMATION: Squad must contain exactly 11 players. Found {offenseNodes.Count}.");
        }

        int menOnLineCount = 0;
        List<RuleCheckPlayerNode> linePlayers = new List<RuleCheckPlayerNode>();

        foreach (var player in offenseNodes)
        {
            float distanceToScrimmage = Mathf.Abs(player.localPosition.z - scrimmageZ);
            if (distanceToScrimmage <= 1.0f) // 1-yard alignment cushion tolerance
            {
                menOnLineCount++;
                linePlayers.Add(player);
            }
        }

        if (menOnLineCount < 7)
        {
            return new FormationLegalityReport(false, $"ILLEGAL FORMATION: Only {menOnLineCount} players on the line. Rules enforce a minimum of 7.");
        }

        if (linePlayers.Count > 0)
        {
            // Sort line players from left to right along the horizontal X-axis boundary width
            linePlayers.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

            if (!linePlayers[0].isEligibleReceiverType)
                return new FormationLegalityReport(false, "ILLEGAL FORMATION: Covered Receiver! Leftmost player on the line must be an eligible receiver type.");

            if (!linePlayers[linePlayers.Count - 1].isEligibleReceiverType)
                return new FormationLegalityReport(false, "ILLEGAL FORMATION: Covered Receiver! Rightmost player on the line must be an eligible receiver type.");
        }

        return new FormationLegalityReport(true, "FORMATION LEGAL");
    }

    #endregion

    // =========================================================================
    // 📁 GROUP 2: PRE-SNAP MOTION LEGALITY (Shared: Live Game & Play Developer Screen)
    // =========================================================================
    #region Pre-Snap Motion Legality

    public static FormationLegalityReport EvaluateSnapMotionLegality(List<RuleCheckPlayerNode> offenseNodes, bool isHomeTeamOnOffense)
    {
        int playersInMotionCount = 0;

        foreach (var player in offenseNodes)
        {
            // Evaluates active horizontal momentum right at the live snap frame OR on the designed editor track!
            if (player.designedOrLiveVelocity.magnitude > 0.1f)
            {
                playersInMotionCount++;

                // A player in motion cannot have a forward velocity vector relative to their end zone destination!
                float forwardVelocityZ = player.designedOrLiveVelocity.z;
                bool isMovingForward = isHomeTeamOnOffense ? (forwardVelocityZ > 0.05f) : (forwardVelocityZ < -0.05f);

                if (isMovingForward)
                {
                    return new FormationLegalityReport(false, "PENALTY / DESIGN ERROR: Illegal Motion! A designed motion path cannot move forward at the snap frame.");
                }
            }
        }

        if (playersInMotionCount > 1)
        {
            return new FormationLegalityReport(false, $"PENALTY / DESIGN ERROR: Illegal Shift! A play design cannot have more than 1 player in active motion at the snap frame.");
        }

        return new FormationLegalityReport(true, "SNAP MOTION LEGAL");
    }

    #endregion

    // =========================================================================
    // 📁 GROUP 3: SPECIAL TEAMS & KINETIC RULES (Fully Defined & Programmed)
    // =========================================================================
    #region Special Teams Kinetic Laws

    /// <summary>
    /// EVALUATE FAIR CATCH INTERFERENCE:
    /// Checks ball flight status, center-point capsule offsets, and velocity exceptions!
    /// </summary>
    public static bool EvaluateFairCatchInterference(Vector3 returnerPos, Vector3 defenderPos, Vector3 defenderVel, bool ballHasHitGroundOrPlayer)
    {
        if (ballHasHitGroundOrPlayer) return false;

        // Center-to-center radius check scaled to 2.0 yards to account for physical capsule pad thicknesses
        return Vector3.Distance(returnerPos, defenderPos) <= 2.0f && defenderVel.magnitude >= 0.1f;
    }

    /// <summary>
    /// EVALUATE KICKER CONTACT PENALTIES:
    /// Leverages velocity thresholds AND live locomotion states to auto-upgrade to 15-yard Roughing calls [2.1].
    /// </summary>
    public static string EvaluateKickerContactFoul(bool isKickerVulnerable, float defenderImpactVelocity, bool isDefenderDivingOrSliding)
    {
        if (!isKickerVulnerable) return "CLEAN CONTACT";

        // Automatically maps diving/sliding trajectories straight to roughing penalties natively [2.1]!
        if (isDefenderDivingOrSliding || defenderImpactVelocity > 5.0f)
        {
            return "PENALTY: Roughing the Kicker! (15 Yards & Automatic First Down)";
        }
        else if (defenderImpactVelocity > 0.1f)
        {
            return "PENALTY: Running Into the Kicker! (5 Yards)";
        }

        return "CLEAN CONTACT";
    }

    /// <summary>
    /// EVALUATE ILLEGAL BLOCK ABOVE THE WAIST:
    /// Upgraded from a flat 180° plane down to a high-precision 120° Rear Wedge to fully protect legal side blocks!
    /// </summary>
    public static bool EvaluateBlockAboveWaist(Vector3 defenderForward, Vector3 blockerPos, Vector3 defenderPos, Vector3 impactForce)
    {
        Vector3 directionToBlocker = (blockerPos - defenderPos).normalized;
        directionToBlocker.y = 0f;
        Vector3 flatForward = defenderForward;
        flatForward.y = 0f;

        float pushAngleDot = Vector3.Dot(flatForward, directionToBlocker);

        // Cosine threshold set to -0.50f limits the penalty wedge strictly to a 120-degree window behind the shoulder line
        if (pushAngleDot < -0.50f)
        {
            if (impactForce.y >= -0.1f) return true;
        }
        return false;
    }

    #endregion
}