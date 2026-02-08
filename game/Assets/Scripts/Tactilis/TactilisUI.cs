using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tactilis
{
    /// <summary>
    /// TactilisUI - World-space UI for the Tactilis AR game.
    /// Displays timer, score, messages, and game over screen.
    /// Follows the camera and stays readable in AR.
    /// </summary>
    public class TactilisUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== CANVAS ===")]
        public Canvas uiCanvas;
        public float distanceFromCamera = 1.5f;
        public float heightOffset = 0.1f;
        public float followSpeed = 5f;

        [Header("=== HUD ELEMENTS ===")]
        public RectTransform hudPanel;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI scoreText;
        public Image timerBackground;
        public Image scoreBackground;

        [Header("=== MESSAGE PANEL ===")]
        public RectTransform messagePanel;
        public TextMeshProUGUI messageText;
        public Image messagePanelBackground;

        [Header("=== GAME OVER PANEL ===")]
        public RectTransform gameOverPanel;
        public TextMeshProUGUI gameOverTitleText;
        public TextMeshProUGUI finalScoreText;
        public TextMeshProUGUI restartHintText;
        public Image gameOverBackground;

        [Header("=== COLORS ===")]
        public Color hudBackgroundColor = new Color(0, 0, 0, 0.7f);
        public Color messageBackgroundColor = new Color(0, 0, 0, 0.85f);
        public Color gameOverBackgroundColor = new Color(0, 0, 0, 0.9f);
        public Color textColor = Color.white;
        public Color timerWarningColor = new Color(1f, 0.5f, 0.2f);
        public Color timerCriticalColor = new Color(1f, 0.2f, 0.2f);
        #endregion

        #region Private State
        private Camera mainCamera;
        private bool hudVisible = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            mainCamera = Camera.main;

            if (uiCanvas == null)
            {
                uiCanvas = GetComponentInChildren<Canvas>();
            }

            if (uiCanvas != null)
            {
                uiCanvas.renderMode = RenderMode.WorldSpace;
                uiCanvas.transform.localScale = Vector3.one * 0.001f;
            }

            // Initialize panels
            ShowGameHUD(false);
            HideMessage();
            HideGameOver();
        }

        private void LateUpdate()
        {
            UpdateCanvasPosition();
        }
        #endregion

        #region Canvas Positioning
        private void UpdateCanvasPosition()
        {
            if (mainCamera == null || uiCanvas == null) return;

            // Position in front of camera
            Vector3 targetPos = mainCamera.transform.position 
                + mainCamera.transform.forward * distanceFromCamera
                + Vector3.up * heightOffset;

            uiCanvas.transform.position = Vector3.Lerp(
                uiCanvas.transform.position,
                targetPos,
                Time.deltaTime * followSpeed
            );

            // Face the camera
            uiCanvas.transform.rotation = Quaternion.LookRotation(
                uiCanvas.transform.position - mainCamera.transform.position
            );
        }
        #endregion

        #region HUD Methods
        public void ShowGameHUD(bool show)
        {
            hudVisible = show;

            if (hudPanel != null) hudPanel.gameObject.SetActive(show);
            if (messagePanel != null && show) messagePanel.gameObject.SetActive(false);
        }

        public void SetTimer(int seconds)
        {
            if (timerText == null) return;

            timerText.text = $"Time: {seconds}";

            // Color based on time remaining
            if (seconds <= 5)
            {
                timerText.color = timerCriticalColor;
            }
            else if (seconds <= 10)
            {
                timerText.color = timerWarningColor;
            }
            else
            {
                timerText.color = textColor;
            }
        }

        public void SetScore(int score)
        {
            if (scoreText == null) return;
            scoreText.text = $"Score: {score}";
        }
        #endregion

        #region Message Methods
        public void ShowMessage(string message)
        {
            if (messagePanel != null)
            {
                messagePanel.gameObject.SetActive(true);
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            // Hide HUD when showing message
            if (hudPanel != null && !hudVisible)
            {
                hudPanel.gameObject.SetActive(false);
            }
        }

        public void HideMessage()
        {
            if (messagePanel != null)
            {
                messagePanel.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Game Over Methods
        public void ShowGameOver(int finalScore)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.gameObject.SetActive(true);
            }

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "GAME OVER";
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score: {finalScore}";
            }

            if (restartHintText != null)
            {
                restartHintText.text = "PINCH to play again";
            }

            // Hide other panels
            HideMessage();
            ShowGameHUD(false);
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Utility
        public void SetRulesText(string text)
        {
            ShowMessage(text);
        }

        public void ShowRules(bool show)
        {
            if (show)
            {
                if (messagePanel != null) messagePanel.gameObject.SetActive(true);
            }
            else
            {
                HideMessage();
            }
        }
        #endregion
    }
}
