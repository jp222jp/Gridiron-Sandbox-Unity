using UnityEngine;
using TMPro;

public enum GeneralMatchCondition { ActiveSequence, PausedSequence }

public enum SpecificMatchPhase
{
    Huddle,
    PreSnap,
    OngoingPlay,
    PlayDead,
    StandardTimeout,
    InjuryTimeout,
    EndOfQuarter,
    Halftime
}

public enum PersonnelGroup { StandardOffense, StandardDefense, GoalLinePackage, PuntingUnit, PuntReturnTeam, KickoffTeam, KickReturnTeam }

public class GridironMatchManager : MonoBehaviour
{
    [Header("📋 Master Match Alignment (1 Unit = 1 Yard)")]
    public float lineOfScrimmageZ = 0f;
    public bool isHomeTeamOnOffense = true;
    public int currentQuarter = 1;

    [Header("🔄 Hierarchical State Matrix")]
    public GeneralMatchCondition generalCondition = GeneralMatchCondition.PausedSequence;
    public SpecificMatchPhase specificPhase = SpecificMatchPhase.StandardTimeout;

    [Header("⏰ Gridiron Time Tracking Engine")]
    public float gameClockSeconds = 900f;
    public float playClockSeconds = 40f;
    private bool isGameClockRunning = false;
    private bool isPlayClockRunning = false;

    // 🛠️ THE FLOOD PROTECTION SHIELD: Prevents the play clock from spamming logs on every frame
    private bool hasSignaledDelayOfGame = false;

    [Header("🏟️ Dynamic Personnel Stance Tracking")]
    public PersonnelGroup homeActivePersonnel = PersonnelGroup.StandardOffense;
    public PersonnelGroup awayActivePersonnel = PersonnelGroup.StandardDefense;

    [Header("🏈 Team Scoreboard Statistics")]
    public string awayTeamName = "AWY";
    public int awayScore = 0;
    public int awayTimeoutsRemaining = 3;
    public const int TOTAL_STARTING_TIMEOUTS = 3;

    [Space(5)]
    public string homeTeamName = "HOM";
    public int homeScore = 0;
    public int homeTimeoutsRemaining = 3;

    private GameObject hudCanvasObject;
    private TextMeshProUGUI scoreboardTextText;

    public static GridironMatchManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResetPlayClock();
        firstDownTargetZ = lineOfScrimmageZ + yardsToGainLine;

        BuildProceduralScoreboardHUDOverlay();
        TransitionToPhase(SpecificMatchPhase.Huddle);
    }

    [Header("📐 Down & Distance Matrices")]
    public int currentDown = 1;
    public float yardsToGainLine = 10f;
    public float firstDownTargetZ = 10f;

    void Update()
    {
        if (generalCondition == GeneralMatchCondition.PausedSequence)
        {
            isGameClockRunning = false;
            isPlayClockRunning = false;
        }
        else
        {
            EvaluateActiveSubPhaseClockRules();
        }

        if (isGameClockRunning)
        {
            gameClockSeconds -= Time.deltaTime;
            if (gameClockSeconds <= 0f)
            {
                gameClockSeconds = 0f;
                isGameClockRunning = false;
                HandleQuarterExpirationSequence();
            }
        }

        if (isPlayClockRunning)
        {
            playClockSeconds -= Time.deltaTime;
            if (playClockSeconds <= 0f)
            {
                playClockSeconds = 0f;
                isPlayClockRunning = false;

                // 👇 FIXED: Only prints ONE time when the play clock hits zero, preventing console flooding!
                if (!hasSignaledDelayOfGame)
                {
                    Debug.LogWarning("[MATCH REF] PLAY CLOCK VIOLATION: Delay of Game Threshold Crossed!");
                    hasSignaledDelayOfGame = true;
                }
            }
        }

        RepaintScoreboardHUDGraphics();
    }

    public void SubstituteTeamPersonnelPackage(bool targetHomeTeam, PersonnelGroup newPackage)
    {
        if (targetHomeTeam) homeActivePersonnel = newPackage;
        else awayActivePersonnel = newPackage;
    }

    private void EvaluateActiveSubPhaseClockRules()
    {
        switch (specificPhase)
        {
            case SpecificMatchPhase.Huddle:
                isPlayClockRunning = true;
                isGameClockRunning = false;
                break;
            case SpecificMatchPhase.PreSnap:
                isPlayClockRunning = true;
                break;
            case SpecificMatchPhase.OngoingPlay:
                isPlayClockRunning = false;
                isGameClockRunning = true;
                break;
            case SpecificMatchPhase.PlayDead:
                isPlayClockRunning = false;
                isGameClockRunning = false;
                break;
        }
    }

    private void HandleQuarterExpirationSequence()
    {
        if (currentQuarter < 4)
        {
            currentQuarter++;
            gameClockSeconds = 900f;
            awayTimeoutsRemaining = TOTAL_STARTING_TIMEOUTS;
            homeTimeoutsRemaining = TOTAL_STARTING_TIMEOUTS;
            TransitionToPhase(SpecificMatchPhase.EndOfQuarter);
        }
        else
        {
            TransitionToPhase(SpecificMatchPhase.Halftime);
        }
    }

    public void TransitionToPhase(SpecificMatchPhase newPhase)
    {
        specificPhase = newPhase;

        if (newPhase == SpecificMatchPhase.StandardTimeout ||
            newPhase == SpecificMatchPhase.InjuryTimeout ||
            newPhase == SpecificMatchPhase.EndOfQuarter ||
            newPhase == SpecificMatchPhase.Halftime)
        {
            generalCondition = GeneralMatchCondition.PausedSequence;
        }
        else
        {
            generalCondition = GeneralMatchCondition.ActiveSequence;
        }

        if (newPhase == SpecificMatchPhase.PreSnap || newPhase == SpecificMatchPhase.Huddle)
        {
            ResetPlayClock();
        }
    }

    public void ResetPlayClock()
    {
        playClockSeconds = 40f;
        // 👇 FIXED: Lowering the shield gate here resets the warning system for the next play cycle!
        hasSignaledDelayOfGame = false;
    }

    private void BuildProceduralScoreboardHUDOverlay()
    {
        hudCanvasObject = new GameObject("Gridiron_MainGameplay_HUD");
        Canvas canvas = hudCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();

        GameObject bannerBackplate = new GameObject("HUD_Scoreboard_Banner");
        bannerBackplate.transform.parent = hudCanvasObject.transform;
        var bgImage = bannerBackplate.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

        RectTransform bgRect = bannerBackplate.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.2f, 0.90f);
        bgRect.anchorMax = new Vector2(0.8f, 0.98f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject textContainer = new GameObject("HUD_Display_Text");
        textContainer.transform.parent = bannerBackplate.transform;
        scoreboardTextText = textContainer.AddComponent<TextMeshProUGUI>();
        scoreboardTextText.fontSize = 22;
        scoreboardTextText.alignment = TextAlignmentOptions.Center;
        scoreboardTextText.color = Color.white;

        RectTransform textRect = textContainer.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void RepaintScoreboardHUDGraphics()
    {
        if (scoreboardTextText == null) return;

        string awayDots = "";
        for (int i = 1; i <= TOTAL_STARTING_TIMEOUTS; i++)
            awayDots += (i <= awayTimeoutsRemaining) ? "<color=red>●</color>" : "<color=#444444>○</color>";

        string homeDots = "";
        for (int i = 1; i <= TOTAL_STARTING_TIMEOUTS; i++)
            homeDots += (i <= homeTimeoutsRemaining) ? "<color=green>●</color>" : "<color=#444444>○</color>";

        int gameMin = Mathf.FloorToInt(gameClockSeconds / 60f);
        int gameSec = Mathf.FloorToInt(gameClockSeconds % 60f);
        string gameClockString = $"{gameMin:00}:{gameSec:00}";
        string playClockString = Mathf.CeilToInt(playClockSeconds).ToString();

        string awayPossessionMarker = (!isHomeTeamOnOffense) ? "<color=yellow>◄</color> " : "   ";
        string homePossessionMarker = (isHomeTeamOnOffense) ? " <color=yellow>►</color>" : "   ";

        string downSuffix = (currentDown == 1) ? "st" : (currentDown == 2) ? "nd" : (currentDown == 3) ? "rd" : "th";
        string downDistanceString = $"{currentDown}{downSuffix} & {yardsToGainLine:F0}";
        string ballSpotString = (lineOfScrimmageZ == 0) ? "50" : (lineOfScrimmageZ < 0) ? $"Own {50 + lineOfScrimmageZ:F0}" : $"Opp {50 - lineOfScrimmageZ:F0}";

        scoreboardTextText.text =
            $"{awayPossessionMarker}<b>{awayTeamName}</b> {awayScore}  {awayDots}   |   <color=yellow><b>Q{currentQuarter}</b></color>  {gameClockString}  [<color=orange><b>{playClockString}</b></color>]   |   {homeDots}  {homeScore} <b>{homeTeamName}</b>{homePossessionMarker}\n" +
            $"<size=16><color=#CCCCCC>Down: {downDistanceString}  •  Ball On: {ballSpotString}  •  Home Package: {homeActivePersonnel}  •  Away Package: {awayActivePersonnel}</color></size>";
    }

    public void SimulateBallSnapInput() { if (specificPhase == SpecificMatchPhase.PreSnap) TransitionToPhase(SpecificMatchPhase.OngoingPlay); }
    public void SimulateWhistlePlayDead() { if (specificPhase == SpecificMatchPhase.OngoingPlay) TransitionToPhase(SpecificMatchPhase.PlayDead); }
}