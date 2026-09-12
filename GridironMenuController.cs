using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GridironMenuController : MonoBehaviour
{
    private enum MenuLevel { Main, CameraSub }

    [Header("🎥 Script Dependencies")]
    public Camera sidelineCamera;
    public Camera playerPOVCamera;
    public GridironCameraController cameraController;

    private GameObject menuCanvasObject;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI listText;
    private TextMeshProUGUI sliderText;

    private MenuLevel currentLevel = MenuLevel.Main;
    private int currentSelectionIndex = 0;

    // 📐 ULTRA-TELEPHOTO SYSTEM BOUNDS: 
    // Lowering MIN_ZOOM_FOV to 0.5 allows the camera to achieve extreme close-up details!
    private const float MIN_ZOOM_FOV = 0.5f;
    private const float MAX_ZOOM_FOV = 35f;
    private float sliderSensitivitySpeed = 18f;

    // Cleaned up arrays with Lock and Unlock items completely removed
    private string[] mainMenuItems = { "Camera", "Audible", "Substitute Players", "Coach Adjustments" };
    private string[] cameraMenuItems = { "Set camera view type", "Set zoom level" };

    void Start()
    {
        if (cameraController == null)
        {
            cameraController = GetComponent<GridironCameraController>();
        }

        GameObject rootUI = new GameObject("Gridiron_HUD_Menu_Root");
        Canvas canvas = rootUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootUI.AddComponent<UnityEngine.UI.CanvasScaler>();
        rootUI.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        menuCanvasObject = rootUI;

        GameObject bgPanel = new GameObject("Menu_Background");
        bgPanel.transform.parent = rootUI.transform;
        var bgImage = bgPanel.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.92f);

        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.1f, 0.22f);
        bgRect.anchorMax = new Vector2(0.9f, 0.85f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ROW 1: HEADER INSTRUCTIONS
        GameObject headerObj = new GameObject("HUD_Header_Instructions");
        headerObj.transform.parent = bgPanel.transform;
        headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.fontSize = 20;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = Color.yellow;
        headerText.text = "Select with LB  |  Exit with RB  |  Scroll Down with LT  |  Scroll Up with RT";

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.90f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        // ROW 2: MENU SELECTION STACK
        GameObject listObj = new GameObject("HUD_Selection_Column");
        listObj.transform.parent = bgPanel.transform;
        listText = listObj.AddComponent<TextMeshProUGUI>();
        listText.fontSize = 32;
        listText.alignment = TextAlignmentOptions.Left;
        listText.color = Color.white;

        RectTransform listRect = listObj.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.05f, 0.38f);
        listRect.anchorMax = new Vector2(0.95f, 0.88f);
        listRect.offsetMin = Vector2.zero;
        listRect.offsetMax = Vector2.zero;

        // ROW 3: PROCEDURAL COMPACT DATA SLIDER
        GameObject sliderObj = new GameObject("HUD_Zoom_Slider_Overlay");
        sliderObj.transform.parent = bgPanel.transform;
        sliderText = sliderObj.AddComponent<TextMeshProUGUI>();
        sliderText.fontSize = 24;
        sliderText.fontStyle = FontStyles.Normal;
        sliderText.alignment = TextAlignmentOptions.Center;
        sliderText.color = Color.white;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.05f, 0.02f);
        sliderRect.anchorMax = new Vector2(0.95f, 0.32f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        menuCanvasObject.SetActive(false);
    }

    void Update()
    {
        bool pressedLB = Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
        bool pressedRB = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool pressedLT = Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame;
        bool pressedRT = Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.leftShoulder.wasPressedThisFrame) pressedLB = true;
            if (Gamepad.current.rightShoulder.wasPressedThisFrame) pressedRB = true;
            if (Gamepad.current.leftTrigger.wasPressedThisFrame) pressedLT = true;
            if (Gamepad.current.rightTrigger.wasPressedThisFrame) pressedRT = true;
        }

        if (!PlayerPhysicsController.isMenuOpen)
        {
            if (pressedLB)
            {
                PlayerPhysicsController.isMenuOpen = true;
                currentLevel = MenuLevel.Main;
                currentSelectionIndex = 0;
                menuCanvasObject.SetActive(true);

                // Synchronize slider state seamlessly with the active camera value when opening the menu
                if (cameraController != null)
                {
                    cameraController.defaultFOV = Mathf.Clamp(cameraController.defaultFOV, MIN_ZOOM_FOV, MAX_ZOOM_FOV);
                }
                RepaintMenuScreen();
            }
            return;
        }

        if (pressedRB)
        {
            if (currentLevel == MenuLevel.CameraSub)
            {
                currentLevel = MenuLevel.Main;
                currentSelectionIndex = 0;
                RepaintMenuScreen();
            }
            else
            {
                PlayerPhysicsController.isMenuOpen = false;
                menuCanvasObject.SetActive(false);
            }
            return;
        }

        if (pressedLT)
        {
            currentSelectionIndex++;
            int maxItems = (currentLevel == MenuLevel.Main) ? mainMenuItems.Length : cameraMenuItems.Length;
            if (currentSelectionIndex >= maxItems) currentSelectionIndex = 0;
            RepaintMenuScreen();
        }
        if (pressedRT)
        {
            currentSelectionIndex--;
            int maxItems = (currentLevel == MenuLevel.Main) ? mainMenuItems.Length : cameraMenuItems.Length;
            if (currentSelectionIndex < 0) currentSelectionIndex = maxItems - 1;
            RepaintMenuScreen();
        }

        if (currentLevel == MenuLevel.CameraSub && currentSelectionIndex == 1)
        {
            HandleLiveZoomSliderAdjustment();
        }

        if (pressedLB)
        {
            ExecuteMenuSelectionAction();
        }
    }

    private void HandleLiveZoomSliderAdjustment()
    {
        float changeInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.isPressed) changeInput += 1f;
            if (Keyboard.current.leftArrowKey.isPressed) changeInput -= 1f;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.right.isPressed) changeInput += 1f;
            if (Gamepad.current.dpad.left.isPressed) changeInput -= 1f;
        }

        if (Mathf.Abs(changeInput) > 0.01f && cameraController != null)
        {
            // Dynamic adjustment scaling factor speeds up tracking near max wide bounds 
            // and tapers smoothly down for microscopic precision as you near 0.5
            float dynamicSpeedModifier = Mathf.Lerp(0.15f, 1f, (cameraController.defaultFOV - MIN_ZOOM_FOV) / (MAX_ZOOM_FOV - MIN_ZOOM_FOV));
            float trackingTargetFOV = cameraController.defaultFOV;

            trackingTargetFOV += changeInput * sliderSensitivitySpeed * dynamicSpeedModifier * Time.deltaTime;
            trackingTargetFOV = Mathf.Clamp(trackingTargetFOV, MIN_ZOOM_FOV, MAX_ZOOM_FOV);

            cameraController.defaultFOV = trackingTargetFOV;
            RepaintMenuScreen();
        }
    }

    private void RepaintMenuScreen()
    {
        string compiledTextStack = "";
        string[] activeArray = (currentLevel == MenuLevel.Main) ? mainMenuItems : cameraMenuItems;

        for (int i = 0; i < activeArray.Length; i++)
        {
            if (i == currentSelectionIndex)
            {
                compiledTextStack += $"<color=yellow><b>[ > ] {activeArray[i]}</b></color>";

                if (currentLevel == MenuLevel.CameraSub && i == 0)
                {
                    string activeCamName = (sidelineCamera != null && sidelineCamera.enabled) ? "Sideline" : "POV";
                    compiledTextStack += $" <color=#00FF00>({activeCamName})</color>";
                }
            }
            else
            {
                compiledTextStack += $"      {activeArray[i]}";
            }
            compiledTextStack += "\n";
        }

        listText.text = compiledTextStack;

        if (currentLevel == MenuLevel.CameraSub && cameraController != null)
        {
            float activeMenuFOV = cameraController.defaultFOV;
            float normValue = (activeMenuFOV - MIN_ZOOM_FOV) / (MAX_ZOOM_FOV - MIN_ZOOM_FOV);
            int barLengthChars = 30;
            int dotHandleIndex = Mathf.RoundToInt(normValue * barLengthChars);

            int tightNotchIndex = Mathf.RoundToInt((10f - MIN_ZOOM_FOV) / (MAX_ZOOM_FOV - MIN_ZOOM_FOV) * barLengthChars);
            int mediumNotchIndex = Mathf.RoundToInt((20f - MIN_ZOOM_FOV) / (MAX_ZOOM_FOV - MIN_ZOOM_FOV) * barLengthChars);
            int wideNotchIndex = Mathf.RoundToInt((35f - MIN_ZOOM_FOV) / (MAX_ZOOM_FOV - MIN_ZOOM_FOV) * barLengthChars);

            string trackSliderString = "";
            for (int j = 0; j <= barLengthChars; j++)
            {
                if (j == dotHandleIndex)
                {
                    trackSliderString += "O";
                }
                else if (j == tightNotchIndex || j == mediumNotchIndex || j == wideNotchIndex)
                {
                    trackSliderString += "|";
                }
                else
                {
                    trackSliderString += "-";
                }
            }

            string bottomLabelsLine = "          Tight           Medium           Wide  ";

            string activePresetLabel = "Custom Detail";
            if (Mathf.Abs(activeMenuFOV - 35f) < 0.6f) activePresetLabel = "Wide View Preset";
            if (Mathf.Abs(activeMenuFOV - 20f) < 0.6f) activePresetLabel = "Medium View Preset";
            if (Mathf.Abs(activeMenuFOV - 10f) < 0.6f) activePresetLabel = "Tight View Preset";
            if (activeMenuFOV <= 2.5f) activePresetLabel = "<color=cyan><b>Microscopic Macro View</b></color>";

            sliderText.text = $"Zoom Track:  [ {MIN_ZOOM_FOV:F1} {trackSliderString} {MAX_ZOOM_FOV:F0} ]\n{bottomLabelsLine}\n\nCurrent Field-of-View Focus: {activeMenuFOV:F1}° -> ({activePresetLabel})";
        }
        else
        {
            sliderText.text = "";
        }
    }

    private void ExecuteMenuSelectionAction()
    {
        if (currentLevel == MenuLevel.Main)
        {
            if (currentSelectionIndex == 0)
            {
                currentLevel = MenuLevel.CameraSub;
                currentSelectionIndex = 0;
                RepaintMenuScreen();
            }
        }
        else if (currentLevel == MenuLevel.CameraSub)
        {
            switch (currentSelectionIndex)
            {
                case 0: // Set camera view type
                    if (sidelineCamera != null && playerPOVCamera != null)
                    {
                        bool isSidelineActive = sidelineCamera.enabled;
                        sidelineCamera.enabled = !isSidelineActive;
                        playerPOVCamera.enabled = isSidelineActive;
                    }
                    break;
            }
            RepaintMenuScreen();
        }
    }
}