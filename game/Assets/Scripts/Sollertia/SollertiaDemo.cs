using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.EventSystems;
// InputSystem types used via fully qualified names to avoid ambiguity with UnityEngine.XR
#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
#endif

namespace Sollertia
{
    /// <summary>
    /// Complete VR demo game that auto-creates EVERYTHING at runtime.
    /// Creates XR Origin, Camera, Button Grid, UI, and handles all input.
    /// Works on: Mac (Editor), Windows, and Meta Quest.
    /// Just add this script to an empty scene and hit play!
    /// </summary>
    public class SollertiaDemo : MonoBehaviour
    {
        [Header("Game Settings - Rehabilitation Pace")]
        public float sessionDuration = 45f;
        public float buttonActiveTime = 5f;
        public float minSpawnDelay = 1.5f;
        public float maxSpawnDelay = 3.0f;
        
        [Header("Grid Settings - Horizontal Table Layout")]
        public int gridRows = 2;
        public int gridColumns = 3;
        public float buttonSpacing = 0.25f;
        public float buttonSize = 0.15f;
        public float tableHeight = 0.7f;
        public float tableDistance = 0.6f;
        
        [Header("Colors - Calming Blue Theme")]
        public Color buttonInactive = new Color(0.1f, 0.2f, 0.35f, 1f);
        public Color buttonActive = new Color(0.3f, 0.6f, 0.9f, 1f);
        public Color buttonPressed = new Color(0.3f, 0.8f, 0.5f, 1f);
        public Color fingerTipColor = new Color(0.4f, 0.7f, 1f, 0.8f);
        public Color uiPanelColor = new Color(0.1f, 0.18f, 0.28f, 0.9f);
        public Color uiTextColor = new Color(0.6f, 0.85f, 1f, 1f);
        
        // Runtime created objects
        private GameObject xrOrigin;
        private Camera mainCamera;
        private GameObject cameraOffset;
        private List<DemoButton> buttons = new List<DemoButton>();
        private GameObject leftIndexTip;
        private GameObject leftMiddleTip;
        private GameObject rightIndexTip;
        private GameObject rightMiddleTip;
        private GameObject buttonGrid;
        private GameObject tableSurface;
        private GameObject uiCanvas;
        
        // UI Elements
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI scoreLabelText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI messageText;
        private GameObject endPanel;
        private TextMeshProUGUI endScoreText;
        private Image timerProgressFill;
        private Image timerProgressBg;
        
        // Passthrough toggle
        private GameObject passthroughButton;
        private bool passthroughEnabled = false;
        
        // Game state
        private int score = 0;
        private float timeRemaining;
        private float nextSpawnTime;
        private bool isPlaying = false;
        private bool gameEnded = false;
        private bool aButtonWasPressed = false;
        private bool bButtonWasPressed = false;
        
        // Main Menu
        private GameObject menuCanvas;
        private GameObject settingsPanel;
        private bool menuActive = true;
        private bool settingsOpen = false;
        private TextMeshProUGUI arToggleLabel;
        private Image arToggleBg;
        
        // Controllers (use fully qualified name to avoid ambiguity with InputSystem.InputDevice)
        private UnityEngine.XR.InputDevice leftController;
        private UnityEngine.XR.InputDevice rightController;
        
        // VR detection
        private bool isVRActive = false;
        
        private void Start()
        {
            StartCoroutine(InitializeVRAndCreateEverything());
        }
        
        private IEnumerator InitializeVRAndCreateEverything()
        {
            // Check if XR is available
            isVRActive = XRSettings.isDeviceActive;
            
            if (!isVRActive && XRGeneralSettings.Instance != null)
            {
                var xrManager = XRGeneralSettings.Instance.Manager;
                if (xrManager != null && !xrManager.isInitializationComplete)
                {
                    xrManager.InitializeLoaderSync();
                    if (xrManager.activeLoader != null)
                    {
                        xrManager.StartSubsystems();
                        yield return new WaitForSeconds(0.5f);
                        isVRActive = XRSettings.isDeviceActive;
                    }
                }
            }
            
            Debug.Log($"[SollertiaDemo] VR Active: {isVRActive}, Device: {XRSettings.loadedDeviceName}");
            
            // Reset this object's position to origin so all children are positioned correctly
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            
            CreateXROrigin();
            CreateEventSystem();
            CreateMainMenu();
            
            // Pre-create game objects (hidden until session begins)
            CreateButtonGrid();
            CreateFingerTips();
            CreateUI();
            RefreshControllers();
            
            InputDevices.deviceConnected += _ => RefreshControllers();
            InputDevices.deviceDisconnected += _ => RefreshControllers();
            
            // Hide game elements until session begins
            if (buttonGrid != null) buttonGrid.SetActive(false);
            if (tableSurface != null) tableSurface.SetActive(false);
            if (uiCanvas != null) uiCanvas.SetActive(false);
            HideFingerTips(true);
            
            Debug.Log("[SollertiaDemo] Main menu ready.");
        }
        
        private void CreateXROrigin()
        {
            // ALWAYS create our own camera to ensure it works
            // First, disable any existing cameras so they don't conflict
            Camera[] existingCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in existingCameras)
            {
                cam.enabled = false;
                Debug.Log($"[SollertiaDemo] Disabled existing camera: {cam.gameObject.name}");
            }
            
            // Destroy existing AudioListeners to avoid warnings
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                Destroy(listener);
            }
            
            // Create our XR Origin
            xrOrigin = new GameObject("SollertiaXROrigin");
            xrOrigin.transform.SetParent(transform);
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
            
            // Camera Offset (for tracking)
            cameraOffset = new GameObject("CameraOffset");
            cameraOffset.transform.SetParent(xrOrigin.transform);
            cameraOffset.transform.localPosition = Vector3.zero;
            
            // Main Camera - positioned to see full table and UI comfortably
            GameObject cameraObj = new GameObject("SollertiaCamera");
            cameraObj.transform.SetParent(cameraOffset.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.3f, -0.2f); // Slightly back, seated height
            cameraObj.transform.localRotation = Quaternion.Euler(35f, 0, 0); // Look down at table
            cameraObj.tag = "MainCamera";
            
            mainCamera = cameraObj.AddComponent<Camera>();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 1f); // Dark blue background
            mainCamera.nearClipPlane = 0.01f;
            mainCamera.farClipPlane = 1000f;
            mainCamera.fieldOfView = 60f;
            mainCamera.enabled = true;
            
            // Add AudioListener
            cameraObj.AddComponent<AudioListener>();
            
            // Add TrackedPoseDriver for VR head tracking (only in VR mode)
            // This is handled by XR Interaction Toolkit if available
            if (isVRActive)
            {
                try
                {
                    var trackedPoseType = System.Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem");
                    if (trackedPoseType != null)
                    {
                        cameraObj.AddComponent(trackedPoseType);
                        Debug.Log("[SollertiaDemo] Added TrackedPoseDriver for VR head tracking");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SollertiaDemo] Could not add TrackedPoseDriver: {e.Message}");
                }
            }
            
            Debug.Log("[SollertiaDemo] Created camera at eye level, looking down at table.");
        }
        
        private void CreateEventSystem()
        {
            // EventSystem is required for UI button clicks to work
            if (FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 0)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(transform);
                esObj.AddComponent<EventSystem>();
                
                // Use the new Input System UI module
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                
                Debug.Log("[SollertiaDemo] Created EventSystem with InputSystem UI module.");
            }
        }
        
        private void CreateMainMenu()
        {
            menuCanvas = new GameObject("MenuCanvas");
            menuCanvas.transform.SetParent(transform);
            Canvas canvas = menuCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            menuCanvas.AddComponent<CanvasScaler>();
            menuCanvas.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900, 600);
            canvasRect.position = new Vector3(0, 1.2f, 1.5f);
            canvasRect.localScale = Vector3.one * 0.002f;
            
            // ── Background ──
            GameObject bgPanel = new GameObject("Background");
            bgPanel.transform.SetParent(menuCanvas.transform, false);
            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0.04f, 0.06f, 0.12f, 0.97f);
            RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // ── Accent line at top ──
            GameObject accentLine = new GameObject("AccentLine");
            accentLine.transform.SetParent(menuCanvas.transform, false);
            Image accentImg = accentLine.AddComponent<Image>();
            accentImg.color = new Color(0.3f, 0.6f, 0.9f, 0.8f);
            RectTransform accentRect = accentLine.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.sizeDelta = new Vector2(0, 4);
            
            // ── Logo "Sollertia" ──
            GameObject logoObj = new GameObject("LogoText");
            logoObj.transform.SetParent(menuCanvas.transform, false);
            TextMeshProUGUI logoText = logoObj.AddComponent<TextMeshProUGUI>();
            logoText.text = "Sollertia";
            logoText.fontSize = 64;
            logoText.color = Color.white;
            logoText.alignment = TextAlignmentOptions.Center;
            logoText.fontStyle = FontStyles.Bold;
            RectTransform logoRect = logoObj.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.sizeDelta = new Vector2(700, 80);
            logoRect.anchoredPosition = new Vector2(0, 140);
            
            // ── Tagline ──
            GameObject taglineObj = new GameObject("Tagline");
            taglineObj.transform.SetParent(menuCanvas.transform, false);
            TextMeshProUGUI taglineText = taglineObj.AddComponent<TextMeshProUGUI>();
            taglineText.text = "Dexterity Rehabilitation System";
            taglineText.fontSize = 22;
            taglineText.color = new Color(0.55f, 0.75f, 0.95f, 0.7f);
            taglineText.alignment = TextAlignmentOptions.Center;
            RectTransform tagRect = taglineObj.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0.5f, 0.5f);
            tagRect.anchorMax = new Vector2(0.5f, 0.5f);
            tagRect.sizeDelta = new Vector2(500, 35);
            tagRect.anchoredPosition = new Vector2(0, 85);
            
            // ── Divider line ──
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(menuCanvas.transform, false);
            Image divImg = divider.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.5f, 0.7f, 0.3f);
            RectTransform divRect = divider.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.5f, 0.5f);
            divRect.anchorMax = new Vector2(0.5f, 0.5f);
            divRect.sizeDelta = new Vector2(400, 1);
            divRect.anchoredPosition = new Vector2(0, 55);
            
            // ── "Begin Session" button ──
            GameObject btnObj = new GameObject("BeginSessionBtn");
            btnObj.transform.SetParent(menuCanvas.transform, false);
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.5f, 0.85f, 1f);
            Button btnComponent = btnObj.AddComponent<Button>();
            btnComponent.targetGraphic = btnBg;
            
            // Button hover/press colors
            ColorBlock colors = btnComponent.colors;
            colors.normalColor = new Color(0.2f, 0.5f, 0.85f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.55f, 0.9f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.7f, 1f);
            colors.selectedColor = new Color(0.2f, 0.5f, 0.85f, 1f);
            btnComponent.colors = colors;
            btnComponent.onClick.AddListener(OnBeginSession);
            
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(320, 65);
            btnRect.anchoredPosition = new Vector2(0, -15);
            
            // Button label
            GameObject btnLabel = new GameObject("BtnLabel");
            btnLabel.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnLabel.AddComponent<TextMeshProUGUI>();
            btnText.text = "Begin Session";
            btnText.fontSize = 28;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontStyle = FontStyles.Bold;
            RectTransform btnLabelRect = btnLabel.GetComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.sizeDelta = Vector2.zero;
            
            // ── Clinician note ──
            GameObject noteObj = new GameObject("ClinicianNote");
            noteObj.transform.SetParent(menuCanvas.transform, false);
            TextMeshProUGUI noteText = noteObj.AddComponent<TextMeshProUGUI>();
            noteText.text = "Clinician: Tap Begin Session or ask your patient to touch the button above";
            noteText.fontSize = 14;
            noteText.color = new Color(0.5f, 0.65f, 0.8f, 0.5f);
            noteText.alignment = TextAlignmentOptions.Center;
            noteText.fontStyle = FontStyles.Italic;
            RectTransform noteRect = noteObj.GetComponent<RectTransform>();
            noteRect.anchorMin = new Vector2(0.5f, 0.5f);
            noteRect.anchorMax = new Vector2(0.5f, 0.5f);
            noteRect.sizeDelta = new Vector2(700, 30);
            noteRect.anchoredPosition = new Vector2(0, -75);
            
            // ── Settings button (gear icon, top-right) ──
            GameObject settingsBtn = new GameObject("SettingsBtn");
            settingsBtn.transform.SetParent(menuCanvas.transform, false);
            Image settingsBg = settingsBtn.AddComponent<Image>();
            settingsBg.color = new Color(0.15f, 0.25f, 0.4f, 0.6f);
            Button settingsBtnComp = settingsBtn.AddComponent<Button>();
            settingsBtnComp.targetGraphic = settingsBg;
            settingsBtnComp.onClick.AddListener(ToggleSettings);
            
            RectTransform settingsBtnRect = settingsBtn.GetComponent<RectTransform>();
            settingsBtnRect.anchorMin = new Vector2(1, 1);
            settingsBtnRect.anchorMax = new Vector2(1, 1);
            settingsBtnRect.pivot = new Vector2(1, 1);
            settingsBtnRect.sizeDelta = new Vector2(50, 50);
            settingsBtnRect.anchoredPosition = new Vector2(-15, -15);
            
            // Gear label
            GameObject gearLabel = new GameObject("GearLabel");
            gearLabel.transform.SetParent(settingsBtn.transform, false);
            TextMeshProUGUI gearText = gearLabel.AddComponent<TextMeshProUGUI>();
            gearText.text = "\u2699";
            gearText.fontSize = 30;
            gearText.color = new Color(0.7f, 0.8f, 0.95f, 0.9f);
            gearText.alignment = TextAlignmentOptions.Center;
            RectTransform gearRect = gearLabel.GetComponent<RectTransform>();
            gearRect.anchorMin = Vector2.zero;
            gearRect.anchorMax = Vector2.one;
            gearRect.sizeDelta = Vector2.zero;
            
            // ── Version / footer ──
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(menuCanvas.transform, false);
            TextMeshProUGUI footerText = footerObj.AddComponent<TextMeshProUGUI>();
            footerText.text = "v1.0  \u2022  UGAHacks XI";
            footerText.fontSize = 12;
            footerText.color = new Color(0.4f, 0.5f, 0.6f, 0.4f);
            footerText.alignment = TextAlignmentOptions.Center;
            RectTransform footerRect = footerObj.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.5f, 0);
            footerRect.anchorMax = new Vector2(0.5f, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.sizeDelta = new Vector2(300, 25);
            footerRect.anchoredPosition = new Vector2(0, 10);
            
            // ── Create Settings Panel (hidden by default) ──
            CreateSettingsPanel();
            
            menuActive = true;
            Debug.Log("[SollertiaDemo] Main menu created.");
        }
        
        private void CreateSettingsPanel()
        {
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(menuCanvas.transform, false);
            
            Image panelBg = settingsPanel.AddComponent<Image>();
            panelBg.color = new Color(0.06f, 0.1f, 0.18f, 0.98f);
            
            RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 300);
            panelRect.anchoredPosition = Vector2.zero;
            
            // Title
            GameObject titleObj = new GameObject("SettingsTitle");
            titleObj.transform.SetParent(settingsPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Clinician Settings";
            titleText.fontSize = 26;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(400, 40);
            titleRect.anchoredPosition = new Vector2(0, -20);
            
            // ── AR / Passthrough Toggle ──
            GameObject arRow = new GameObject("ARToggleRow");
            arRow.transform.SetParent(settingsPanel.transform, false);
            RectTransform arRowRect = arRow.AddComponent<RectTransform>();
            arRowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arRowRect.sizeDelta = new Vector2(400, 50);
            arRowRect.anchoredPosition = new Vector2(0, 30);
            
            // AR label
            GameObject arLabelObj = new GameObject("ARLabel");
            arLabelObj.transform.SetParent(arRow.transform, false);
            TextMeshProUGUI arLabel = arLabelObj.AddComponent<TextMeshProUGUI>();
            arLabel.text = "AR Passthrough";
            arLabel.fontSize = 20;
            arLabel.color = new Color(0.7f, 0.8f, 0.95f, 1f);
            arLabel.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform arLabelRect = arLabelObj.GetComponent<RectTransform>();
            arLabelRect.anchorMin = new Vector2(0, 0);
            arLabelRect.anchorMax = new Vector2(0.6f, 1);
            arLabelRect.sizeDelta = Vector2.zero;
            
            // AR toggle button
            GameObject arToggleBtn = new GameObject("ARToggleBtn");
            arToggleBtn.transform.SetParent(arRow.transform, false);
            arToggleBg = arToggleBtn.AddComponent<Image>();
            arToggleBg.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            Button arBtnComp = arToggleBtn.AddComponent<Button>();
            arBtnComp.targetGraphic = arToggleBg;
            arBtnComp.onClick.AddListener(OnToggleAR);
            
            RectTransform arToggleRect = arToggleBtn.GetComponent<RectTransform>();
            arToggleRect.anchorMin = new Vector2(0.65f, 0.15f);
            arToggleRect.anchorMax = new Vector2(1f, 0.85f);
            arToggleRect.sizeDelta = Vector2.zero;
            
            // Toggle label
            GameObject arToggleLabelObj = new GameObject("ARToggleLabel");
            arToggleLabelObj.transform.SetParent(arToggleBtn.transform, false);
            arToggleLabel = arToggleLabelObj.AddComponent<TextMeshProUGUI>();
            arToggleLabel.text = "OFF";
            arToggleLabel.fontSize = 18;
            arToggleLabel.color = Color.white;
            arToggleLabel.alignment = TextAlignmentOptions.Center;
            arToggleLabel.fontStyle = FontStyles.Bold;
            RectTransform arToggleLabelRect = arToggleLabelObj.GetComponent<RectTransform>();
            arToggleLabelRect.anchorMin = Vector2.zero;
            arToggleLabelRect.anchorMax = Vector2.one;
            arToggleLabelRect.sizeDelta = Vector2.zero;
            
            // ── Session Duration Display ──
            GameObject durRow = new GameObject("DurationRow");
            durRow.transform.SetParent(settingsPanel.transform, false);
            RectTransform durRowRect = durRow.AddComponent<RectTransform>();
            durRowRect.anchorMin = new Vector2(0.5f, 0.5f);
            durRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            durRowRect.sizeDelta = new Vector2(400, 40);
            durRowRect.anchoredPosition = new Vector2(0, -30);
            
            GameObject durLabelObj = new GameObject("DurLabel");
            durLabelObj.transform.SetParent(durRow.transform, false);
            TextMeshProUGUI durLabel = durLabelObj.AddComponent<TextMeshProUGUI>();
            durLabel.text = $"Session Duration: {sessionDuration}s";
            durLabel.fontSize = 18;
            durLabel.color = new Color(0.6f, 0.7f, 0.85f, 0.8f);
            durLabel.alignment = TextAlignmentOptions.Center;
            RectTransform durLabelRect = durLabelObj.GetComponent<RectTransform>();
            durLabelRect.anchorMin = Vector2.zero;
            durLabelRect.anchorMax = Vector2.one;
            durLabelRect.sizeDelta = Vector2.zero;
            
            // ── Close button ──
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(settingsPanel.transform, false);
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.2f, 0.35f, 0.55f, 0.8f);
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.targetGraphic = closeBg;
            closeBtnComp.onClick.AddListener(ToggleSettings);
            
            RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.5f, 0);
            closeBtnRect.anchorMax = new Vector2(0.5f, 0);
            closeBtnRect.pivot = new Vector2(0.5f, 0);
            closeBtnRect.sizeDelta = new Vector2(160, 40);
            closeBtnRect.anchoredPosition = new Vector2(0, 20);
            
            GameObject closeLabelObj = new GameObject("CloseLabel");
            closeLabelObj.transform.SetParent(closeBtn.transform, false);
            TextMeshProUGUI closeLabel = closeLabelObj.AddComponent<TextMeshProUGUI>();
            closeLabel.text = "Close";
            closeLabel.fontSize = 18;
            closeLabel.color = Color.white;
            closeLabel.alignment = TextAlignmentOptions.Center;
            RectTransform closeLabelRect = closeLabelObj.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.sizeDelta = Vector2.zero;
            
            settingsPanel.SetActive(false);
        }
        
        private void ToggleSettings()
        {
            settingsOpen = !settingsOpen;
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(settingsOpen);
            }
        }
        
        private void OnToggleAR()
        {
            TogglePassthrough();
            if (arToggleLabel != null)
            {
                arToggleLabel.text = passthroughEnabled ? "ON" : "OFF";
            }
            if (arToggleBg != null)
            {
                arToggleBg.color = passthroughEnabled 
                    ? new Color(0.2f, 0.6f, 0.4f, 1f) 
                    : new Color(0.3f, 0.3f, 0.4f, 1f);
            }
        }
        
        private void OnBeginSession()
        {
            if (menuActive && !isPlaying && !gameEnded)
            {
                // Hide menu
                menuActive = false;
                if (menuCanvas != null) menuCanvas.SetActive(false);
                
                // Show game elements
                if (buttonGrid != null) buttonGrid.SetActive(true);
                if (tableSurface != null) tableSurface.SetActive(true);
                if (uiCanvas != null) uiCanvas.SetActive(true);
                HideFingerTips(false);
                
                // Start the game
                StartGame();
                
                Debug.Log("[SollertiaDemo] Session begun!");
            }
        }
        
        private void HideFingerTips(bool hide)
        {
            if (leftIndexTip != null) leftIndexTip.SetActive(!hide);
            if (leftMiddleTip != null) leftMiddleTip.SetActive(!hide);
            if (rightIndexTip != null) rightIndexTip.SetActive(!hide);
            if (rightMiddleTip != null) rightMiddleTip.SetActive(!hide);
        }
        
        private void CreateButtonGrid()
        {
            // Create table surface (visual only)
            tableSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableSurface.name = "TableSurface";
            tableSurface.transform.SetParent(transform);
            
            // Position table in front of player at waist height
            float tableWidth = (gridColumns + 1) * buttonSpacing;
            float tableDepth = (gridRows + 1) * buttonSpacing;
            tableSurface.transform.position = new Vector3(0, tableHeight - 0.02f, tableDistance);
            tableSurface.transform.localScale = new Vector3(tableWidth, 0.04f, tableDepth);
            
            // Table material - dark surface
            Renderer tableRend = tableSurface.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material tableMat = new Material(shader);
            tableMat.color = new Color(0.08f, 0.12f, 0.18f, 1f);
            tableRend.material = tableMat;
            
            // Remove table collider (we don't want to block finger tips)
            Collider tableCol = tableSurface.GetComponent<Collider>();
            if (tableCol != null) Destroy(tableCol);
            
            // Create button grid on table surface (horizontal, facing UP)
            buttonGrid = new GameObject("ButtonGrid");
            buttonGrid.transform.SetParent(transform);
            buttonGrid.transform.position = new Vector3(0, tableHeight, tableDistance);
            
            // Buttons laid out on horizontal surface (X = left/right, Z = near/far)
            float startX = -(gridColumns - 1) * buttonSpacing / 2f;
            float startZ = -(gridRows - 1) * buttonSpacing / 2f;
            
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    // X = horizontal spread, Z = depth (near/far from player)
                    Vector3 localPos = new Vector3(
                        startX + col * buttonSpacing,
                        0,
                        startZ + row * buttonSpacing
                    );
                    
                    DemoButton btn = CreateButton(buttonGrid.transform, localPos, row * gridColumns + col);
                    buttons.Add(btn);
                }
            }
            
            Debug.Log($"[SollertiaDemo] Created {buttons.Count} buttons in a {gridRows}x{gridColumns} horizontal table grid.");
        }
        
        private DemoButton CreateButton(Transform parent, Vector3 localPos, int index)
        {
            GameObject buttonObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonObj.name = $"Button_{index}";
            buttonObj.transform.SetParent(parent);
            buttonObj.transform.localPosition = localPos;
            // Cylinder faces UP (horizontal table surface) - no rotation needed
            buttonObj.transform.localRotation = Quaternion.identity;
            buttonObj.transform.localScale = new Vector3(buttonSize, 0.03f, buttonSize);
            
            // Make collider a trigger with extra height for easy pressing
            CapsuleCollider col = buttonObj.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.height = 4f; // Tall trigger zone above button
                col.center = new Vector3(0, 1f, 0); // Center above button surface
            }
            
            // Setup material
            Renderer rend = buttonObj.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = buttonInactive;
            rend.material = mat;
            
            // Add DemoButton component
            DemoButton btn = buttonObj.AddComponent<DemoButton>();
            btn.Setup(this, mat, buttonInactive, buttonActive, buttonPressed);
            
            return btn;
        }
        
        private void CreateFingerTips()
        {
            // Create 4 finger tips: index + middle for both hands
            leftIndexTip = CreateFingerTip("LeftIndexTip", true, false);
            leftMiddleTip = CreateFingerTip("LeftMiddleTip", true, true);
            rightIndexTip = CreateFingerTip("RightIndexTip", false, false);
            rightMiddleTip = CreateFingerTip("RightMiddleTip", false, true);
        }
        
        private GameObject CreateFingerTip(string name, bool isLeft, bool isMiddle)
        {
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = name;
            tip.transform.SetParent(cameraOffset != null ? cameraOffset.transform : transform);
            tip.transform.localScale = Vector3.one * 0.02f;
            
            // Position finger tips at table height, offset for index vs middle
            float xBase = isLeft ? -0.15f : 0.15f;
            float xOffset = isMiddle ? (isLeft ? 0.03f : -0.03f) : 0f;
            tip.transform.localPosition = new Vector3(xBase + xOffset, tableHeight + 0.1f, tableDistance);
            
            // Setup collider
            SphereCollider col = tip.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.5f;
            
            // Add rigidbody
            Rigidbody rb = tip.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            // Setup material
            Renderer rend = tip.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
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
            // Create World Space Canvas - positioned behind the table, facing player
            uiCanvas = new GameObject("GameCanvas");
            uiCanvas.transform.SetParent(transform);
            Canvas canvas = uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            uiCanvas.AddComponent<CanvasScaler>();
            uiCanvas.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800, 300);
            canvasRect.position = new Vector3(0, tableHeight + 0.35f, tableDistance + 0.3f); // Behind and above table
            canvasRect.localScale = Vector3.one * 0.001f;
            canvasRect.localRotation = Quaternion.Euler(25f, 0, 0); // Slight tilt toward player
            
            // Create main panel background
            GameObject panelObj = CreateUIPanel(uiCanvas.transform, "MainPanel", Vector2.zero, new Vector2(700, 250));
            
            // Create Score section (left side)
            CreateScoreUI(panelObj.transform);
            
            // Create Timer section (right side)
            CreateTimerUI(panelObj.transform);
            
            // Create Message text (center bottom)
            messageText = CreateUIText(panelObj.transform, "MessageText", "", 
                new Vector2(0, -80), 20);
            
            // Create End Panel (hidden initially)
            CreateEndPanel(uiCanvas.transform);
            
            // Passthrough toggle is now in the main menu settings panel
        }
        
        private void CreatePassthroughIndicator(Transform parent)
        {
            GameObject toggleObj = new GameObject("PassthroughToggle");
            toggleObj.transform.SetParent(parent, false);
            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchoredPosition = new Vector2(280, 90);
            toggleRect.sizeDelta = new Vector2(120, 40);
            
            Image toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = new Color(0.2f, 0.3f, 0.4f, 0.8f);
            
            TextMeshProUGUI toggleText = CreateUIText(toggleObj.transform, "ToggleText", "P: AR Off", Vector2.zero, 14);
            toggleText.color = new Color(0.7f, 0.8f, 0.9f, 1f);
            
            passthroughButton = toggleObj;
        }
        
        private void CreateScoreUI(Transform parent)
        {
            // Score container
            GameObject scoreContainer = new GameObject("ScoreContainer");
            scoreContainer.transform.SetParent(parent, false);
            RectTransform scoreRect = scoreContainer.AddComponent<RectTransform>();
            scoreRect.anchoredPosition = new Vector2(-150, 20);
            scoreRect.sizeDelta = new Vector2(180, 120);
            
            // Score background
            Image scoreBg = scoreContainer.AddComponent<Image>();
            scoreBg.color = new Color(uiPanelColor.r * 0.8f, uiPanelColor.g * 0.8f, uiPanelColor.b * 0.8f, 0.6f);
            
            // Score label
            scoreLabelText = CreateUIText(scoreContainer.transform, "ScoreLabel", "SCORE", 
                new Vector2(0, 30), 18);
            scoreLabelText.color = new Color(uiTextColor.r, uiTextColor.g, uiTextColor.b, 0.7f);
            
            // Score value
            scoreText = CreateUIText(scoreContainer.transform, "ScoreValue", "0", 
                new Vector2(0, -10), 48);
            scoreText.fontStyle = FontStyles.Bold;
        }
        
        private void CreateTimerUI(Transform parent)
        {
            // Timer container
            GameObject timerContainer = new GameObject("TimerContainer");
            timerContainer.transform.SetParent(parent, false);
            RectTransform timerRect = timerContainer.AddComponent<RectTransform>();
            timerRect.anchoredPosition = new Vector2(150, 20);
            timerRect.sizeDelta = new Vector2(180, 120);
            
            // Timer background
            Image timerBg = timerContainer.AddComponent<Image>();
            timerBg.color = new Color(uiPanelColor.r * 0.8f, uiPanelColor.g * 0.8f, uiPanelColor.b * 0.8f, 0.6f);
            
            // Timer value
            timerText = CreateUIText(timerContainer.transform, "TimerValue", "30", 
                new Vector2(0, 10), 52);
            timerText.fontStyle = FontStyles.Bold;
            
            // Progress bar background
            GameObject progressBgObj = new GameObject("ProgressBg");
            progressBgObj.transform.SetParent(timerContainer.transform, false);
            timerProgressBg = progressBgObj.AddComponent<Image>();
            timerProgressBg.color = new Color(0.1f, 0.15f, 0.25f, 0.5f);
            RectTransform progressBgRect = progressBgObj.GetComponent<RectTransform>();
            progressBgRect.anchoredPosition = new Vector2(0, -40);
            progressBgRect.sizeDelta = new Vector2(150, 10);
            
            // Progress bar fill
            GameObject progressFillObj = new GameObject("ProgressFill");
            progressFillObj.transform.SetParent(progressBgObj.transform, false);
            timerProgressFill = progressFillObj.AddComponent<Image>();
            timerProgressFill.color = buttonActive;
            timerProgressFill.type = Image.Type.Filled;
            timerProgressFill.fillMethod = Image.FillMethod.Horizontal;
            timerProgressFill.fillAmount = 1f;
            RectTransform progressFillRect = progressFillObj.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = Vector2.one;
            progressFillRect.sizeDelta = Vector2.zero;
            progressFillRect.anchoredPosition = Vector2.zero;
        }
        
        private GameObject CreateUIPanel(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = uiPanelColor;
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchoredPosition = pos;
            panelRect.sizeDelta = size;
            return panelObj;
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
            rect.sizeDelta = new Vector2(200, 60);
            
            return tmp;
        }
        
        private void CreateEndPanel(Transform parent)
        {
            endPanel = new GameObject("EndPanel");
            endPanel.transform.SetParent(parent, false);
            
            // Full screen overlay
            CanvasGroup cg = endPanel.AddComponent<CanvasGroup>();
            
            Image bg = endPanel.AddComponent<Image>();
            bg.color = new Color(uiPanelColor.r, uiPanelColor.g, uiPanelColor.b, 0.98f);
            
            RectTransform rect = endPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            // Score / results area
            endScoreText = CreateUIText(endPanel.transform, "EndScore", "", new Vector2(0, 10), 28);
            endScoreText.fontStyle = FontStyles.Normal;
            endScoreText.alignment = TextAlignmentOptions.Center;
            
            endPanel.SetActive(false);
        }
        
        public void OnStartButtonTouched()
        {
            // Legacy - now handled by OnBeginSession via menu
            OnBeginSession();
        }
        
        private void RefreshControllers()
        {
            var leftDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftDevices
            );
            if (leftDevices.Count > 0) leftController = leftDevices[0];
            
            var rightDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightDevices
            );
            if (rightDevices.Count > 0) rightController = rightDevices[0];
            
            Debug.Log($"[SollertiaDemo] Controllers: Left={leftController.isValid}, Right={rightController.isValid}");
        }
        
        private void Update()
        {
            UpdateFingerTips();
            HandleInput();
            UpdateUIPosition();
            
            if (isPlaying)
            {
                UpdateGame();
            }
            
            // Desktop simulation: move finger tips with mouse
            if (!isVRActive)
            {
                SimulateDesktopInput();
            }
        }
        
        private void SimulateDesktopInput()
        {
            if (mainCamera == null) return;
            if (UnityEngine.InputSystem.Mouse.current == null) return;
            
            // Cast ray from mouse to table surface
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Plane tablePlane = new Plane(Vector3.up, new Vector3(0, tableHeight, 0));
            
            Vector3 targetPos;
            if (tablePlane.Raycast(ray, out float distance))
            {
                targetPos = ray.GetPoint(distance);
            }
            else
            {
                targetPos = ray.origin + ray.direction * 1f;
            }
            
            // Move right index finger tip with mouse (simulates pointing)
            if (rightIndexTip != null)
            {
                rightIndexTip.transform.position = Vector3.Lerp(
                    rightIndexTip.transform.position, 
                    targetPos, 
                    Time.deltaTime * 15f
                );
            }
            
            // Left click to push finger down onto button
            if (UnityEngine.InputSystem.Mouse.current.leftButton.isPressed && rightIndexTip != null)
            {
                // Push finger down to table surface
                Vector3 pushPos = targetPos;
                pushPos.y = tableHeight - 0.01f;
                rightIndexTip.transform.position = pushPos;
            }
        }
        
        private void UpdateUIPosition()
        {
            // Keep UI facing the camera
            if (uiCanvas != null && mainCamera != null)
            {
                Vector3 lookDir = uiCanvas.transform.position - mainCamera.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    uiCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            
            // Table stays fixed - no rotation needed for horizontal layout
        }
        
        private void UpdateFingerTips()
        {
            // Try hand tracking first (Quest native hand tracking)
            // Update all 4 finger tips: index + middle for both hands
            bool leftIndexTracked = TryUpdateFingerTipFromHand(leftIndexTip, true, false);
            bool leftMiddleTracked = TryUpdateFingerTipFromHand(leftMiddleTip, true, true);
            bool rightIndexTracked = TryUpdateFingerTipFromHand(rightIndexTip, false, false);
            bool rightMiddleTracked = TryUpdateFingerTipFromHand(rightMiddleTip, false, true);
            
            // Fall back to controller tracking if hands not tracked
            if (!leftIndexTracked)
            {
                UpdateFingerTipFromController(leftController, leftIndexTip, false);
            }
            if (!leftMiddleTracked)
            {
                UpdateFingerTipFromController(leftController, leftMiddleTip, true);
            }
            if (!rightIndexTracked)
            {
                UpdateFingerTipFromController(rightController, rightIndexTip, false);
            }
            if (!rightMiddleTracked)
            {
                UpdateFingerTipFromController(rightController, rightMiddleTip, true);
            }
        }
        
        private bool TryUpdateFingerTipFromHand(GameObject fingerTip, bool isLeft, bool isMiddle)
        {
            if (fingerTip == null) return false;
            
            // Use Unity's XR Hand Subsystem for Quest hand tracking
            // This requires the com.unity.xr.hands package
            var handSubsystems = new List<UnityEngine.SubsystemsImplementation.SubsystemWithProvider>();
            
            // Try to find hand tracking subsystem dynamically
            // This works with Meta's hand tracking on Quest
            var handDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                (isLeft ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right) | 
                InputDeviceCharacteristics.HandTracking,
                handDevices
            );
            
            foreach (var handDevice in handDevices)
            {
                if (!handDevice.isValid) continue;
                
                // Try to get hand tracking data via InputDevice API
                // Meta Quest exposes hand joints through this interface
                if (handDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 handPos) &&
                    handDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion handRot))
                {
                    Transform origin = cameraOffset != null ? cameraOffset.transform : 
                        (mainCamera != null ? mainCamera.transform.parent : null);
                    
                    Vector3 worldPos;
                    Quaternion worldRot;
                    
                    if (origin != null)
                    {
                        worldPos = origin.TransformPoint(handPos);
                        worldRot = origin.rotation * handRot;
                    }
                    else
                    {
                        worldPos = handPos;
                        worldRot = handRot;
                    }
                    
                    // Offset forward to approximate fingertip position
                    // Index finger: straight forward, Middle finger: slightly to the side
                    Vector3 fingerOffset = isMiddle ? 
                        new Vector3(isLeft ? 0.02f : -0.02f, 0, 0.09f) : 
                        new Vector3(0, 0, 0.1f);
                    worldPos += worldRot * fingerOffset;
                    
                    fingerTip.transform.position = worldPos;
                    fingerTip.transform.rotation = worldRot;
                    return true;
                }
            }
            
            return false;
        }
        
        private bool CheckPinchGesture()
        {
            // Check for pinch gesture on either hand (thumb + index close together)
            // This is used to start the game when using hand tracking
            var handDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking, handDevices);
            
            foreach (var handDevice in handDevices)
            {
                if (!handDevice.isValid) continue;
                
                // Check for pinch/select gesture
                if (handDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue > 0.8f)
                {
                    return true;
                }
                
                // Alternative: check grip
                if (handDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue) && gripValue > 0.8f)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void UpdateFingerTipFromController(UnityEngine.XR.InputDevice controller, GameObject fingerTip, bool isMiddle)
        {
            if (fingerTip == null) return;
            
            if (!controller.isValid)
            {
                return;
            }
            
            if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                Transform origin = cameraOffset != null ? cameraOffset.transform : (mainCamera != null ? mainCamera.transform.parent : null);
                
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
                
                // Offset to fingertip position
                // Index: straight forward, Middle: slightly offset to side
                Vector3 fingerOffset = isMiddle ? 
                    new Vector3(0.015f, 0, 0.07f) : 
                    new Vector3(0, 0, 0.08f);
                worldPos += worldRot * fingerOffset;
                
                fingerTip.transform.position = worldPos;
                fingerTip.transform.rotation = worldRot;
            }
        }
        
        private void HandleInput()
        {
            // If menu is active, handle menu interactions via controller/pinch
            bool beginPressed = false;
            
            // Check A button (primary button on right controller)
            if (rightController.isValid)
            {
                if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
                {
                    if (aPressed && !aButtonWasPressed)
                    {
                        beginPressed = true;
                    }
                    aButtonWasPressed = aPressed;
                }
            }
            
            // Check for pinch gesture (hand tracking - no controllers)
            if (!beginPressed && CheckPinchGesture())
            {
                beginPressed = true;
            }
            
            // Keyboard fallback for desktop testing
            if (!beginPressed && UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                beginPressed = true;
            }
            
            if (beginPressed && menuActive)
            {
                OnBeginSession();
            }
        }
        
        private void TogglePassthrough()
        {
            passthroughEnabled = !passthroughEnabled;
            
            // On Quest, this would enable passthrough via OVR API
            // For now, we change the camera background
            if (mainCamera != null)
            {
                if (passthroughEnabled)
                {
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = new Color(0, 0, 0, 0);
                    Debug.Log("[SollertiaDemo] Passthrough enabled");
                }
                else
                {
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 1f);
                    Debug.Log("[SollertiaDemo] Passthrough disabled");
                }
            }
        }
        
        private void ShowStartMessage()
        {
            // No longer needed - main menu handles session start
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
            
            // Deactivate all game buttons
            foreach (var btn in buttons)
            {
                btn.Deactivate();
                btn.gameObject.SetActive(false);
            }
            
            // Hide the table
            if (tableSurface != null) tableSurface.SetActive(false);
            if (buttonGrid != null) buttonGrid.SetActive(false);
            
            // Hide finger tips
            HideFingerTips(true);
            
            // Show final results screen
            if (endPanel != null)
            {
                endPanel.SetActive(true);
                if (endScoreText != null)
                {
                    int maxPossible = Mathf.FloorToInt(sessionDuration / ((minSpawnDelay + maxSpawnDelay) / 2f));
                    endScoreText.text = $"Session Complete\n\nButtons Pressed: {score}\n\nThank You!";
                }
            }
            
            // Clear other UI
            if (messageText != null) messageText.text = "";
            if (scoreText != null) scoreText.text = "";
            if (timerText != null) timerText.text = "";
            if (scoreLabelText != null) scoreLabelText.text = "";
            
            Debug.Log($"[SollertiaDemo] Session complete. Buttons pressed: {score}. Thank you!");
        }
        
        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
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
            
            // Update progress bar
            if (timerProgressFill != null)
            {
                timerProgressFill.fillAmount = Mathf.Max(0, timeRemaining) / sessionDuration;
                
                // Change color when low time
                if (timeRemaining <= 5f && timeRemaining > 0)
                {
                    timerProgressFill.color = new Color(1f, 0.6f, 0.3f, 1f);
                }
                else
                {
                    timerProgressFill.color = buttonActive;
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
    
    /// <summary>
    /// Trigger component for the START button - detects finger touch.
    /// </summary>
    public class StartButtonTrigger : MonoBehaviour
    {
        public SollertiaDemo demo;
        private bool wasPressed = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (wasPressed) return;
            
            // Check if it's a finger
            if (other.GetComponent<DemoFinger>() != null)
            {
                wasPressed = true;
                if (demo != null)
                {
                    demo.OnStartButtonTouched();
                }
                Debug.Log("[StartButton] Touched! Starting game...");
            }
        }
        
        private void OnEnable()
        {
            wasPressed = false;
        }
    }
}
