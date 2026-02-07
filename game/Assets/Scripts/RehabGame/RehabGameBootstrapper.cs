using UnityEngine;

/// <summary>
/// Bootstrapper that sets up all game components and wires them together.
/// Add this to an empty GameObject in your scene to auto-configure the rehab game.
/// </summary>
public class RehabGameBootstrapper : MonoBehaviour
{
    [Header("Auto-Create Components")]
    public bool autoCreateMissingComponents = true;

    [Header("Grid Settings")]
    public int gridRows = 2;
    public int gridColumns = 3;
    public float cellSize = 0.08f; // 8cm cells
    public float cellSpacing = 0.01f;
    public Color cellInactiveColor = new Color(0.2f, 0.2f, 0.3f, 0.7f);
    public Color cellActiveColor = new Color(0.1f, 0.9f, 0.3f, 1f);

    [Header("Game Settings")]
    public float gameDuration = 30f;
    public float targetLifetime = 3f;

    [Header("Serial Port (Arduino)")]
    public string serialPortName = "COM3"; // Change to your port
    public bool simulateInEditor = true;

    [Header("Created References (Auto-populated)")]
    public RehabGameManager gameManager;
    public RehabGrid grid;
    public FRSSerialManager frsManager;
    public HandTrackingPressDetector pressDetector;
    public RehabGameUI gameUI;

    void Awake()
    {
        if (autoCreateMissingComponents)
        {
            SetupComponents();
        }
    }

    void SetupComponents()
    {
        // 1. FRS Serial Manager
        frsManager = FindFirstObjectByType<FRSSerialManager>();
        if (!frsManager)
        {
            GameObject frsObj = new GameObject("FRSSerialManager");
            frsObj.transform.SetParent(transform);
            frsManager = frsObj.AddComponent<FRSSerialManager>();
            frsManager.portName = serialPortName;
            frsManager.simulateInEditor = simulateInEditor;
        }

        // 2. Grid (will be placed by ARTablePlacer in MR, or manually positioned)
        grid = FindFirstObjectByType<RehabGrid>();
        if (!grid)
        {
            GameObject gridObj = new GameObject("RehabGrid");
            gridObj.transform.SetParent(transform);
            gridObj.transform.localPosition = new Vector3(0, 0.8f, 0.5f); // Default: 80cm high, 50cm in front
            grid = gridObj.AddComponent<RehabGrid>();
            grid.rows = gridRows;
            grid.columns = gridColumns;
            grid.cellSize = cellSize;
            grid.cellSpacing = cellSpacing;
        }

        // 3. Press Detector
        pressDetector = FindFirstObjectByType<HandTrackingPressDetector>();
        if (!pressDetector)
        {
            GameObject detectorObj = new GameObject("HandTrackingPressDetector");
            detectorObj.transform.SetParent(transform);
            pressDetector = detectorObj.AddComponent<HandTrackingPressDetector>();
            pressDetector.grid = grid;
            pressDetector.frsManager = frsManager;
        }

        // 4. Game Manager
        gameManager = FindFirstObjectByType<RehabGameManager>();
        if (!gameManager)
        {
            GameObject managerObj = new GameObject("RehabGameManager");
            managerObj.transform.SetParent(transform);
            gameManager = managerObj.AddComponent<RehabGameManager>();
            gameManager.grid = grid;
            gameManager.pressDetector = pressDetector;
            gameManager.gameDuration = gameDuration;
            gameManager.targetLifetime = targetLifetime;
        }

        // 5. Game UI (needs Canvas - create basic one if missing)
        gameUI = FindFirstObjectByType<RehabGameUI>();
        if (!gameUI)
        {
            CreateBasicUI();
        }

        // Wire up references
        if (gameManager)
        {
            gameManager.grid = grid;
            gameManager.pressDetector = pressDetector;
            gameManager.gameUI = gameUI;
        }

        if (pressDetector)
        {
            pressDetector.grid = grid;
            pressDetector.frsManager = frsManager;
        }

        Debug.Log("[RehabGameBootstrapper] Game components initialized");
    }

    void CreateBasicUI()
    {
        // Create World Space Canvas for VR/AR
        GameObject canvasObj = new GameObject("RehabGameCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Position canvas above the grid
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 300);
        canvasRect.localScale = Vector3.one * 0.001f; // Scale down for world space
        canvasRect.localPosition = new Vector3(0, 1.2f, 0.5f); // Above grid

        // Add UI component
        gameUI = canvasObj.AddComponent<RehabGameUI>();

        // Create basic UI elements
        CreateTimerUI(canvasObj.transform);
        CreateScoreUI(canvasObj.transform);
        CreateCountdownUI(canvasObj.transform);
        CreateResultsUI(canvasObj.transform);
        CreateStartUI(canvasObj.transform);
    }

    void CreateTimerUI(Transform parent)
    {
        GameObject timerPanel = CreatePanel("TimerPanel", parent, new Vector2(0, 100), new Vector2(200, 60));
        gameUI.timerPanel = timerPanel;

        // Timer text
        GameObject timerTextObj = CreateTextElement("TimerText", timerPanel.transform, "0:30", 36);
        gameUI.timerText = timerTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        // Timer fill (simplified - just background)
        UnityEngine.UI.Image bg = timerPanel.GetComponent<UnityEngine.UI.Image>();
        if (bg)
        {
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            gameUI.timerFillImage = bg;
        }
    }

    void CreateScoreUI(Transform parent)
    {
        GameObject scorePanel = CreatePanel("ScorePanel", parent, new Vector2(0, 40), new Vector2(200, 50));
        gameUI.scorePanel = scorePanel;

        GameObject scoreText = CreateTextElement("ScoreText", scorePanel.transform, "0", 32);
        gameUI.scoreText = scoreText.GetComponent<TMPro.TextMeshProUGUI>();
    }

    void CreateCountdownUI(Transform parent)
    {
        GameObject countdownPanel = CreatePanel("CountdownPanel", parent, new Vector2(0, 0), new Vector2(150, 150));
        countdownPanel.SetActive(false);
        gameUI.countdownPanel = countdownPanel;

        GameObject countdownText = CreateTextElement("CountdownText", countdownPanel.transform, "3", 72);
        gameUI.countdownText = countdownText.GetComponent<TMPro.TextMeshProUGUI>();
    }

    void CreateResultsUI(Transform parent)
    {
        GameObject resultsPanel = CreatePanel("ResultsPanel", parent, new Vector2(0, 0), new Vector2(300, 250));
        resultsPanel.SetActive(false);
        gameUI.resultsPanel = resultsPanel;

        // Title
        GameObject titleText = CreateTextElement("TitleText", resultsPanel.transform, "GREAT JOB!", 36, new Vector2(0, 90));
        gameUI.resultsTitleText = titleText.GetComponent<TMPro.TextMeshProUGUI>();

        // Score
        GameObject scoreText = CreateTextElement("ScoreText", resultsPanel.transform, "Score: 0", 28, new Vector2(0, 50));
        gameUI.resultsScoreText = scoreText.GetComponent<TMPro.TextMeshProUGUI>();

        // Hits
        GameObject hitsText = CreateTextElement("HitsText", resultsPanel.transform, "Hits: 0", 24, new Vector2(0, 20));
        gameUI.resultsHitsText = hitsText.GetComponent<TMPro.TextMeshProUGUI>();

        // Accuracy
        GameObject accuracyText = CreateTextElement("AccuracyText", resultsPanel.transform, "Accuracy: 0%", 24, new Vector2(0, -10));
        gameUI.resultsAccuracyText = accuracyText.GetComponent<TMPro.TextMeshProUGUI>();

        // Reaction
        GameObject reactionText = CreateTextElement("ReactionText", resultsPanel.transform, "Avg: 0.00s", 20, new Vector2(0, -40));
        gameUI.resultsReactionText = reactionText.GetComponent<TMPro.TextMeshProUGUI>();

        // Play Again Button
        GameObject playAgainBtn = CreateButton("PlayAgainButton", resultsPanel.transform, "Play Again", new Vector2(0, -90));
        gameUI.playAgainButton = playAgainBtn.GetComponent<UnityEngine.UI.Button>();
    }

    void CreateStartUI(Transform parent)
    {
        GameObject startPanel = CreatePanel("StartPanel", parent, new Vector2(0, 0), new Vector2(300, 200));
        gameUI.startPanel = startPanel;

        // Instructions
        GameObject instructionsText = CreateTextElement("InstructionsText", startPanel.transform, 
            "Press the lit buttons\nas fast as you can!\n\n30 seconds", 24, new Vector2(0, 30));
        gameUI.instructionsText = instructionsText.GetComponent<TMPro.TextMeshProUGUI>();

        // Start Button
        GameObject startBtn = CreateButton("StartButton", startPanel.transform, "START", new Vector2(0, -60));
        gameUI.startButton = startBtn.GetComponent<UnityEngine.UI.Button>();
    }

    GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        return panel;
    }

    GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, Vector2 position = default)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(280, 50);

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return textObj;
    }

    GameObject CreateButton(string name, Transform parent, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(150, 50);

        UnityEngine.UI.Image image = btnObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.2f, 0.6f, 0.3f, 1f);

        UnityEngine.UI.Button button = btnObj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;

        // Button text
        GameObject textObj = CreateTextElement("Text", btnObj.transform, text, 24);

        return btnObj;
    }

    [ContextMenu("Test Start Game")]
    public void TestStartGame()
    {
        if (gameManager)
            gameManager.StartGame();
    }
}
