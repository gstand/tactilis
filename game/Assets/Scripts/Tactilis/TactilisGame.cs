using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Tactilis
{
    /// <summary>
    /// TactilisGame - Complete unified game controller for the Tactilis AR rehabilitation game.
    /// 
    /// This single script manages the entire game flow:
    /// 1. Table Calibration - Detect/select a table surface in AR
    /// 2. Grid Placement - Position the button grid on the table
    /// 3. Countdown - 3-2-1-GO sequence
    /// 4. Gameplay - Whack-a-mole with finger tracking
    /// 5. Game Over - Show results, option to restart
    /// 
    /// Designed for Meta Quest 3 with hand tracking (no controllers).
    /// </summary>
    public class TactilisGame : MonoBehaviour
    {
        #region Enums
        public enum GamePhase
        {
            Initializing,       // Setting up AR/hand tracking
            WaitingForTable,    // Waiting for user to select a table
            PlacingGrid,        // User positioning the button grid
            Countdown,          // 3-2-1-GO
            Playing,            // Active gameplay
            GameOver,           // Showing results
            Paused              // Game paused
        }
        #endregion

        #region Serialized Fields
        [Header("=== GAME SETTINGS ===")]
        [Tooltip("Duration of gameplay in seconds")]
        public float gameDuration = 30f;

        [Tooltip("Countdown duration (3-2-1)")]
        public int countdownFrom = 3;

        [Tooltip("Delay between spawning buttons")]
        public float minSpawnDelay = 0.8f;
        public float maxSpawnDelay = 2.0f;

        [Tooltip("How long a button stays active before timeout")]
        public float buttonActiveTime = 3.0f;

        [Header("=== SCORING ===")]
        public int pointsCorrectHit = 10;
        public int pointsWrongColor = -5;
        public int pointsTimeout = -3;

        [Header("=== TABLE GRID (Real-World Meters) ===")]
        [Tooltip("Number of button rows")]
        public int gridRows = 3;

        [Tooltip("Number of button columns")]
        public int gridCols = 3;

        [Tooltip("Spacing between buttons (meters) - 4.5cm default for rehab")]
        public float buttonSpacing = 0.045f;

        [Tooltip("Button diameter (meters) - 3cm default")]
        public float buttonDiameter = 0.03f;

        [Tooltip("Table height from floor (meters) - 0.75m standard desk")]
        public float defaultTableHeight = 0.75f;

        [Header("=== REFERENCES ===")]
        [Tooltip("New unified UI system")]
        public SollertiaUI gameUI;
        public TactilisHandTracker handTracker;
        [Tooltip("Hybrid calibration: auto-detects tables, falls back to manual")]
        public SollertiaCalibrationSystem calibrationSystem;
        [Tooltip("Legacy table calibration (optional fallback)")]
        public TactilisTableCalibration tableCalibration;
        public Transform buttonGridRoot;
        public TactilisButton[] buttons;

        [Header("=== AUDIO ===")]
        public AudioSource audioSource;
        public AudioClip countdownBeep;
        public AudioClip goSound;
        public AudioClip correctHitSound;
        public AudioClip wrongHitSound;
        public AudioClip timeoutSound;
        public AudioClip gameOverSound;

        [Header("=== EVENTS ===")]
        public UnityEvent OnGameStarted;
        public UnityEvent OnGameEnded;
        public UnityEvent<int> OnScoreChanged;
        public UnityEvent<TactilisButton> OnButtonHit;
        #endregion

        #region Private State
        [Header("=== DEBUG (Read Only) ===")]
        [SerializeField] private GamePhase currentPhase = GamePhase.Initializing;
        [SerializeField] private int score = 0;
        [SerializeField] private float gameTimer = 0f;
        [SerializeField] private float nextSpawnTime = 0f;

        private List<TactilisButton> activeButtons = new List<TactilisButton>();
        private Coroutine countdownCoroutine;
        private bool isInitialized = false;
        #endregion

        #region Properties
        public GamePhase CurrentPhase => currentPhase;
        public int Score => score;
        public float TimeRemaining => gameTimer;
        public bool IsPlaying => currentPhase == GamePhase.Playing;
        public bool IsGameOver => currentPhase == GamePhase.GameOver;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!isInitialized) return;

            switch (currentPhase)
            {
                case GamePhase.Initializing:
                    UpdateInitializing();
                    break;
                case GamePhase.WaitingForTable:
                    UpdateWaitingForTable();
                    break;
                case GamePhase.PlacingGrid:
                    UpdatePlacingGrid();
                    break;
                case GamePhase.Playing:
                    UpdatePlaying();
                    break;
            }
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            // Validate references
            if (gameUI == null) gameUI = FindFirstObjectByType<SollertiaUI>();
            if (handTracker == null) handTracker = FindFirstObjectByType<TactilisHandTracker>();
            if (calibrationSystem == null) calibrationSystem = FindFirstObjectByType<SollertiaCalibrationSystem>();
            if (tableCalibration == null) tableCalibration = FindFirstObjectByType<TactilisTableCalibration>();

            // Initialize buttons array if not set
            if (buttons == null || buttons.Length == 0)
            {
                buttons = buttonGridRoot != null 
                    ? buttonGridRoot.GetComponentsInChildren<TactilisButton>() 
                    : new TactilisButton[0];
            }

            // Subscribe to hand tracker events
            if (handTracker != null)
            {
                handTracker.OnFingerTap.AddListener(OnFingerTap);
                handTracker.OnPinchGesture.AddListener(OnPinchGesture);
            }

            // Subscribe to hybrid calibration system (preferred)
            if (calibrationSystem != null)
            {
                calibrationSystem.OnCalibrationCompleted.AddListener(OnHybridCalibrationCompleted);
                calibrationSystem.OnAutoDetectionFailed.AddListener(OnAutoDetectionFailed);
                calibrationSystem.OnStatusChanged.AddListener(OnCalibrationStatusChanged);
            }

            // Subscribe to legacy table calibration (fallback)
            if (tableCalibration != null)
            {
                tableCalibration.OnTableSelected.AddListener(OnTableSelected);
            }

            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }

            // Hide grid initially
            if (buttonGridRoot != null) buttonGridRoot.gameObject.SetActive(false);

            isInitialized = true;
            currentPhase = GamePhase.Initializing;

            Debug.Log("[TactilisGame] Initialized - Sollertia Rehabilitation Game");
        }

        private void OnDestroy()
        {
            if (handTracker != null)
            {
                handTracker.OnFingerTap.RemoveListener(OnFingerTap);
                handTracker.OnPinchGesture.RemoveListener(OnPinchGesture);
            }

            if (calibrationSystem != null)
            {
                calibrationSystem.OnCalibrationCompleted.RemoveListener(OnHybridCalibrationCompleted);
                calibrationSystem.OnAutoDetectionFailed.RemoveListener(OnAutoDetectionFailed);
                calibrationSystem.OnStatusChanged.RemoveListener(OnCalibrationStatusChanged);
            }

            if (tableCalibration != null)
            {
                tableCalibration.OnTableSelected.RemoveListener(OnTableSelected);
            }
        }
        #endregion

        #region Phase Updates
        private void UpdateInitializing()
        {
            // Check if hand tracking is ready
            bool handTrackingReady = handTracker == null || handTracker.IsTracking;
            
            if (handTrackingReady)
            {
                // Start hybrid calibration if available
                if (calibrationSystem != null)
                {
                    calibrationSystem.StartCalibration();
                }
                TransitionToPhase(GamePhase.WaitingForTable);
            }
            else
            {
                // Show "Waiting for hand tracking..." message
                if (gameUI != null)
                {
                    gameUI.ShowMessage("Waiting for hand tracking...\n\nHold your hands in front of you.");
                }
            }
        }

        private void UpdateWaitingForTable()
        {
            // If using hybrid calibration, it handles its own UI
            if (calibrationSystem != null && calibrationSystem.IsCalibrating)
            {
                // Calibration system is handling detection
                return;
            }

            // Fallback: UI prompts user to look at a table and pinch to select
            if (gameUI != null)
            {
                gameUI.ShowMessage(
                    "<size=120%><b>TABLE CALIBRATION</b></size>\n\n" +
                    "Looking for table surface...\n\n" +
                    "If no table detected, <color=#44FF44>PINCH</color> to place manually."
                );
            }
        }

        private void UpdatePlacingGrid()
        {
            // If calibration system is in manual mode, let it handle positioning
            if (calibrationSystem != null && 
                calibrationSystem.CurrentMode == SollertiaCalibrationSystem.CalibrationMode.Manual)
            {
                // Calibration system handles grid positioning
                return;
            }

            // Fallback: Grid follows the user's index finger tip
            if (handTracker != null && buttonGridRoot != null)
            {
                Vector3 fingerPos = handTracker.GetIndexFingerTipPosition(TactilisHandTracker.Hand.Left);
                
                // Keep grid at table height, follow finger X/Z
                float tableHeight = defaultTableHeight;
                if (calibrationSystem != null && calibrationSystem.HasValidPlane)
                {
                    // Use detected table height
                    tableHeight = buttonGridRoot.position.y; // Already set by calibration
                }
                else if (tableCalibration != null)
                {
                    tableHeight = tableCalibration.TableHeight;
                }

                Vector3 gridPos = new Vector3(fingerPos.x, tableHeight, fingerPos.z);
                buttonGridRoot.position = Vector3.Lerp(buttonGridRoot.position, gridPos, 0.2f);

                // Rotate to face user
                Vector3 toCamera = Camera.main.transform.position - buttonGridRoot.position;
                toCamera.y = 0;
                if (toCamera.sqrMagnitude > 0.001f)
                {
                    buttonGridRoot.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                }
            }

            if (gameUI != null)
            {
                gameUI.ShowMessage(
                    "<size=120%><b>POSITION GRID</b></size>\n\n" +
                    "Move your hand to adjust position.\n\n" +
                    "<color=#44FF44>PINCH</color> to confirm"
                );
            }
        }

        // Hybrid calibration event handlers
        private void OnHybridCalibrationCompleted(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"[TactilisGame] Hybrid calibration completed at {position}");
            
            // Position grid at calibrated location
            if (buttonGridRoot != null)
            {
                buttonGridRoot.position = position;
                buttonGridRoot.rotation = rotation;
                buttonGridRoot.gameObject.SetActive(true);
            }

            // Skip to countdown
            TransitionToPhase(GamePhase.Countdown);
        }

        private void OnAutoDetectionFailed()
        {
            Debug.Log("[TactilisGame] Auto-detection failed, switching to manual mode");
            
            // Show grid for manual placement
            if (buttonGridRoot != null)
            {
                buttonGridRoot.gameObject.SetActive(true);
            }

            TransitionToPhase(GamePhase.PlacingGrid);
        }

        private void OnCalibrationStatusChanged(string status)
        {
            if (gameUI != null)
            {
                gameUI.ShowMessage(status);
            }
        }

        private void UpdatePlaying()
        {
            // Update timer
            gameTimer -= Time.deltaTime;

            if (gameUI != null)
            {
                gameUI.SetTimer(Mathf.CeilToInt(gameTimer));
            }

            // Check for game over
            if (gameTimer <= 0)
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

            // Check for timeouts
            CheckButtonTimeouts();
        }
        #endregion

        #region Phase Transitions
        private void TransitionToPhase(GamePhase newPhase)
        {
            GamePhase oldPhase = currentPhase;
            currentPhase = newPhase;

            Debug.Log($"[TactilisGame] Phase: {oldPhase} → {newPhase}");

            switch (newPhase)
            {
                case GamePhase.WaitingForTable:
                    if (buttonGridRoot != null) buttonGridRoot.gameObject.SetActive(false);
                    break;

                case GamePhase.PlacingGrid:
                    if (buttonGridRoot != null) buttonGridRoot.gameObject.SetActive(true);
                    break;

                case GamePhase.Countdown:
                    StartCountdown();
                    break;

                case GamePhase.Playing:
                    StartGameplay();
                    break;

                case GamePhase.GameOver:
                    ShowGameOver();
                    break;
            }
        }

        private void OnTableSelected(Vector3 tablePosition, float tableHeight)
        {
            Debug.Log($"[TactilisGame] Table selected at height {tableHeight}m");
            
            // Position grid at table
            if (buttonGridRoot != null)
            {
                buttonGridRoot.position = new Vector3(tablePosition.x, tableHeight, tablePosition.z);
            }

            TransitionToPhase(GamePhase.PlacingGrid);
        }

        private void OnPinchGesture(TactilisHandTracker.Hand hand)
        {
            switch (currentPhase)
            {
                case GamePhase.WaitingForTable:
                    // Use current finger position as table position
                    if (handTracker != null)
                    {
                        Vector3 fingerPos = handTracker.GetIndexFingerTipPosition(hand);
                        OnTableSelected(fingerPos, fingerPos.y);
                    }
                    break;

                case GamePhase.PlacingGrid:
                    // Confirm grid placement
                    TransitionToPhase(GamePhase.Countdown);
                    break;

                case GamePhase.GameOver:
                    // Restart game
                    RestartGame();
                    break;
            }
        }
        #endregion

        #region Countdown
        private void StartCountdown()
        {
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(CountdownSequence());
        }

        private IEnumerator CountdownSequence()
        {
            // "Get Ready"
            if (gameUI != null)
            {
                gameUI.ShowMessage("<size=150%><b>GET READY!</b></size>");
            }
            yield return new WaitForSeconds(1f);

            // 3... 2... 1...
            for (int i = countdownFrom; i > 0; i--)
            {
                if (gameUI != null)
                {
                    gameUI.ShowMessage($"<size=200%><b>{i}</b></size>");
                }
                PlaySound(countdownBeep);
                yield return new WaitForSeconds(1f);
            }

            // GO!
            if (gameUI != null)
            {
                gameUI.ShowMessage("<size=200%><color=#44FF44><b>GO!</b></color></size>");
            }
            PlaySound(goSound);
            yield return new WaitForSeconds(0.5f);

            TransitionToPhase(GamePhase.Playing);
            countdownCoroutine = null;
        }
        #endregion

        #region Gameplay
        private void StartGameplay()
        {
            score = 0;
            gameTimer = gameDuration;
            nextSpawnTime = Time.time + 0.5f;
            activeButtons.Clear();

            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }

            // Update UI
            if (gameUI != null)
            {
                gameUI.HideMessage();
                gameUI.SetScore(score);
                gameUI.SetTimer(Mathf.CeilToInt(gameTimer));
                gameUI.ShowGameHUD(true);
            }

            OnGameStarted?.Invoke();
            Debug.Log("[TactilisGame] Gameplay started!");
        }

        private void SpawnRandomButton()
        {
            // Find inactive buttons
            List<TactilisButton> available = new List<TactilisButton>();
            foreach (var btn in buttons)
            {
                if (btn != null && !btn.IsActive)
                {
                    available.Add(btn);
                }
            }

            if (available.Count == 0) return;

            // Pick random button and color
            TactilisButton chosen = available[Random.Range(0, available.Count)];
            TactilisButton.ButtonColor color = Random.value > 0.5f 
                ? TactilisButton.ButtonColor.Blue 
                : TactilisButton.ButtonColor.Red;

            chosen.Activate(color, buttonActiveTime);
            activeButtons.Add(chosen);
        }

        private void CheckButtonTimeouts()
        {
            for (int i = activeButtons.Count - 1; i >= 0; i--)
            {
                var btn = activeButtons[i];
                if (btn == null || !btn.IsActive)
                {
                    activeButtons.RemoveAt(i);
                    continue;
                }

                if (btn.HasTimedOut)
                {
                    // Timeout penalty
                    AddScore(pointsTimeout);
                    PlaySound(timeoutSound);
                    btn.Deactivate();
                    activeButtons.RemoveAt(i);
                }
            }
        }

        private void OnFingerTap(TactilisHandTracker.Hand hand, TactilisButton button)
        {
            if (currentPhase != GamePhase.Playing) return;
            if (button == null || !button.IsActive) return;

            // Determine expected color based on hand
            TactilisButton.ButtonColor expectedColor = hand == TactilisHandTracker.Hand.Left
                ? TactilisButton.ButtonColor.Blue
                : TactilisButton.ButtonColor.Red;

            bool correct = button.CurrentColor == expectedColor;

            if (correct)
            {
                AddScore(pointsCorrectHit);
                PlaySound(correctHitSound);
                button.OnCorrectHit();
            }
            else
            {
                AddScore(pointsWrongColor);
                PlaySound(wrongHitSound);
                button.OnWrongHit();
            }

            button.Deactivate();
            activeButtons.Remove(button);
            OnButtonHit?.Invoke(button);
        }

        private void AddScore(int points)
        {
            score += points;
            if (score < 0) score = 0;

            if (gameUI != null)
            {
                gameUI.SetScore(score);
            }

            OnScoreChanged?.Invoke(score);
        }
        #endregion

        #region Game Over
        private void EndGame()
        {
            currentPhase = GamePhase.GameOver;

            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }
            activeButtons.Clear();

            PlaySound(gameOverSound);
            OnGameEnded?.Invoke();

            Debug.Log($"[TactilisGame] Game Over! Final score: {score}");
        }

        private void ShowGameOver()
        {
            if (gameUI != null)
            {
                gameUI.ShowGameHUD(false);
                gameUI.ShowGameOver(score);
            }
        }

        public void RestartGame()
        {
            Debug.Log("[TactilisGame] Restarting game...");

            // Reset state
            score = 0;
            gameTimer = gameDuration;
            activeButtons.Clear();

            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }

            // Go back to countdown (keep grid position)
            TransitionToPhase(GamePhase.Countdown);
        }

        public void RestartWithCalibration()
        {
            Debug.Log("[TactilisGame] Restarting with recalibration...");

            score = 0;
            gameTimer = gameDuration;
            activeButtons.Clear();

            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }

            if (buttonGridRoot != null) buttonGridRoot.gameObject.SetActive(false);

            TransitionToPhase(GamePhase.WaitingForTable);
        }
        #endregion

        #region Audio
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        #endregion

        #region Editor Testing (Keyboard Fallback)
#if UNITY_EDITOR
        private void LateUpdate()
        {
            // Keyboard fallback for editor testing
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            // P = Pinch gesture
            if (kb.pKey.wasPressedThisFrame)
            {
                OnPinchGesture(TactilisHandTracker.Hand.Left);
            }

            // R = Restart
            if (kb.rKey.wasPressedThisFrame && currentPhase == GamePhase.GameOver)
            {
                RestartGame();
            }

            // 1-9 = Simulate button taps (for testing)
            if (currentPhase == GamePhase.Playing)
            {
                for (int i = 0; i < Mathf.Min(9, buttons.Length); i++)
                {
                    if (kb[UnityEngine.InputSystem.Key.Digit1 + i].wasPressedThisFrame)
                    {
                        if (buttons[i] != null && buttons[i].IsActive)
                        {
                            // Alternate hands based on key
                            var hand = i % 2 == 0 
                                ? TactilisHandTracker.Hand.Left 
                                : TactilisHandTracker.Hand.Right;
                            OnFingerTap(hand, buttons[i]);
                        }
                    }
                }
            }
        }
#endif
        #endregion
    }
}
