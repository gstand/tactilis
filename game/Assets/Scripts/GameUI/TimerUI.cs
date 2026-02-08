using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameUI
{
    /// <summary>
    /// </summary>
    public class TimerUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image progressBackground;

        [Header("Visual Settings")]
        [SerializeField] private bool showMilliseconds = false;
        [SerializeField] private bool pulseOnLowTime = true;
        [SerializeField] private float lowTimeThreshold = 5f;

        [Header("Colors")]
        [SerializeField] private Color primaryBlue = new Color(0.2f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color lightBlue = new Color(0.4f, 0.7f, 0.95f, 1f);
        [SerializeField] private Color darkBlue = new Color(0.1f, 0.25f, 0.45f, 1f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.6f, 0.3f, 1f);

        private float pulseTimer;
        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
            ApplyColorScheme();
        }

        private void ApplyColorScheme()
        {
            if (progressFill != null)
            {
                progressFill.color = primaryBlue;
            }

            if (progressBackground != null)
            {
                progressBackground.color = new Color(darkBlue.r, darkBlue.g, darkBlue.b, 0.3f);
            }

            if (timerText != null)
            {
                timerText.color = lightBlue;
            }
        }

        /// <summary>
        /// Updates the timer display. Called by GameSessionManager.
        /// </summary>
        /// <param name="timeRemaining">Current time remaining in seconds</param>
        /// <param name="totalTime">Total session duration</param>
        public void UpdateTimer(float timeRemaining, float totalTime)
        {
            timeRemaining = Mathf.Max(0f, timeRemaining);

            if (timerText != null)
            {
                if (showMilliseconds)
                {
                    int seconds = Mathf.FloorToInt(timeRemaining);
                    int milliseconds = Mathf.FloorToInt((timeRemaining - seconds) * 100);
                    timerText.text = $"{seconds:00}:{milliseconds:00}";
                }
                else
                {
                    int seconds = Mathf.CeilToInt(timeRemaining);
                    timerText.text = $"{seconds}";
                }
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = timeRemaining / totalTime;
            }

            if (pulseOnLowTime && timeRemaining <= lowTimeThreshold && timeRemaining > 0)
            {
                HandleLowTimePulse(timeRemaining);
            }
            else
            {
                transform.localScale = originalScale;
                if (progressFill != null)
                {
                    progressFill.color = primaryBlue;
                }
            }
        }

        private void HandleLowTimePulse(float timeRemaining)
        {
            pulseTimer += Time.deltaTime * 4f;
            float pulse = 1f + Mathf.Sin(pulseTimer) * 0.05f;
            transform.localScale = originalScale * pulse;

            if (progressFill != null)
            {
                progressFill.color = Color.Lerp(primaryBlue, warningColor, 
                    (lowTimeThreshold - timeRemaining) / lowTimeThreshold);
            }
        }

        /// <summary>
        /// Gets the primary blue color for external use.
        /// </summary>
        public Color GetPrimaryColor() => primaryBlue;
        
        /// <summary>
        /// Gets the light blue color for external use.
        /// </summary>
        public Color GetLightColor() => lightBlue;
        
        /// <summary>
        /// Gets the dark blue color for external use.
        /// </summary>
        public Color GetDarkColor() => darkBlue;
    }
}
