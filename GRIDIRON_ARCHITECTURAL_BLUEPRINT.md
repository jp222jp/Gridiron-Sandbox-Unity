By keeping it right next to your code, you ensure it is automatically backed up if you duplicate your folder or commit changes to Git. When you start a future AI session, your very first prompt can simply be: "Read this blueprint file to understand the exact structure and rules of my game engine before writing any code."





markdown# 🏈 GRIDIRON GAME ENGINE: MASTER ARCHITECTURAL BLUEPRINT



This document serves as the absolute source of truth for the spatial alignment, state engines, and combinatorial optimization logic of the project. Any future updates must strictly adhere to the constraints and variables documented below.



\---



\## 📐 1. GLOBAL SYSTEM CONSTRAINTS

\*   \*\*The Expanded Out-of-Bounds Grid:\*\* The total terrestrial environment foundation spans 300x350 yards via `Stadium\_Ground\_Foundation`. Outer collision barriers are fixed at X = ±150 and Z = ±175 to perfectly accommodate a GEHA Field at Arrowhead Stadium structural footprint envelope.

\*   \*\*The Scale Matrix:\*\* Exactly 1 Unit = 1 Yard across all axes. All velocity vectors, spatial checks, and measurements conform to this 1:1 scale natively.

\*   \*\*Field Boundary Dimensions:\*\* The gridiron width is exactly 53.33 units, centered perfectly at X = 0. The out-of-bounds boundary threshold triggers if `Mathf.Abs(transform.position.x) > 26.665f`.

\*   \*\*The Unified Shield:\*\* All player physics calculation updates and gameplay movement inputs must immediately exit the calculation loops (`return;`) if `PlayerPhysicsController.isMenuOpen` or `GridironMatchManager.Instance.generalCondition == GeneralMatchCondition.PausedSequence` evaluate to true.

\*   \*\*The Oblong Procedural Tumble Interceptor:\*\* The football handles physics via `GridironBallPhysics.cs`. It maps strategic kicking intents using `FootballSpinType` ({ CoordinatedSpiral, Backspin, AussieDropLeft, AussieDropRight, Knuckleball }). The spin selection alters both the chaos bucket probability size and the physical redirection vector forces on ground contact: CoordinatedSpiral dampens chaos (0.35x) and boosts linear glide; Backspin enforces a sharp backward/vertical recoil vector; Aussie modes displace momentum laterally to shear the ball 90-degrees toward sideline ramps; Knuckleball maximizes the chaos bucket size (1.45x) to ensure pure erratic behavior.



\---



\## 🎛️ 2. THE CHRONOS STATE MANAGER (GridironMatchManager.cs)

Implements a strict Hierarchical State Machine (HSM). States are decoupled into a generic global condition layer and specific sub-phases to eliminate redundant code loops and sync errors.



\### Data Models (Enums)

\*   `GeneralMatchCondition`: { ActiveSequence, PausedSequence }

\*   `SpecificMatchPhase`: { Huddle, PreSnap, OngoingPlay, PlayDead, StandardTimeout, InjuryTimeout, EndOfQuarter, Halftime }



\### Structural Constraints \& Rules

1\.  \*\*The Pause Shield:\*\* If `generalCondition == GeneralMatchCondition.PausedSequence`, BOTH the Game Clock and the Play Clock are globally locked to false at a single point in code. No sub-state (Timeout, Injury, etc.) can ever accidentally let a clock run.

2\.  \*\*State-Driven Time Rules:\*\* 

&#x20;   \*   `Huddle` \& `PreSnap` enforce Play Clock = true, Game Clock = false (or inherited).

&#x20;   \*   `OngoingPlay` forces Play Clock = false, Game Clock = true.

3\.  \*\*Scoreboard HUD Display Engine:\*\* Uses dynamic procedural string construction to output live team names, colors, scores, game periods, digital game clock tracking, down, distance, ball spot yard line relative to center field (`50`), and procedural typographic indicators for spent/remaining timeouts (`●` and `○`).

4\.  \*\*Possession Consistency Rule:\*\* Possession (`isHomeTeamOnOffense`) tracks which team handles the tactical system and must remain completely static and unchanged through both active plays and paused sequences until a down is officially declared dead by a referee whistle.



\---



\## 🧠 3. THE PERSONNEL MATRIX OPTIMIZATION SOLVER (GridironPersonnelSolver.cs)

Replaces traditional linear, robotic depth-chart replacement loops with a combinatorial multi-variable optimization matrix. It solves the football alignment challenge \*jointly\* rather than evaluating positions in isolation.



\### Why It Is Structured This Way

\*   \*\*Anti-Isolation Design:\*\* A standard sequential assignment loop is blind to the whole roster. If it locks a player into Position A simply because they are the highest rated there, it can leave Position B with a complete liability. The solver evaluates the entire team configuration \*jointly\* to achieve the lowest possible total team penalty score.

\*   \*\*Live Attribute Decay Calculation:\*\* Player suitability for a position is calculated \*live in real-time\* rather than loaded as a static JSON number. If a player triggers a playable injury or drains fatigue, their active speed/explosiveness properties drop inside `PlayerAttributes.cs`. The solver reads these modified states on frame zero, automatically lowering their suitability and sliding healthy or better-suited versatile players into the line dynamically.

\*   \*\*The K-Best Pool Extension (Murty's Algorithm):\*\* Instead of calculating an absolute perfect lineup every single time (which creates robotic, perfect AI behavior), the solver generates an ordered list of the top `X` unique team combinations.

\*   \*\*The Statistical Chaos Principle:\*\* The size of the lineup sample pool `X` (`coachLineupPoolSizeX`) is tied directly to coaching attributes. An elite coach has an `X` of 1 or 2 (picking optimal configurations). A novice or panicked coach has a larger `X`, expanding their selection pool to include sub-optimal combinations to realistically simulate human management error.

\*   \*\*The Cost Function Formula:\*\* `Score = k1 \* (100 - LiveSuitability) + k2 \* DepthChartRank`. Inverting suitability ensures high talent produces a low penalty score, driving the assignment matrix toward a global minimum sum. `k1` and `k2` function as coaching philosophy sliders (Talent vs. Depth Scheme Discipline).

\*   \*\*The Fallback Protection Shield:\*\* If severe injury waves decimate the healthy roster pool below the formation's positional requirement count, the solver activates an emergency loop. It forces all remaining healthy bodies onto the field based on depth chart rankings, uses a Suitability Tie-Breaker to resolve versatile duplicates, and prevents game crashes during full-season simulation passes.



\---



\## 📐 4. STANCE-AWARE ATHLETIC LOCOMOTION (PlayerPhysicsController.cs)

Decouples locomotion attributes based explicitly on the active user input stance modifier, matching real-world biomechanics.



\### Stance Variations

\*   \*\*The Strafe Gear (LT Squeezed):\*\* The player drops their hips into a low tracking stance. Forward, backward, lateral, and diagonal agility are flattened to a uniform baseline scale (`strafeUniformScale = 0.75f`). Speed and jump capabilities behave symmetrically in all directions to facilitate responsive lateral tracking.

\*   \*\*The Open-Field Sprint Gear (LT Released):\*\* The player unlocks their hips to chase downfield velocity. Forward agility functions at 100% capacity (`sprintForwardScale = 1.0f`), while lateral shuffling and un-strafed backward jumps are damped (`0.70f` and `0.55f`) to prevent unrealistic sideways or reverse launching.

\*   \*\*Diagonal Vector Blending:\*\* Evaluates raw stick movement inputs as angular dot contributions relative to the capsule's forward chest visor heading vector to ensure smooth acceleration interpolation across 45-degree transitions.



\### The Physics Trajectory Protection Shields

1\.  \*\*The Mid-Air Momentum Shield:\*\* When `isGrounded` evaluates to false, all ground-cut braking deceleration parameters are completely bypassed. If a user releases the left thumbstick at the peak of a running jump, the horizontal velocity vectors are left completely un-damped, allowing the capsule to trace a natural projectile physics arc across the yards until cleats make physical contact with the grass layer.

2\.  \*\*The Ground Check Apex Fix:\*\* Removed legacy velocity checks that mistakenly flagged the player as grounded at the dead-stop vertical apex (`linearVelocity.y < 0.05f`) of a jump. Grounding is strictly bound to physical downward raycasts.

3\.  \*\*The Chalk Mesh Collision Filter:\*\* The downward raycast tests the name of target colliders. If it encounters a component containing the strings `"Chalk"`, `"Overlay"`, or `"Digit"`, it marks `isGrounded` as false. This prevents athletes from getting an accidental "double jump" boost off the painted field lines.

4\.  \*\*The Compact Zone of Proximity Cylinder:\*\* Mid-air jumps trigger an `OverlapCapsule` cylinder scan extending out horizontally (`1.2 yards`) and vertically (`2.0 yards`) up to shoulder height. If it intercepts an object layer, it calculates real-time momentum conservation:

&#x20;   \*   \*If Opponent is Grounded:\* Opponent acts like a solid wall brace; player pops up with a maximum velocity boost (`7.5`).

&#x20;   \*   \*If Opponent is Mid-Air:\* Kinetic energy splits. Player gains a moderate leap boost (`6.0`), while the airborne defender's Rigidbody receives a sharp downward recoil impulse (`-4.5`), spiking them straight into the turf.

5\.  \*\*Dynamic Fumble Probability Filter:\*\* Football pickup interactions (`EvaluateFootballPickupReachZone()`) analyze the live Match Manager phase index. Outside-of-play phases allow a 100% instant capture lock step. Active `OngoingPlay` phases clamp pickup execution behind a strict 25% statistical success roll parameter per tap to realistically simulate messy on-field scramble conditions. User input commands map exclusively to Xbox Left Stick Press (L3) and the keyboard 'G' key fallback.



\---



\## ⚖️ 5. SPATIAL FIELD GRID OFFICIATING (GridironOfficialAgent.cs)

Transforms match rules from random background dice rolls into an explicit, 3D physical visibility grid.



\### The Regulatory Surveillance Positioning Grid

Seven individual official capsule agents automate their initial pre-snap coordinate footprints flat on the turf based on the active line of scrimmage (`losZ`):

\*   `Referee`: 12 yards back in the offensive backfield, favoring the passing-arm side of the QB.

\*   `Umpire`: 10 yards back into the defensive backfield behind linebackers.

\*   `DownJudge` / `LineJudge`: Straddling the exact line of scrimmage line on opposite sideline boundary edges.

\*   `FieldJudge` / `SideJudge`: 20 yards deep downfield from the LOS line along the sideline boundaries.

\*   `BackJudge`: 25 yards deep downfield, anchored centered directly between the hash line indicators.



\### In-Game Render Toggles

\*   \*\*The Live Arc Renderer:\*\* Uses a native `LineRenderer` flat vertex loop to paint a semi-transparent vision cone projection flat onto the grass at the official's feet during regular gameplay.

\*   \*\*The Proportional Visibility Switches:\*\* 

&#x20;   \*   `showVisionConesInGame`: A master boolean checkbox that instantly toggles line drawing on or off to preserve field aesthetics.

&#x20;   \*   `useMaximumSightDistanceForTesting`: When false, clips the visible arc to a tight `3.0 yards` to keep indicators clean. When true, projects the lines out to their maximum real-world sight limits (e.g., `40 yards`) for strict debugging passes.


\---



\## ⚖️ 6. DECOUPLED STRATEGIC SCRIMMAGE LEGALITY (GridironRuleBook.cs)

\*   \*\*The 34-Switch Customization Mandate:\*\* The game engine operates with 34 discrete customizable rule configurations, globally controlled outside of active gameplay loops via `GridironRuleSettings.cs`. Every single flag logic pipeline must verify `GridironRuleSettings.Instance.IsPenaltyTypeActive()` before flashing yellow flags on the television HUD banner.

\*   \*\*Decoupled Rule Divisions:\*\* 

&#x20;   1. \*Group A, B, \& C (Pre-Snap, Boundaries, Clocks, \& Special Teams):\* Fully defined, mathematical, and operational. Group C processes advanced vector metrics natively (`EvaluateFairCatchInterference()`, `EvaluateKickerContactFoul()`, `EvaluateBlockAboveWaist()`) using direct velocity magnitudes, capsule padding radius offsets, and 180-degree horizontal facemask cutting boundaries.

\*   \*\*Targeting \& League Scaling Constraints:\*\* Features an adjustable `LeagueRulesMode` toggle switch ({ NFL, NCAA\_D1 }). Selecting NCAA\_D1 activates the custom `Targeting` placeholder interceptor loops, changing penalties to automatic player ejection reviews. Selecting NFL transforms helmet-contact safety alerts into traditional Unnecessary Roughness classifications.

\*   \*\*The 120° Rear Wedge Constraint:\*\* `EvaluateBlockAboveWaist()` narrows the illegal contact vector from a 180-degree flat plane to a strict 120-degree rear wedge (Cosine threshold = -0.50f) to fully accommodate legal blocking mechanics from the side/parallel profile.

\*   \*\*The Forced Out-of-Bounds State Loop:\*\* Implements real-time time tracking via `ReceiverOutOfBoundsTracker`. Receivers leaving the field width boundaries (X = ±26.66) check for defender proximity. Active contact freezes the tracking clock. Breaking contact activates a 1.0-second re-entry countdown timer loop. Failure to cross back into legal play space before the timer expires flags an irreversible `isIllegalTouchingActive = true` condition state for that specific play down.

\*   \*\*The Forced vs. Unforced Out-of-Bounds Matrix:\*\* Integrates complete boundary entry logic via `ReceiverOutOfBoundsTracker`. Receivers crossing past the field limits (X = ±26.66) execute an immediate contact scan. Voluntary exits without active defender contact lock `isIllegalTouchingActive = true` instantly. Forced exits due to defender contact freeze the clock, initializing a 1.0-second re-entry countdown upon defender disengagement. Any physical contact between the football asset mesh and an armed receiver node (`EvaluateBallContactTouchFoul()`) drops an immediate Illegal Touching rule violation flag on the field.


























FUTURE IMPROVEMENTS

How Other Scripts Will Trigger This Future InteractionWhen we build your passing, punting, or fumbling code later down the road, making a kicker's attribute lock onto the ball is now incredibly simple. The player script will simply run this single, clean handshake line right when the ball leaves their foot:csharp// Example of future kicking code functionality:

GridironBallPhysics ballScript = footballObject.GetComponent<GridironBallPhysics>();

if (ballScript != null)

{

&#x20;   // High skill kicker rating (e.g. 95) drops chaos multiplier to 0.2f for a tight spiral!

&#x20;   ballScript.activeBallSpinChaosModifier = Mathf.Lerp(1.5f, 0.2f, kickerPlayerStats.spiralControl / 100f);

}








