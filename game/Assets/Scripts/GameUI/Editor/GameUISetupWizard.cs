#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace GameUI.Editor
{
    /// <summary>
    /// </summary>
    public class GameUISetupWizard : EditorWindow
    {
        private static readonly Color PrimaryBlue = new Color(0.2f, 0.5f, 0.8f, 1f);
        private static readonly Color LightBlue = new Color(0.4f, 0.7f, 0.95f, 1f);
        private static readonly Color DarkBlue = new Color(0.1f, 0.25f, 0.45f, 1f);
        private static readonly Color PanelBackground = new Color(0.12f, 0.18f, 0.28f, 0.95f);

        [MenuItem("Window/Game UI/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<GameUISetupWizard>("Game UI Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Game UI Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard will create all the necessary UI components for the Timer, Score, and End Session system.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Complete UI Setup", GUILayout.Height(40)))
            {
                CreateCompleteUISetup();
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("Or create individual components:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Timer UI"))
            {
                CreateTimerUI();
            }
            if (GUILayout.Button("Score UI"))
            {
                CreateScoreUI();
            }
            if (GUILayout.Button("End Session UI"))
            {
                CreateEndSessionUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "After creation, assign the UI references in the GameSessionManager component.",
                MessageType.Warning);
        }

        private void CreateCompleteUISetup()
        {
            GameObject gameUIRoot = new GameObject("GameUI");
            Undo.RegisterCreatedObjectUndo(gameUIRoot, "Create Game UI");

            GameSessionManager sessionManager = gameUIRoot.AddComponent<GameSessionManager>();

            Canvas canvas = CreateWorldSpaceCanvas(gameUIRoot.transform);

            GameObject timerObj = CreateTimerUI(canvas.transform);
            GameObject scoreObj = CreateScoreUI(canvas.transform);
            GameObject endSessionObj = CreateEndSessionUI(canvas.transform);

            SerializedObject so = new SerializedObject(sessionManager);
            so.FindProperty("timerUI").objectReferenceValue = timerObj.GetComponent<TimerUI>();
            so.FindProperty("scoreUI").objectReferenceValue = scoreObj.GetComponent<ScoreUI>();
            so.FindProperty("endSessionUI").objectReferenceValue = endSessionObj.GetComponent<EndSessionUI>();
            so.ApplyModifiedProperties();

            Selection.activeGameObject = gameUIRoot;
            Debug.Log("Game UI Setup Complete! Assign to your scene and configure as needed.");
        }

        private Canvas CreateWorldSpaceCanvas(Transform parent)
        {
            GameObject canvasObj = new GameObject("WorldSpaceCanvas");
            canvasObj.transform.SetParent(parent);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 600);
            rt.localScale = Vector3.one * 0.001f;
            rt.localPosition = new Vector3(0, 0, 2f);

            WorldSpaceUIFollower follower = canvasObj.AddComponent<WorldSpaceUIFollower>();

            return canvas;
        }

        private GameObject CreateTimerUI(Transform parent = null)
        {
            GameObject timerRoot = new GameObject("TimerUI");
            if (parent != null) timerRoot.transform.SetParent(parent, false);

            RectTransform timerRT = timerRoot.AddComponent<RectTransform>();
            timerRT.anchorMin = new Vector2(0.5f, 1f);
            timerRT.anchorMax = new Vector2(0.5f, 1f);
            timerRT.pivot = new Vector2(0.5f, 1f);
            timerRT.anchoredPosition = new Vector2(0, -20);
            timerRT.sizeDelta = new Vector2(200, 200);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(timerRoot.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(DarkBlue.r, DarkBlue.g, DarkBlue.b, 0.3f);
            bgImage.type = Image.Type.Filled;
            bgImage.fillMethod = Image.FillMethod.Radial360;
            bgImage.fillAmount = 1f;
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("ProgressFill");
            fillObj.transform.SetParent(timerRoot.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = PrimaryBlue;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillClockwise = false;
            fillImage.fillOrigin = 2;
            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0.1f, 0.1f);
            fillRT.anchorMax = new Vector2(0.9f, 0.9f);
            fillRT.sizeDelta = Vector2.zero;

            GameObject textObj = new GameObject("TimerText");
            textObj.transform.SetParent(timerRoot.transform, false);
            TextMeshProUGUI timerText = textObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "30";
            timerText.fontSize = 72;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = LightBlue;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            TimerUI timerUI = timerRoot.AddComponent<TimerUI>();
            SerializedObject so = new SerializedObject(timerUI);
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("progressFill").objectReferenceValue = fillImage;
            so.FindProperty("progressBackground").objectReferenceValue = bgImage;
            so.ApplyModifiedProperties();

            if (parent == null)
            {
                Undo.RegisterCreatedObjectUndo(timerRoot, "Create Timer UI");
            }

            return timerRoot;
        }

        private GameObject CreateScoreUI(Transform parent = null)
        {
            GameObject scoreRoot = new GameObject("ScoreUI");
            if (parent != null) scoreRoot.transform.SetParent(parent, false);

            RectTransform scoreRT = scoreRoot.AddComponent<RectTransform>();
            scoreRT.anchorMin = new Vector2(1f, 1f);
            scoreRT.anchorMax = new Vector2(1f, 1f);
            scoreRT.pivot = new Vector2(1f, 1f);
            scoreRT.anchoredPosition = new Vector2(-20, -20);
            scoreRT.sizeDelta = new Vector2(200, 100);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(scoreRoot.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = PanelBackground;
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(scoreRoot.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "SCORE";
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(LightBlue.r, LightBlue.g, LightBlue.b, 0.7f);
            RectTransform labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0.6f);
            labelRT.anchorMax = new Vector2(1, 1f);
            labelRT.sizeDelta = Vector2.zero;

            GameObject valueObj = new GameObject("ScoreValue");
            valueObj.transform.SetParent(scoreRoot.transform, false);
            TextMeshProUGUI scoreText = valueObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "0";
            scoreText.fontSize = 48;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = LightBlue;
            RectTransform valueRT = valueObj.GetComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0, 0);
            valueRT.anchorMax = new Vector2(1, 0.7f);
            valueRT.sizeDelta = Vector2.zero;

            ScoreUI scoreUI = scoreRoot.AddComponent<ScoreUI>();
            SerializedObject so = new SerializedObject(scoreUI);
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("labelText").objectReferenceValue = labelText;
            so.FindProperty("backgroundPanel").objectReferenceValue = bgImage;
            so.ApplyModifiedProperties();

            if (parent == null)
            {
                Undo.RegisterCreatedObjectUndo(scoreRoot, "Create Score UI");
            }

            return scoreRoot;
        }

        private GameObject CreateEndSessionUI(Transform parent = null)
        {
            GameObject endRoot = new GameObject("EndSessionUI");
            if (parent != null) endRoot.transform.SetParent(parent, false);

            RectTransform endRT = endRoot.AddComponent<RectTransform>();
            endRT.anchorMin = Vector2.zero;
            endRT.anchorMax = Vector2.one;
            endRT.sizeDelta = Vector2.zero;

            CanvasGroup canvasGroup = endRoot.AddComponent<CanvasGroup>();

            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(endRoot.transform, false);
            RectTransform panelRT = panelObj.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(400, 350);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = PanelBackground;

            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(panelObj.transform, false);
            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(0.3f, 0.5f, 0.7f, 0.8f);
            RectTransform borderRT = borderObj.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.sizeDelta = new Vector2(4, 4);
            borderRT.anchoredPosition = Vector2.zero;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Session Ended";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.6f, 0.8f, 0.95f, 1f);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.75f);
            titleRT.anchorMax = new Vector2(1, 0.95f);
            titleRT.sizeDelta = Vector2.zero;

            GameObject scoreLabelObj = new GameObject("ScoreLabel");
            scoreLabelObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI scoreLabelText = scoreLabelObj.AddComponent<TextMeshProUGUI>();
            scoreLabelText.text = "Your Score";
            scoreLabelText.fontSize = 24;
            scoreLabelText.alignment = TextAlignmentOptions.Center;
            scoreLabelText.color = new Color(0.5f, 0.65f, 0.8f, 0.8f);
            RectTransform scoreLabelRT = scoreLabelObj.GetComponent<RectTransform>();
            scoreLabelRT.anchorMin = new Vector2(0, 0.55f);
            scoreLabelRT.anchorMax = new Vector2(1, 0.7f);
            scoreLabelRT.sizeDelta = Vector2.zero;

            GameObject scoreValueObj = new GameObject("ScoreValue");
            scoreValueObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI scoreValueText = scoreValueObj.AddComponent<TextMeshProUGUI>();
            scoreValueText.text = "0";
            scoreValueText.fontSize = 72;
            scoreValueText.alignment = TextAlignmentOptions.Center;
            scoreValueText.color = new Color(0.4f, 0.75f, 0.95f, 1f);
            RectTransform scoreValueRT = scoreValueObj.GetComponent<RectTransform>();
            scoreValueRT.anchorMin = new Vector2(0, 0.3f);
            scoreValueRT.anchorMax = new Vector2(1, 0.6f);
            scoreValueRT.sizeDelta = Vector2.zero;

            GameObject buttonObj = new GameObject("RestartButton");
            buttonObj.transform.SetParent(panelObj.transform, false);
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.55f, 0.8f, 1f);
            Button button = buttonObj.AddComponent<Button>();
            RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0.2f, 0.08f);
            buttonRT.anchorMax = new Vector2(0.8f, 0.22f);
            buttonRT.sizeDelta = Vector2.zero;

            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Try Again";
            buttonText.fontSize = 24;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform buttonTextRT = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRT.anchorMin = Vector2.zero;
            buttonTextRT.anchorMax = Vector2.one;
            buttonTextRT.sizeDelta = Vector2.zero;

            EndSessionUI endSessionUI = endRoot.AddComponent<EndSessionUI>();
            SerializedObject so = new SerializedObject(endSessionUI);
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("panelTransform").objectReferenceValue = panelRT;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("scoreValueText").objectReferenceValue = scoreValueText;
            so.FindProperty("scoreLabelText").objectReferenceValue = scoreLabelText;
            so.FindProperty("panelBackground").objectReferenceValue = panelBg;
            so.FindProperty("panelBorder").objectReferenceValue = borderImage;
            so.FindProperty("restartButton").objectReferenceValue = button;
            so.FindProperty("restartButtonText").objectReferenceValue = buttonText;
            so.ApplyModifiedProperties();

            if (parent == null)
            {
                Undo.RegisterCreatedObjectUndo(endRoot, "Create End Session UI");
            }

            return endRoot;
        }
    }
}
#endif
