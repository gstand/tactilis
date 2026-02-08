#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click scene builder for the Tactilis AR rehab game.
/// Menu: Tactilis > Setup Full Scene
///
/// Creates a physically-accurate table surface with buttons mapped to real-world
/// ergonomic dimensions based on Zone 1 primary reach research (BOSTONtec):
///   - Zone 1 (neutral reach): up to 35.5cm (14") horizontal
///   - Button spacing: 4.5cm (comfortable finger reach for rehab)
///   - Button diameter: 3cm (realistic press target)
///   - Table height: 0.75m (standard desk)
///
/// In AR, the grid is calibrated to the player's fingertip position on a real table.
/// </summary>
public class TactilisSceneSetup : EditorWindow
{
    // --- Ergonomic defaults (real-world meters) ---
    private int gridRows = 3;
    private int gridCols = 3;
    private float buttonSpacing = 0.045f;   // 4.5cm — comfortable finger reach for rehab
    private float buttonDiameter = 0.030f;  // 3cm — realistic press target
    private float tablePadding = 0.04f;     // 4cm padding around outermost buttons
    private float tableHeight = 0.75f;      // 0.75m — standard desk height
    private float tableThickness = 0.005f;  // 5mm visual thickness
    private float buttonHeight = 0.003f;    // 3mm — buttons sit just above table surface
    private bool createXROrigin = true;

    [MenuItem("Tactilis/Setup Full Scene")]
    public static void ShowWindow()
    {
        GetWindow<TactilisSceneSetup>("Tactilis Scene Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tactilis AR Rehab — Scene Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Builds a physically-accurate table surface with buttons:\n\n" +
            "• Table surface maps to a real-world desk (0.75m height)\n" +
            "• Buttons are flat discs ON the table (3cm diameter, 4.5cm apart)\n" +
            "• Spacing based on ergonomic Zone 1 primary reach research\n" +
            "• In AR, grid is placed at fingertip on a real table via calibration\n\n" +
            "Run on a fresh/empty scene for best results.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        GUILayout.Label("Grid Layout", EditorStyles.boldLabel);
        gridRows = EditorGUILayout.IntSlider("Rows", gridRows, 2, 5);
        gridCols = EditorGUILayout.IntSlider("Columns", gridCols, 2, 5);

        EditorGUILayout.Space(5);
        GUILayout.Label("Ergonomic Dimensions (meters)", EditorStyles.boldLabel);
        buttonSpacing = EditorGUILayout.Slider("Button Spacing", buttonSpacing, 0.03f, 0.08f);
        buttonDiameter = EditorGUILayout.Slider("Button Diameter", buttonDiameter, 0.02f, 0.05f);
        tablePadding = EditorGUILayout.Slider("Table Padding", tablePadding, 0.02f, 0.08f);
        tableHeight = EditorGUILayout.Slider("Table Height", tableHeight, 0.60f, 0.90f);

        // Show computed table size
        float tableW = (gridCols - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;
        float tableD = (gridRows - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;
        EditorGUILayout.HelpBox(
            $"Computed table surface: {tableW * 100:F1}cm x {tableD * 100:F1}cm\n" +
            $"Total buttons: {gridRows * gridCols}\n" +
            $"Fits within Zone 1 reach: {(Mathf.Max(tableW, tableD) <= 0.355f ? "YES" : "WARNING — exceeds 35.5cm")}",
            Mathf.Max(tableW, tableD) <= 0.355f ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space(5);
        createXROrigin = EditorGUILayout.Toggle("Create XR Origin (simulated)", createXROrigin);

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("BUILD FULL SCENE", GUILayout.Height(45)))
        {
            BuildScene();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "After building:\n" +
            "1. Press Play to test\n" +
            "2. Press G — place table grid at fingertip\n" +
            "3. Press 1 — confirm placement (A button)\n" +
            "4. Press 2 — cancel and retry (B button)\n" +
            "5. Countdown → Game starts!",
            MessageType.Warning);
    }

    private void BuildScene()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tactilis Full Scene Setup");

        // --- 1. XR Origin / Camera ---
        GameObject cameraRig;
        Transform leftFingerTip;
        Transform rightFingerTip;
        Camera mainCam;

        if (createXROrigin)
        {
            cameraRig = CreateSimulatedXROrigin(out leftFingerTip, out rightFingerTip, out mainCam);
        }
        else
        {
            // Use existing main camera
            mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.transform.position = new Vector3(0, 1.6f, 0);
                Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
            }

            // Create finger tip proxies as children of camera
            GameObject leftHand = new GameObject("LeftHand_Simulated");
            leftHand.transform.SetParent(mainCam.transform);
            leftHand.transform.localPosition = new Vector3(-0.15f, -0.2f, 0.3f);
            Undo.RegisterCreatedObjectUndo(leftHand, "Create Left Hand");

            GameObject leftTip = new GameObject("IndexFingerTip");
            leftTip.transform.SetParent(leftHand.transform);
            leftTip.transform.localPosition = Vector3.zero;
            leftFingerTip = leftTip.transform;

            FingerColorIndicator blueIndicator = leftTip.AddComponent<FingerColorIndicator>();
            blueIndicator.fingerColor = FingerColorIndicator.FingerColorType.Blue;

            GameObject rightHand = new GameObject("RightHand_Simulated");
            rightHand.transform.SetParent(mainCam.transform);
            rightHand.transform.localPosition = new Vector3(0.15f, -0.2f, 0.3f);
            Undo.RegisterCreatedObjectUndo(rightHand, "Create Right Hand");

            GameObject rightTip = new GameObject("IndexFingerTip");
            rightTip.transform.SetParent(rightHand.transform);
            rightTip.transform.localPosition = Vector3.zero;
            rightFingerTip = rightTip.transform;

            FingerColorIndicator redIndicator = rightTip.AddComponent<FingerColorIndicator>();
            redIndicator.fingerColor = FingerColorIndicator.FingerColorType.Red;

            cameraRig = mainCam.gameObject;
        }

        // --- 2. Button Grid ---
        GameObject buttonGrid = CreateButtonGrid(out WhackAMoleButton[] buttons);

        // --- 3. AR UI Canvas ---
        GameObject arUIObj = CreateARGameUI(out ARGameUI arUI);

        // --- 4. Managers ---
        GameObject managersRoot = new GameObject("--- Managers ---");
        Undo.RegisterCreatedObjectUndo(managersRoot, "Create Managers");

        // XRInputWatcher
        GameObject inputWatcherObj = new GameObject("XRInputWatcher");
        inputWatcherObj.transform.SetParent(managersRoot.transform);
        XRInputWatcher inputWatcher = inputWatcherObj.AddComponent<XRInputWatcher>();
        Undo.RegisterCreatedObjectUndo(inputWatcherObj, "Create XRInputWatcher");

        // CalibrationManager
        GameObject calibObj = new GameObject("CalibrationManager");
        calibObj.transform.SetParent(managersRoot.transform);
        CalibrationManager calibManager = calibObj.AddComponent<CalibrationManager>();
        calibManager.buttonGrid = buttonGrid;
        calibManager.indexFingerTip = leftFingerTip;
        calibManager.arUI = arUI;
        calibManager.inputWatcher = inputWatcher;
        Undo.RegisterCreatedObjectUndo(calibObj, "Create CalibrationManager");

        // WhackAMoleGameManager
        GameObject gameManagerObj = new GameObject("WhackAMoleGameManager");
        gameManagerObj.transform.SetParent(managersRoot.transform);
        WhackAMoleGameManager gameManager = gameManagerObj.AddComponent<WhackAMoleGameManager>();
        gameManager.buttons = buttons;
        gameManager.blueFingerTip = leftFingerTip;
        gameManager.redFingerTip = rightFingerTip;
        gameManager.arUI = arUI;
        AudioSource gameAudio = gameManagerObj.AddComponent<AudioSource>();
        gameManager.audioSource = gameAudio;
        Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create WhackAMoleGameManager");

        // GameController
        GameObject controllerObj = new GameObject("GameController");
        controllerObj.transform.SetParent(managersRoot.transform);
        GameController gameController = controllerObj.AddComponent<GameController>();
        gameController.calibrationManager = calibManager;
        gameController.gameManager = gameManager;
        gameController.arUI = arUI;
        AudioSource controllerAudio = controllerObj.AddComponent<AudioSource>();
        gameController.audioSource = controllerAudio;
        Undo.RegisterCreatedObjectUndo(controllerObj, "Create GameController");

        // --- 5. Select the managers root ---
        Selection.activeGameObject = managersRoot;

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "[Tactilis] Scene setup complete!\n" +
            $"  - Button Grid: {gridRows}x{gridCols} ({buttons.Length} buttons)\n" +
            $"  - Left finger tip: {leftFingerTip.name}\n" +
            $"  - Right finger tip: {rightFingerTip.name}\n" +
            "  - All managers wired up\n" +
            "  Press Play to test!"
        );

        EditorUtility.DisplayDialog(
            "Tactilis Scene Ready",
            $"Scene built successfully!\n\n" +
            $"• {buttons.Length} buttons in a {gridRows}x{gridCols} grid\n" +
            $"• All managers wired up\n" +
            $"• AR UI canvas created\n\n" +
            "Press Play to test.\n" +
            "Use keyboard to simulate XR input:\n" +
            "  G = Grip (place grid)\n" +
            "  1 = A button (confirm)\n" +
            "  2 = B button (cancel)",
            "Got it!"
        );
    }

    private GameObject CreateSimulatedXROrigin(out Transform leftFingerTip, out Transform rightFingerTip, out Camera mainCam)
    {
        // Remove existing main camera if present
        Camera existingCam = Camera.main;
        if (existingCam != null)
        {
            Undo.DestroyObjectImmediate(existingCam.gameObject);
        }

        // XR Origin root
        GameObject xrOrigin = new GameObject("XR Origin (Simulated)");
        xrOrigin.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(xrOrigin, "Create XR Origin");

        // Camera Offset
        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOrigin.transform);
        cameraOffset.transform.localPosition = new Vector3(0, 1.6f, 0);

        // Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(cameraOffset.transform);
        camObj.transform.localPosition = Vector3.zero;
        mainCam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        // Left Hand
        GameObject leftHand = new GameObject("Left Hand");
        leftHand.transform.SetParent(cameraOffset.transform);
        leftHand.transform.localPosition = new Vector3(-0.15f, -0.2f, 0.35f);

        GameObject leftTip = new GameObject("IndexFingerTip");
        leftTip.transform.SetParent(leftHand.transform);
        leftTip.transform.localPosition = new Vector3(0, 0, 0.05f);
        leftFingerTip = leftTip.transform;

        FingerColorIndicator blueIndicator = leftTip.AddComponent<FingerColorIndicator>();
        blueIndicator.fingerColor = FingerColorIndicator.FingerColorType.Blue;

        // Right Hand
        GameObject rightHand = new GameObject("Right Hand");
        rightHand.transform.SetParent(cameraOffset.transform);
        rightHand.transform.localPosition = new Vector3(0.15f, -0.2f, 0.35f);

        GameObject rightTip = new GameObject("IndexFingerTip");
        rightTip.transform.SetParent(rightHand.transform);
        rightTip.transform.localPosition = new Vector3(0, 0, 0.05f);
        rightFingerTip = rightTip.transform;

        FingerColorIndicator redIndicator = rightTip.AddComponent<FingerColorIndicator>();
        redIndicator.fingerColor = FingerColorIndicator.FingerColorType.Red;

        return xrOrigin;
    }

    private GameObject CreateButtonGrid(out WhackAMoleButton[] buttons)
    {
        // --- Root object (origin = center of table surface) ---
        GameObject gridRoot = new GameObject("Table Button Grid");
        gridRoot.transform.position = new Vector3(0, tableHeight, 0.4f);
        Undo.RegisterCreatedObjectUndo(gridRoot, "Create Table Button Grid");

        // --- Compute real-world table dimensions ---
        float tableW = (gridCols - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;
        float tableD = (gridRows - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;

        // --- Table surface (flat cube, lies in XZ plane) ---
        GameObject tableSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableSurface.name = "TableSurface";
        tableSurface.transform.SetParent(gridRoot.transform);
        tableSurface.transform.localPosition = new Vector3(0, -tableThickness / 2f, 0);
        tableSurface.transform.localScale = new Vector3(tableW, tableThickness, tableD);

        // Semi-transparent table material (visible in AR, shows real table underneath)
        Renderer tableRenderer = tableSurface.GetComponent<Renderer>();
        Material tableMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        tableMat.SetFloat("_Surface", 1); // Transparent
        tableMat.SetFloat("_Blend", 0);
        tableMat.SetOverrideTag("RenderType", "Transparent");
        tableMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        tableMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        tableMat.SetInt("_ZWrite", 0);
        tableMat.renderQueue = 3000;
        tableMat.color = new Color(0.3f, 0.6f, 0.9f, 0.15f); // Subtle blue tint
        tableRenderer.material = tableMat;

        // Remove table collider (buttons have their own)
        Object.DestroyImmediate(tableSurface.GetComponent<Collider>());

        // --- Edge outline (thin border around table) ---
        GameObject tableBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableBorder.name = "TableBorder";
        tableBorder.transform.SetParent(gridRoot.transform);
        tableBorder.transform.localPosition = new Vector3(0, 0.001f, 0);
        float borderThick = 0.002f;
        tableBorder.transform.localScale = new Vector3(tableW + borderThick, 0.001f, tableD + borderThick);

        Renderer borderRenderer = tableBorder.GetComponent<Renderer>();
        Material borderMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        borderMat.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        borderMat.SetFloat("_Surface", 1);
        borderMat.SetOverrideTag("RenderType", "Transparent");
        borderMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        borderMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        borderMat.SetInt("_ZWrite", 0);
        borderMat.renderQueue = 3001;
        borderRenderer.material = borderMat;
        Object.DestroyImmediate(tableBorder.GetComponent<Collider>());

        // --- Buttons (flat cylinders sitting ON the table surface) ---
        buttons = new WhackAMoleButton[gridRows * gridCols];
        int index = 0;

        float gridW = (gridCols - 1) * buttonSpacing;
        float gridD = (gridRows - 1) * buttonSpacing;
        float startX = -gridW / 2f;
        float startZ = -gridD / 2f;

        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                // Cylinder: default is 2m tall, 1m diameter
                // Scale Y for height, XZ for diameter
                GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                btnObj.name = $"Button_R{row}_C{col}";
                btnObj.transform.SetParent(gridRoot.transform);

                float x = startX + col * buttonSpacing;
                float z = startZ + row * buttonSpacing;
                // Sit on table: cylinder center is at half its scaled height
                float btnScaleY = buttonHeight / 2f; // Cylinder is 2 units tall by default
                btnObj.transform.localPosition = new Vector3(x, buttonHeight / 2f, z);
                btnObj.transform.localScale = new Vector3(buttonDiameter, btnScaleY, buttonDiameter);

                WhackAMoleButton btn = btnObj.AddComponent<WhackAMoleButton>();
                btn.buttonRenderer = btnObj.GetComponent<Renderer>();

                // Add a point light for glow effect (disabled by default)
                GameObject lightObj = new GameObject("ButtonLight");
                lightObj.transform.SetParent(btnObj.transform);
                lightObj.transform.localPosition = new Vector3(0, 2f, 0); // Above button (in local scaled space)
                Light btnLight = lightObj.AddComponent<Light>();
                btnLight.type = LightType.Point;
                btnLight.range = 0.1f;
                btnLight.intensity = 0.5f;
                btnLight.enabled = false;
                btn.buttonLight = btnLight;

                buttons[index] = btn;
                index++;
            }
        }

        // Start hidden — CalibrationManager will show it after calibration
        gridRoot.SetActive(false);

        Debug.Log(
            $"[Tactilis] Table grid created:\n" +
            $"  Table: {tableW * 100:F1}cm x {tableD * 100:F1}cm at {tableHeight}m height\n" +
            $"  Buttons: {gridRows}x{gridCols}, {buttonDiameter * 100:F1}cm diameter, {buttonSpacing * 100:F1}cm apart\n" +
            $"  Button height above table: {buttonHeight * 1000:F1}mm"
        );

        return gridRoot;
    }

    private GameObject CreateARGameUI(out ARGameUI arUI)
    {
        GameObject uiRoot = new GameObject("AR Game UI");
        Undo.RegisterCreatedObjectUndo(uiRoot, "Create AR Game UI");

        // World-space canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(uiRoot.transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(800, 600);
        canvasRT.localScale = Vector3.one * 0.001f;
        canvasRT.position = new Vector3(0, 2f, 2f);

        // --- Rules Panel ---
        GameObject rulesPanel = new GameObject("RulesPanel");
        rulesPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform rulesPanelRT = rulesPanel.AddComponent<RectTransform>();
        rulesPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        rulesPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        rulesPanelRT.pivot = new Vector2(0.5f, 0.5f);
        rulesPanelRT.sizeDelta = new Vector2(600, 400);
        rulesPanelRT.anchoredPosition = Vector2.zero;

        Image rulesBg = rulesPanel.AddComponent<Image>();
        rulesBg.color = new Color(0, 0, 0, 0.85f);

        GameObject rulesTextObj = new GameObject("RulesText");
        rulesTextObj.transform.SetParent(rulesPanel.transform, false);

        RectTransform rulesTextRT = rulesTextObj.AddComponent<RectTransform>();
        rulesTextRT.anchorMin = Vector2.zero;
        rulesTextRT.anchorMax = Vector2.one;
        rulesTextRT.sizeDelta = new Vector2(-40, -30);
        rulesTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI rulesText = rulesTextObj.AddComponent<TextMeshProUGUI>();
        rulesText.text = "Waiting for setup...";
        rulesText.fontSize = 28;
        rulesText.alignment = TextAlignmentOptions.Center;
        rulesText.color = Color.white;
        rulesText.textWrappingMode = TextWrappingModes.Normal;

        // --- Score Panel ---
        GameObject scorePanel = new GameObject("ScorePanel");
        scorePanel.transform.SetParent(canvasObj.transform, false);

        RectTransform scorePanelRT = scorePanel.AddComponent<RectTransform>();
        scorePanelRT.anchorMin = new Vector2(1, 1);
        scorePanelRT.anchorMax = new Vector2(1, 1);
        scorePanelRT.pivot = new Vector2(1, 1);
        scorePanelRT.sizeDelta = new Vector2(200, 60);
        scorePanelRT.anchoredPosition = new Vector2(-20, -20);

        Image scoreBg = scorePanel.AddComponent<Image>();
        scoreBg.color = new Color(0, 0, 0, 0.7f);

        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(scorePanel.transform, false);

        RectTransform scoreTextRT = scoreTextObj.AddComponent<RectTransform>();
        scoreTextRT.anchorMin = Vector2.zero;
        scoreTextRT.anchorMax = Vector2.one;
        scoreTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 32;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = Color.white;

        // --- Timer Panel ---
        GameObject timerPanel = new GameObject("TimerPanel");
        timerPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform timerPanelRT = timerPanel.AddComponent<RectTransform>();
        timerPanelRT.anchorMin = new Vector2(0, 1);
        timerPanelRT.anchorMax = new Vector2(0, 1);
        timerPanelRT.pivot = new Vector2(0, 1);
        timerPanelRT.sizeDelta = new Vector2(200, 60);
        timerPanelRT.anchoredPosition = new Vector2(20, -20);

        Image timerBg = timerPanel.AddComponent<Image>();
        timerBg.color = new Color(0, 0, 0, 0.7f);

        GameObject timerTextObj = new GameObject("TimerText");
        timerTextObj.transform.SetParent(timerPanel.transform, false);

        RectTransform timerTextRT = timerTextObj.AddComponent<RectTransform>();
        timerTextRT.anchorMin = Vector2.zero;
        timerTextRT.anchorMax = Vector2.one;
        timerTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI timerText = timerTextObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "Time: 30";
        timerText.fontSize = 32;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;

        // --- Game Over Panel ---
        GameObject gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform gameOverPanelRT = gameOverPanel.AddComponent<RectTransform>();
        gameOverPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverPanelRT.pivot = new Vector2(0.5f, 0.5f);
        gameOverPanelRT.sizeDelta = new Vector2(500, 300);
        gameOverPanelRT.anchoredPosition = Vector2.zero;

        Image gameOverBg = gameOverPanel.AddComponent<Image>();
        gameOverBg.color = new Color(0, 0, 0, 0.9f);

        GameObject gameOverTextObj = new GameObject("GameOverText");
        gameOverTextObj.transform.SetParent(gameOverPanel.transform, false);

        RectTransform gameOverTextRT = gameOverTextObj.AddComponent<RectTransform>();
        gameOverTextRT.anchorMin = Vector2.zero;
        gameOverTextRT.anchorMax = Vector2.one;
        gameOverTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI gameOverText = gameOverTextObj.AddComponent<TextMeshProUGUI>();
        gameOverText.text = "Game Over!";
        gameOverText.fontSize = 48;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.white;

        gameOverPanel.SetActive(false);

        // --- Wire up ARGameUI component ---
        arUI = uiRoot.AddComponent<ARGameUI>();
        arUI.uiCanvas = canvas;
        arUI.rulesPanel = rulesPanelRT;
        arUI.rulesText = rulesText;
        arUI.rulesPanelBackground = rulesBg;
        arUI.scorePanel = scorePanelRT;
        arUI.scoreText = scoreText;
        arUI.scorePanelBackground = scoreBg;
        arUI.timerPanel = timerPanelRT;
        arUI.timerText = timerText;
        arUI.timerPanelBackground = timerBg;
        arUI.gameOverPanel = gameOverPanelRT;
        arUI.gameOverText = gameOverText;

        return uiRoot;
    }
}
#endif
