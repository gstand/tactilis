#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Tactilis.Editor
{
    /// <summary>
    /// TactilisSceneBuilder - One-click scene setup for the complete Tactilis AR game.
    /// Creates all necessary GameObjects, components, and wiring for Meta Quest 3.
    /// Menu: Tactilis > Build Complete Scene
    /// </summary>
    public class TactilisSceneBuilder : EditorWindow
    {
        #region Settings
        private int gridRows = 3;
        private int gridCols = 3;
        private float buttonSpacing = 0.045f;
        private float buttonDiameter = 0.03f;
        private float tableHeight = 0.75f;
        private float tablePadding = 0.04f;
        #endregion

        [MenuItem("Tactilis/Build Complete Scene")]
        public static void ShowWindow()
        {
            GetWindow<TactilisSceneBuilder>("Tactilis Scene Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tactilis AR Rehab Game", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Builds a complete Meta Quest 3 hand-tracking game:\n\n" +
                "• TactilisGame - Unified game controller\n" +
                "• TactilisHandTracker - XR Hands finger tracking\n" +
                "• TactilisTableCalibration - AR plane detection\n" +
                "• TactilisUI - World-space HUD\n" +
                "• Button Grid - Ergonomic table layout\n\n" +
                "Run on a fresh scene for best results.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            GUILayout.Label("Grid Layout", EditorStyles.boldLabel);
            gridRows = EditorGUILayout.IntSlider("Rows", gridRows, 2, 5);
            gridCols = EditorGUILayout.IntSlider("Columns", gridCols, 2, 5);

            EditorGUILayout.Space(5);
            GUILayout.Label("Ergonomic Dimensions (meters)", EditorStyles.boldLabel);
            buttonSpacing = EditorGUILayout.Slider("Button Spacing", buttonSpacing, 0.03f, 0.08f);
            buttonDiameter = EditorGUILayout.Slider("Button Diameter", buttonDiameter, 0.02f, 0.05f);
            tableHeight = EditorGUILayout.Slider("Table Height", tableHeight, 0.60f, 0.90f);

            // Computed dimensions
            float tableW = (gridCols - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;
            float tableD = (gridRows - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;
            EditorGUILayout.HelpBox(
                $"Table: {tableW * 100:F1}cm x {tableD * 100:F1}cm\n" +
                $"Buttons: {gridRows * gridCols}",
                MessageType.None);

            EditorGUILayout.Space(15);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("BUILD COMPLETE SCENE", GUILayout.Height(45)))
            {
                BuildScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Editor Testing:\n" +
                "• P = Pinch gesture (place/confirm)\n" +
                "• 1-9 = Tap buttons\n" +
                "• R = Restart (when game over)",
                MessageType.Warning);
        }

        private void BuildScene()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Tactilis Complete Scene");

            // 1. Camera
            Camera mainCam = CreateCamera();

            // 2. Button Grid
            GameObject buttonGrid = CreateButtonGrid(out TactilisButton[] buttons);

            // 3. UI
            GameObject uiObj = CreateUI(out TactilisUI ui);

            // 4. Hand Tracker
            GameObject handTrackerObj = CreateHandTracker(out TactilisHandTracker handTracker);

            // 5. Table Calibration
            GameObject tableCalibObj = CreateTableCalibration(out TactilisTableCalibration tableCalib);

            // 6. Game Controller (main)
            GameObject gameObj = CreateGameController(
                ui, handTracker, tableCalib, buttonGrid.transform, buttons
            );

            // 7. Lighting
            CreateLighting();

            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = gameObj;

            Debug.Log(
                "[Tactilis] Scene built successfully!\n" +
                $"  • {buttons.Length} buttons ({gridRows}x{gridCols})\n" +
                $"  • Table: {tableHeight}m height\n" +
                "  Press Play to test!"
            );

            EditorUtility.DisplayDialog(
                "Tactilis Scene Ready",
                $"Scene built!\n\n" +
                $"• {buttons.Length} buttons\n" +
                $"• Hand tracking ready\n" +
                $"• AR table calibration ready\n\n" +
                "Press Play to test.\n\n" +
                "Editor controls:\n" +
                "  P = Pinch\n" +
                "  1-9 = Tap buttons\n" +
                "  R = Restart",
                "Got it!"
            );
        }

        #region Create Methods
        private Camera CreateCamera()
        {
            // Remove existing
            Camera existing = Camera.main;
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 1.6f, -0.5f);
            camObj.transform.rotation = Quaternion.Euler(30f, 0, 0);

            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.01f;

            camObj.AddComponent<AudioListener>();

            Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
            return cam;
        }

        private GameObject CreateButtonGrid(out TactilisButton[] buttons)
        {
            GameObject gridRoot = new GameObject("Button Grid");
            gridRoot.transform.position = new Vector3(0, tableHeight, 0.4f);
            Undo.RegisterCreatedObjectUndo(gridRoot, "Create Button Grid");

            // Table surface dimensions
            float tableW = (gridCols - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;
            float tableD = (gridRows - 1) * buttonSpacing + buttonDiameter + tablePadding * 2;

            // Table surface
            GameObject tableSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableSurface.name = "TableSurface";
            tableSurface.transform.SetParent(gridRoot.transform);
            tableSurface.transform.localPosition = new Vector3(0, -0.0025f, 0);
            tableSurface.transform.localScale = new Vector3(tableW, 0.005f, tableD);

            Renderer tableRend = tableSurface.GetComponent<Renderer>();
            Material tableMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            tableMat.color = new Color(0.3f, 0.6f, 0.9f, 0.2f);
            SetupTransparentMaterial(tableMat);
            tableRend.material = tableMat;

            Object.DestroyImmediate(tableSurface.GetComponent<Collider>());

            // Buttons
            buttons = new TactilisButton[gridRows * gridCols];
            int index = 0;

            float gridW = (gridCols - 1) * buttonSpacing;
            float gridD = (gridRows - 1) * buttonSpacing;
            float startX = -gridW / 2f;
            float startZ = -gridD / 2f;

            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    btnObj.name = $"Button_R{row}_C{col}";
                    btnObj.transform.SetParent(gridRoot.transform);

                    float x = startX + col * buttonSpacing;
                    float z = startZ + row * buttonSpacing;
                    float btnHeight = 0.003f;

                    btnObj.transform.localPosition = new Vector3(x, btnHeight / 2f, z);
                    btnObj.transform.localScale = new Vector3(buttonDiameter, btnHeight / 2f, buttonDiameter);

                    // Add trigger collider
                    CapsuleCollider col3d = btnObj.GetComponent<CapsuleCollider>();
                    if (col3d != null) col3d.isTrigger = true;

                    TactilisButton btn = btnObj.AddComponent<TactilisButton>();
                    btn.buttonRenderer = btnObj.GetComponent<Renderer>();

                    // Add light
                    GameObject lightObj = new GameObject("Light");
                    lightObj.transform.SetParent(btnObj.transform);
                    lightObj.transform.localPosition = new Vector3(0, 1f, 0);
                    Light btnLight = lightObj.AddComponent<Light>();
                    btnLight.type = LightType.Point;
                    btnLight.range = 0.08f;
                    btnLight.intensity = 0.5f;
                    btnLight.enabled = false;
                    btn.buttonLight = btnLight;

                    buttons[index] = btn;
                    index++;
                }
            }

            gridRoot.SetActive(false);
            return gridRoot;
        }

        private GameObject CreateUI(out TactilisUI ui)
        {
            GameObject uiRoot = new GameObject("Tactilis UI");
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create UI");

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(uiRoot.transform);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(800, 600);
            canvasRT.localScale = Vector3.one * 0.002f;
            canvasRT.position = new Vector3(0, 1.8f, 1.5f);

            // HUD Panel
            GameObject hudPanel = CreatePanel(canvasObj.transform, "HUDPanel", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(400, 80), new Vector2(0, -20));

            // Timer
            GameObject timerObj = CreateTextElement(hudPanel.transform, "TimerText", "Time: 30",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(150, 50), new Vector2(80, 0));
            TextMeshProUGUI timerText = timerObj.GetComponent<TextMeshProUGUI>();

            // Score
            GameObject scoreObj = CreateTextElement(hudPanel.transform, "ScoreText", "Score: 0",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(150, 50), new Vector2(-80, 0));
            TextMeshProUGUI scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

            // Message Panel
            GameObject messagePanel = CreatePanel(canvasObj.transform, "MessagePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600, 400), Vector2.zero);

            GameObject messageTextObj = CreateTextElement(messagePanel.transform, "MessageText", "Welcome",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 360), Vector2.zero);
            TextMeshProUGUI messageText = messageTextObj.GetComponent<TextMeshProUGUI>();
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 28;

            // Game Over Panel
            GameObject gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500, 350), Vector2.zero);
            gameOverPanel.SetActive(false);

            GameObject goTitleObj = CreateTextElement(gameOverPanel.transform, "TitleText", "GAME OVER",
                new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(400, 80), Vector2.zero);
            TextMeshProUGUI goTitleText = goTitleObj.GetComponent<TextMeshProUGUI>();
            goTitleText.fontSize = 48;
            goTitleText.alignment = TextAlignmentOptions.Center;

            GameObject goScoreObj = CreateTextElement(gameOverPanel.transform, "FinalScoreText", "Final Score: 0",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400, 60), Vector2.zero);
            TextMeshProUGUI goScoreText = goScoreObj.GetComponent<TextMeshProUGUI>();
            goScoreText.fontSize = 36;
            goScoreText.alignment = TextAlignmentOptions.Center;

            GameObject goHintObj = CreateTextElement(gameOverPanel.transform, "RestartHint", "PINCH to play again",
                new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(400, 40), Vector2.zero);
            TextMeshProUGUI goHintText = goHintObj.GetComponent<TextMeshProUGUI>();
            goHintText.fontSize = 24;
            goHintText.alignment = TextAlignmentOptions.Center;
            goHintText.color = new Color(0.5f, 1f, 0.5f);

            // Wire up TactilisUI
            ui = uiRoot.AddComponent<TactilisUI>();
            ui.uiCanvas = canvas;
            ui.hudPanel = hudPanel.GetComponent<RectTransform>();
            ui.timerText = timerText;
            ui.scoreText = scoreText;
            ui.messagePanel = messagePanel.GetComponent<RectTransform>();
            ui.messageText = messageText;
            ui.gameOverPanel = gameOverPanel.GetComponent<RectTransform>();
            ui.gameOverTitleText = goTitleText;
            ui.finalScoreText = goScoreText;
            ui.restartHintText = goHintText;

            return uiRoot;
        }

        private GameObject CreateHandTracker(out TactilisHandTracker handTracker)
        {
            GameObject obj = new GameObject("Hand Tracker");
            Undo.RegisterCreatedObjectUndo(obj, "Create Hand Tracker");

            handTracker = obj.AddComponent<TactilisHandTracker>();
            return obj;
        }

        private GameObject CreateTableCalibration(out TactilisTableCalibration tableCalib)
        {
            GameObject obj = new GameObject("Table Calibration");
            Undo.RegisterCreatedObjectUndo(obj, "Create Table Calibration");

            tableCalib = obj.AddComponent<TactilisTableCalibration>();
            tableCalib.defaultTableHeight = tableHeight;
            return obj;
        }

        private GameObject CreateGameController(
            TactilisUI ui,
            TactilisHandTracker handTracker,
            TactilisTableCalibration tableCalib,
            Transform buttonGridRoot,
            TactilisButton[] buttons)
        {
            GameObject obj = new GameObject("Tactilis Game");
            Undo.RegisterCreatedObjectUndo(obj, "Create Game Controller");

            TactilisGame game = obj.AddComponent<TactilisGame>();
            game.gameUI = ui;
            game.handTracker = handTracker;
            game.tableCalibration = tableCalib;
            game.buttonGridRoot = buttonGridRoot;
            game.buttons = buttons;
            game.gridRows = gridRows;
            game.gridCols = gridCols;
            game.buttonSpacing = buttonSpacing;
            game.buttonDiameter = buttonDiameter;
            game.defaultTableHeight = tableHeight;

            AudioSource audio = obj.AddComponent<AudioSource>();
            game.audioSource = audio;

            return obj;
        }

        private void CreateLighting()
        {
            // Check for existing directional light
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional) return;
            }

            GameObject lightObj = new GameObject("Directional Light");
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);

            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1f;
            dirLight.color = Color.white;

            Undo.RegisterCreatedObjectUndo(lightObj, "Create Light");
        }
        #endregion

        #region UI Helpers
        private GameObject CreatePanel(Transform parent, string name, 
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            return panel;
        }

        private GameObject CreateTextElement(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 32;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return textObj;
        }

        private void SetupTransparentMaterial(Material mat)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }
        #endregion
    }
}
#endif
