using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tactilis
{
    /// <summary>
    /// SollertiaUI - Unified UI system for the Sollertia rehabilitation game.
    /// Handles all game states: calibration, gameplay HUD, and game over screens.
    /// Designed for AR overlay on Meta Quest 3.
    /// </summary>
    public class SollertiaUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== PANELS ===")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private GameObject gameHUDPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject calibrationPanel;

        [Header("=== MESSAGE DISPLAY ===")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image messageBackground;

        [Header("=== GAME HUD ===")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private Slider timerSlider;

        [Header("=== GAME OVER ===")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button recalibrateButton;

        [Header("=== CALIBRATION STATUS ===")]
        [SerializeField] private TextMeshProUGUI calibrationStatusText;
        [SerializeField] private Image calibrationProgressIndicator;

        [Header("=== STYLING ===")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color warningTextColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color dangerTextColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color successTextColor = new Color(0.3f, 1f, 0.5f);

        [Header("=== ANIMATION ===")]
        [SerializeField] private float scorePunchScale = 1.2f;
        [SerializeField] private float scorePunchDuration = 0.15f;
        #endregion

        #region Private State
        private int displayedScore = 0;
        private float gameDuration = 30f;
        private Coroutine scorePunchCoroutine;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Hide all panels initially
            HideAll();
        }

        private void Start()
        {
            // Wire up buttons if assigned
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (recalibrateButton != null)
            {
                recalibrateButton.onClick.AddListener(OnRecalibrateClicked);
            }
        }
        #endregion

        #region Public Methods - Message Display
        public void ShowMessage(string message)
        {
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
            }
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void HideMessage()
        {
            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }
        }

        public void SetMessageColor(Color color)
        {
            if (messageText != null)
            {
                messageText.color = color;
            }
        }
        #endregion

        #region Public Methods - Game HUD
        public void ShowGameHUD(bool show)
        {
            if (gameHUDPanel != null)
            {
                gameHUDPanel.SetActive(show);
            }
            if (show)
            {
                HideMessage();
                HideGameOver();
            }
        }

        public void SetScore(int score)
        {
            displayedScore = score;
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
                
                // Punch animation
                if (scorePunchCoroutine != null)
                {
                    StopCoroutine(scorePunchCoroutine);
                }
                scorePunchCoroutine = StartCoroutine(PunchScale(scoreText.transform));
            }
        }

        public void SetTimer(int seconds)
        {
            if (timerText != null)
            {
                timerText.text = seconds.ToString();
                
                // Color based on time remaining
                if (seconds <= 5)
                {
                    timerText.color = dangerTextColor;
                }
                else if (seconds <= 10)
                {
                    timerText.color = warningTextColor;
                }
                else
                {
                    timerText.color = normalTextColor;
                }
            }

            if (timerSlider != null)
            {
                timerSlider.value = seconds / gameDuration;
            }
        }

        public void SetTimer(float seconds)
        {
            SetTimer(Mathf.CeilToInt(seconds));
        }

        public void SetCombo(int combo)
        {
            if (comboText != null)
            {
                if (combo > 1)
                {
                    comboText.gameObject.SetActive(true);
                    comboText.text = $"x{combo}";
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                }
            }
        }

        public void SetGameDuration(float duration)
        {
            gameDuration = duration;
        }
        #endregion

        #region Public Methods - Game Over
        public void ShowGameOver(int finalScore)
        {
            ShowGameHUD(false);
            HideMessage();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = finalScore.ToString();
            }
        }

        public void ShowGameOver(int finalScore, int hits, int misses, float accuracy)
        {
            ShowGameOver(finalScore);

            if (statsText != null)
            {
                statsText.text = $"Hits: {hits}\nMisses: {misses}\nAccuracy: {accuracy:P0}";
            }
        }

        public void SetHighScore(int highScore)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"Best: {highScore}";
            }
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }
        #endregion

        #region Public Methods - Calibration
        public void ShowCalibrationStatus(string status)
        {
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(true);
            }
            if (calibrationStatusText != null)
            {
                calibrationStatusText.text = status;
            }
        }

        public void HideCalibrationStatus()
        {
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(false);
            }
        }

        public void SetCalibrationProgress(float progress)
        {
            if (calibrationProgressIndicator != null)
            {
                calibrationProgressIndicator.fillAmount = progress;
            }
        }
        #endregion

        #region Public Methods - Utility
        public void HideAll()
        {
            HideMessage();
            ShowGameHUD(false);
            HideGameOver();
            HideCalibrationStatus();
        }

        public void ShowRules(bool show)
        {
            if (show)
            {
                if (messagePanel != null) messagePanel.SetActive(true);
            }
            else
            {
                HideMessage();
            }
        }

        public void SetRulesText(string text)
        {
            ShowMessage(text);
        }
        #endregion

        #region Private Methods
        private System.Collections.IEnumerator PunchScale(Transform target)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 punchedScale = originalScale * scorePunchScale;

            float elapsed = 0f;
            while (elapsed < scorePunchDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scorePunchDuration;
                
                // Punch out then back
                float scale = t < 0.5f 
                    ? Mathf.Lerp(1f, scorePunchScale, t * 2f)
                    : Mathf.Lerp(scorePunchScale, 1f, (t - 0.5f) * 2f);
                
                target.localScale = Vector3.one * scale;
                yield return null;
            }

            target.localScale = originalScale;
            scorePunchCoroutine = null;
        }
        #endregion

        #region Button Callbacks
        private void OnRestartClicked()
        {
            // Find game controller and restart
            var game = FindFirstObjectByType<TactilisGame>();
            if (game != null)
            {
                game.RestartGame();
            }
        }

        private void OnRecalibrateClicked()
        {
            // Find game controller and restart with calibration
            var game = FindFirstObjectByType<TactilisGame>();
            if (game != null)
            {
                game.RestartWithCalibration();
            }
        }
        #endregion
    }
}
