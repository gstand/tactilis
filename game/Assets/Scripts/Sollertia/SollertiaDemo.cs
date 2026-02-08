using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.XR;

namespace Sollertia
{
    /// <summary>
    /// Complete demo game that auto-creates everything at runtime.
    /// Just add this script to an empty GameObject and hit play!
    /// Uses the blue calming UI theme.
    /// </summary>
    public class SollertiaDemo : MonoBehaviour
    {
        [Header("Game Settings")]
        public float sessionDuration = 30f;
        public float buttonActiveTime = 2f;
        public float minSpawnDelay = 0.8f;
        public float maxSpawnDelay = 1.5f;
        
        [Header("Grid Settings")]
        public int gridRows = 3;
        public int gridColumns = 3;
        public float buttonSpacing = 0.12f;
        public float buttonSize = 0.08f;
        public float gridHeight = 0.8f;
        public float gridDistance = 0.5f;
        
        [Header("Colors - Calming Blue Theme")]
        public Color buttonInactive = new Color(0.1f, 0.2f, 0.35f, 1f);
        public Color buttonActive = new Color(0.3f, 0.6f, 0.9f, 1f);
        public Color buttonPressed = new Color(0.3f, 0.8f, 0.5f, 1f);
        public Color fingerTipColor = new Color(0.4f, 0.7f, 1f, 0.8f);
        public Color uiPanelColor = new Color(0.1f, 0.18f, 0.28f, 0.9f);
        public Color uiTextColor = new Color(0.6f, 0.85f, 1f, 1f);
        
        // Runtime created objects
        private List<DemoButton> buttons = new List<DemoButton>();
        private GameObject leftFingerTip;
        private GameObject rightFingerTip;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI messageText;
        private GameObject endPanel;
        private TextMeshProUGUI endScoreText;
        
        // Game state
        private int score = 0;
        private float timeRemaining;
        private float nextSpawnTime;
        private bool isPlaying = false;
        private bool gameEnded = false;
        
        // Controllers
        private InputDevice leftController;
        private InputDevice rightController;
        
        private void Start()
        {
            CreateEverything();
            ShowStartMessage();
        }
        
        private void CreateEverything()
        {
            CreateButtonGrid();
            CreateFingerTips();
            CreateUI();
            RefreshControllers();
            
            InputDevices.deviceConnected += _ => RefreshControllers();
            InputDevices.deviceDisconnected += _ => RefreshControllers();
            
            Debug.Log("[SollertiaDemo] Everything created! Press A or Space to start.");
        }
        
        private void CreateButtonGrid()
        {
            GameObject gridParent = new GameObject("ButtonGrid");
            gridParent.transform.SetParent(transform);
            gridParent.transform.localPosition = new Vector3(0, gridHeight, gridDistance);
            
            float startX = -(gridColumns - 1) * buttonSpacing / 2f;
            float startZ = -(gridRows - 1) * buttonSpacing / 2f;
            
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    Vector3 localPos = new Vector3(
                        startX + col * buttonSpacing,
                        0,
                        startZ + row * buttonSpacing
                    );
                    
                    DemoButton btn = CreateButton(gridParent.transform, localPos, row * gridColumns + col);
                    buttons.Add(btn);
                }
            }
        }
        
        private DemoButton CreateButton(Transform parent, Vector3 localPos, int index)
        {
            GameObject buttonObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonObj.name = $"Button_{index}";
            buttonObj.transform.SetParent(parent);
            buttonObj.transform.localPosition = localPos;
            buttonObj.transform.localScale = new Vector3(buttonSize, 0.015f, buttonSize);
            
            // Make collider a trigger
            CapsuleCollider col = buttonObj.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.height = 2f;
            }
            
            // Setup material
            Renderer rend = buttonObj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = buttonInactive;
            rend.material = mat;
            
            // Add DemoButton component
            DemoButton btn = buttonObj.AddComponent<DemoButton>();
            btn.Setup(this, mat, buttonInactive, buttonActive, buttonPressed);
            
            return btn;
        }
        
        private void CreateFingerTips()
        {
            leftFingerTip = CreateFingerTip("LeftFingerTip", true);
            rightFingerTip = CreateFingerTip("RightFingerTip", false);
        }
        
        private GameObject CreateFingerTip(string name, bool isLeft)
        {
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = name;
            tip.transform.SetParent(transform);
            tip.transform.localScale = Vector3.one * 0.025f;
            
            // Setup collider
            SphereCollider col = tip.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1f;
            
            // Add rigidbody
            Rigidbody rb = tip.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            // Setup material
            Renderer rend = tip.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = fingerTipColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", fingerTipColor * 0.5f);
            }
            rend.material = mat;
            
            // Add finger component
            DemoFinger finger = tip.AddComponent<DemoFinger>();
            finger.isLeftHand = isLeft;
            
            return tip;
        }
        
        private void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            canvasObj.transform.SetParent(transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400, 150);
            canvasRect.localPosition = new Vector3(0, gridHeight + 0.25f, gridDistance);
            canvasRect.localScale = Vector3.one * 0.001f;
            
            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = uiPanelColor;
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            // Create Score text (left side)
            scoreText = CreateUIText(canvasObj.transform, "ScoreText", "Score: 0", 
                new Vector2(-100, 30), 28);
            
            // Create Timer text (right side)
            timerText = CreateUIText(canvasObj.transform, "TimerText", "30", 
                new Vector2(100, 30), 36);
            
            // Create Message text (center bottom)
            messageText = CreateUIText(canvasObj.transform, "MessageText", "", 
                new Vector2(0, -20), 24);
            
            // Create End Panel (hidden initially)
            CreateEndPanel(canvasObj.transform);
        }
        
        private TextMeshProUGUI CreateUIText(Transform parent, string name, string text, Vector2 pos, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = uiTextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200, 50);
            
            return tmp;
        }
        
        private void CreateEndPanel(Transform parent)
        {
            endPanel = new GameObject("EndPanel");
            endPanel.transform.SetParent(parent, false);
            
            Image bg = endPanel.AddComponent<Image>();
            bg.color = new Color(uiPanelColor.r, uiPanelColor.g, uiPanelColor.b, 0.98f);
            
            RectTransform rect = endPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            // Title
            CreateUIText(endPanel.transform, "EndTitle", "Session Complete!", new Vector2(0, 40), 32);
            
            // Score
            endScoreText = CreateUIText(endPanel.transform, "EndScore", "Score: 0", new Vector2(0, 0), 28);
            
            // Restart hint
            CreateUIText(endPanel.transform, "RestartHint", "Press A to Play Again", new Vector2(0, -40), 20);
            
            endPanel.SetActive(false);
        }
        
        private void RefreshControllers()
        {
            var leftDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftDevices
            );
            if (leftDevices.Count > 0) leftController = leftDevices[0];
            
            var rightDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightDevices
            );
            if (rightDevices.Count > 0) rightController = rightDevices[0];
        }
        
        private void Update()
        {
            UpdateFingerTips();
            HandleInput();
            
            if (isPlaying)
            {
                UpdateGame();
            }
        }
        
        private void UpdateFingerTips()
        {
            UpdateFingerTip(leftController, leftFingerTip);
            UpdateFingerTip(rightController, rightFingerTip);
        }
        
        private void UpdateFingerTip(InputDevice controller, GameObject fingerTip)
        {
            if (fingerTip == null) return;
            
            if (!controller.isValid)
            {
                return;
            }
            
            if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                Transform origin = Camera.main?.transform.parent;
                
                Vector3 worldPos;
                Quaternion worldRot;
                
                if (origin != null)
                {
                    worldPos = origin.TransformPoint(pos);
                    worldRot = origin.rotation * rot;
                }
                else
                {
                    worldPos = pos;
                    worldRot = rot;
                }
                
                worldPos += worldRot * Vector3.forward * 0.05f;
                
                fingerTip.transform.position = worldPos;
                fingerTip.transform.rotation = worldRot;
            }
        }
        
        private void HandleInput()
        {
            bool startPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            
            if (rightController.isValid)
            {
                if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed) && aPressed)
                {
                    startPressed = true;
                }
            }
            
            if (startPressed)
            {
                if (!isPlaying)
                {
                    StartGame();
                }
                else if (gameEnded)
                {
                    StartGame();
                }
            }
        }
        
        private void ShowStartMessage()
        {
            if (messageText != null)
            {
                messageText.text = "Press A to Start";
            }
        }
        
        private void StartGame()
        {
            isPlaying = true;
            gameEnded = false;
            score = 0;
            timeRemaining = sessionDuration;
            nextSpawnTime = Time.time + 0.5f;
            
            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                btn.Deactivate();
            }
            
            // Update UI
            UpdateScoreUI();
            UpdateTimerUI();
            if (messageText != null) messageText.text = "";
            if (endPanel != null) endPanel.SetActive(false);
            
            Debug.Log("[SollertiaDemo] Game started!");
        }
        
        private void UpdateGame()
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            
            if (timeRemaining <= 0)
            {
                EndGame();
                return;
            }
            
            // Spawn buttons
            if (Time.time >= nextSpawnTime)
            {
                SpawnRandomButton();
                nextSpawnTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
            }
        }
        
        private void SpawnRandomButton()
        {
            var inactive = new List<DemoButton>();
            foreach (var btn in buttons)
            {
                if (!btn.IsActive)
                    inactive.Add(btn);
            }
            
            if (inactive.Count == 0) return;
            
            int index = Random.Range(0, inactive.Count);
            inactive[index].Activate(buttonActiveTime);
        }
        
        public void OnButtonPressed(bool success)
        {
            if (!isPlaying || gameEnded) return;
            
            if (success)
            {
                score++;
                Debug.Log($"[SollertiaDemo] +1! Score: {score}");
            }
            
            UpdateScoreUI();
        }
        
        private void EndGame()
        {
            isPlaying = false;
            gameEnded = true;
            
            foreach (var btn in buttons)
            {
                btn.Deactivate();
            }
            
            if (endPanel != null)
            {
                endPanel.SetActive(true);
                if (endScoreText != null)
                {
                    endScoreText.text = $"Score: {score}";
                }
            }
            
            Debug.Log($"[SollertiaDemo] Game over! Final score: {score}");
        }
        
        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }
        
        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0, timeRemaining));
                timerText.text = seconds.ToString();
                
                // Change color when low time
                if (timeRemaining <= 5f && timeRemaining > 0)
                {
                    timerText.color = new Color(1f, 0.6f, 0.3f, 1f);
                }
                else
                {
                    timerText.color = uiTextColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Simple button component for the demo.
    /// </summary>
    public class DemoButton : MonoBehaviour
    {
        private SollertiaDemo game;
        private Material material;
        private Color inactiveColor;
        private Color activeColor;
        private Color pressedColor;
        private bool isActive = false;
        private float activationTime;
        private float duration;
        
        public bool IsActive => isActive;
        
        public void Setup(SollertiaDemo game, Material mat, Color inactive, Color active, Color pressed)
        {
            this.game = game;
            this.material = mat;
            this.inactiveColor = inactive;
            this.activeColor = active;
            this.pressedColor = pressed;
        }
        
        public void Activate(float dur)
        {
            isActive = true;
            duration = dur;
            activationTime = Time.time;
            SetColor(activeColor, true);
        }
        
        public void Deactivate()
        {
            isActive = false;
            SetColor(inactiveColor, false);
        }
        
        public void Press()
        {
            if (!isActive) return;
            
            isActive = false;
            SetColor(pressedColor, false);
            game.OnButtonPressed(true);
            
            Invoke(nameof(Deactivate), 0.1f);
        }
        
        private void Update()
        {
            if (isActive && Time.time - activationTime >= duration)
            {
                game.OnButtonPressed(false);
                Deactivate();
            }
        }
        
        private void SetColor(Color color, bool emissive)
        {
            if (material == null) return;
            
            material.color = color;
            
            if (material.HasProperty("_EmissionColor"))
            {
                if (emissive)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 1.5f);
                }
                else
                {
                    material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<DemoFinger>() != null)
            {
                Press();
            }
        }
    }
    
    /// <summary>
    /// Simple finger component for the demo.
    /// </summary>
    public class DemoFinger : MonoBehaviour
    {
        public bool isLeftHand = true;
    }
}
