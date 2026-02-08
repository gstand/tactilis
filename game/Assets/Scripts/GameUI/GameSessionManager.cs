using UnityEngine;
using UnityEngine.Events;

namespace GameUI
{
    /// <summary>
    /// </summary>
    public class GameSessionManager : MonoBehaviour
    {
        [Header("Session Settings")]
        [SerializeField] private float sessionDuration = 30f;
        [SerializeField] private bool autoStartSession = false;

        [Header("UI References")]
        [SerializeField] private TimerUI timerUI;
        [SerializeField] private ScoreUI scoreUI;
        [SerializeField] private EndSessionUI endSessionUI;

        [Header("Events")]
        public UnityEvent OnSessionStart;
        public UnityEvent OnSessionEnd;
        public UnityEvent<int> OnScoreChanged;

        private float currentTime;
        private int currentScore;
        private bool isSessionActive;

        public bool IsSessionActive => isSessionActive;
        public int CurrentScore => currentScore;
        public float TimeRemaining => currentTime;

        private void Start()
        {
            if (autoStartSession)
            {
                StartSession();
            }
            else
            {
                ResetSession();
            }
        }

        private void Update()
        {
            if (!isSessionActive) return;

            currentTime -= Time.deltaTime;
            
            if (timerUI != null)
            {
                timerUI.UpdateTimer(currentTime, sessionDuration);
            }

            if (currentTime <= 0f)
            {
                EndSession();
            }
        }

        /// <summary>
        /// Starts a new game session. Call this from your main game.
        /// </summary>
        public void StartSession()
        {
            currentTime = sessionDuration;
            currentScore = 0;
            isSessionActive = true;

            if (timerUI != null)
            {
                timerUI.gameObject.SetActive(true);
                timerUI.UpdateTimer(currentTime, sessionDuration);
            }

            if (scoreUI != null)
            {
                scoreUI.gameObject.SetActive(true);
                scoreUI.UpdateScore(currentScore);
            }

            if (endSessionUI != null)
            {
                endSessionUI.Hide();
            }

            OnSessionStart?.Invoke();
        }

        /// <summary>
        /// Adds points to the current score. Call this when user clicks a button.
        /// </summary>
        /// <param name="points">Number of points to add (default 1)</param>
        public void AddScore(int points = 1)
        {
            if (!isSessionActive) return;

            currentScore += points;
            
            if (scoreUI != null)
            {
                scoreUI.UpdateScore(currentScore);
            }

            OnScoreChanged?.Invoke(currentScore);
        }

        /// <summary>
        /// Ends the current session and shows the end screen.
        /// </summary>
        public void EndSession()
        {
            if (!isSessionActive) return;

            isSessionActive = false;
            currentTime = 0f;

            if (timerUI != null)
            {
                timerUI.UpdateTimer(0f, sessionDuration);
            }

            if (endSessionUI != null)
            {
                endSessionUI.Show(currentScore);
            }

            OnSessionEnd?.Invoke();
        }

        /// <summary>
        /// Resets the session without starting it.
        /// </summary>
        public void ResetSession()
        {
            isSessionActive = false;
            currentTime = sessionDuration;
            currentScore = 0;

            if (timerUI != null)
            {
                timerUI.UpdateTimer(currentTime, sessionDuration);
            }

            if (scoreUI != null)
            {
                scoreUI.UpdateScore(currentScore);
            }

            if (endSessionUI != null)
            {
                endSessionUI.Hide();
            }
        }

        /// <summary>
        /// Sets the session duration. Call before StartSession().
        /// </summary>
        public void SetSessionDuration(float duration)
        {
            sessionDuration = duration;
        }
    }
}
