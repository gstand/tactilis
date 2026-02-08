using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sollertia
{
    /// <summary>
    /// Main game controller for Sollertia rehabilitation game.
    /// Manages button grid, scoring, and game flow.
    /// </summary>
    public class SollertiaGame : MonoBehaviour
    {
        public enum GameState { Setup, Playing, GameOver }
        
        [Header("Game Settings")]
        public float gameDuration = 30f;
        public float buttonActiveTime = 2f;
        public float minSpawnDelay = 0.5f;
        public float maxSpawnDelay = 1.5f;
        
        [Header("Scoring")]
        public int pointsCorrect = 10;
        public int pointsWrong = -5;
        public int pointsTimeout = -3;
        
        [Header("References")]
        public SollertiaButton[] buttons;
        public SollertiaHandTracker handTracker;
        
        [Header("UI")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI messageText;
        public GameObject gameOverPanel;
        
        [Header("Debug")]
        [SerializeField] private GameState state = GameState.Setup;
        [SerializeField] private int score = 0;
        [SerializeField] private float timeRemaining;
        
        private float nextSpawnTime;
        
        public int Score => score;
        public GameState CurrentState => state;
        
        private void Start()
        {
            // Subscribe to button hits
            SollertiaFinger.OnAnyButtonHit += OnButtonHit;
            
            // Find buttons if not assigned
            if (buttons == null || buttons.Length == 0)
            {
                buttons = FindObjectsByType<SollertiaButton>(FindObjectsSortMode.None);
            }
            
            // Find hand tracker if not assigned
            if (handTracker == null)
            {
                handTracker = FindFirstObjectByType<SollertiaHandTracker>();
            }
            
            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }
            
            // Hide game over
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            
            // Show start message
            ShowMessage("Press A to Start");
            
            state = GameState.Setup;
        }
        
        private void OnDestroy()
        {
            SollertiaFinger.OnAnyButtonHit -= OnButtonHit;
        }
        
        private void Update()
        {
            // Check for start input
            if (state == GameState.Setup)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    StartGame();
                }
                
                // Check for controller A button
                if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(
                    UnityEngine.XR.CommonUsages.primaryButton, out bool aPressed) && aPressed)
                {
                    StartGame();
                }
            }
            
            // Game logic
            if (state == GameState.Playing)
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
            
            // Restart from game over
            if (state == GameState.GameOver)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    RestartGame();
                }
                
                if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(
                    UnityEngine.XR.CommonUsages.primaryButton, out bool aPressed) && aPressed)
                {
                    RestartGame();
                }
            }
        }
        
        public void StartGame()
        {
            state = GameState.Playing;
            score = 0;
            timeRemaining = gameDuration;
            nextSpawnTime = Time.time + 0.5f;
            
            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }
            
            // Hide message and game over
            ShowMessage("");
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            
            UpdateScoreUI();
            UpdateTimerUI();
            
            Debug.Log("[SollertiaGame] Game started!");
        }
        
        public void EndGame()
        {
            state = GameState.GameOver;
            
            // Deactivate all buttons
            foreach (var btn in buttons)
            {
                if (btn != null) btn.Deactivate();
            }
            
            // Show game over
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            ShowMessage($"Game Over!\nScore: {score}\n\nPress A to Restart");
            
            Debug.Log($"[SollertiaGame] Game over! Final score: {score}");
        }
        
        public void RestartGame()
        {
            StartGame();
        }
        
        private void SpawnRandomButton()
        {
            // Find inactive buttons
            var inactive = new System.Collections.Generic.List<SollertiaButton>();
            foreach (var btn in buttons)
            {
                if (btn != null && !btn.IsActive)
                    inactive.Add(btn);
            }
            
            if (inactive.Count == 0) return;
            
            // Pick random button and color
            SollertiaButton chosen = inactive[Random.Range(0, inactive.Count)];
            SollertiaButton.ButtonColor color = Random.value > 0.5f ? 
                SollertiaButton.ButtonColor.Blue : 
                SollertiaButton.ButtonColor.Red;
            
            chosen.Activate(color, buttonActiveTime);
        }
        
        private void OnButtonHit(SollertiaButton button, bool correctHand)
        {
            if (state != GameState.Playing) return;
            
            if (correctHand)
            {
                score += pointsCorrect;
                Debug.Log($"[SollertiaGame] Correct! +{pointsCorrect}");
            }
            else
            {
                score += pointsWrong;
                Debug.Log($"[SollertiaGame] Wrong hand! {pointsWrong}");
            }
            
            UpdateScoreUI();
        }
        
        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }
        
        private void UpdateTimerUI()
        {
            if (timerText != null)
                timerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}";
        }
        
        private void ShowMessage(string msg)
        {
            if (messageText != null)
                messageText.text = msg;
        }
    }
}
