using UnityEngine;
using UnityEngine.Events;

namespace GameUI
{
    /// <summary>
    /// </summary>
    public class ButtonClickHandler : MonoBehaviour
    {
        [Header("Score Settings")]
        [SerializeField] private int pointsPerClick = 1;
        [SerializeField] private GameSessionManager sessionManager;

        [Header("Optional Feedback")]
        [SerializeField] private AudioSource clickSound;
        [SerializeField] private ParticleSystem clickParticles;

        [Header("Events")]
        public UnityEvent OnButtonClicked;

        private void Start()
        {
            if (sessionManager == null)
            {
                sessionManager = FindFirstObjectByType<GameSessionManager>();
            }
        }

        /// <summary>
        /// Call this method when the button is clicked/selected.
        /// Can be connected to XR Interactable events or UI Button onClick.
        /// </summary>
        public void OnClick()
        {
            if (sessionManager != null && sessionManager.IsSessionActive)
            {
                sessionManager.AddScore(pointsPerClick);
                
                if (clickSound != null)
                {
                    clickSound.Play();
                }

                if (clickParticles != null)
                {
                    clickParticles.Play();
                }

                OnButtonClicked?.Invoke();
            }
        }

        /// <summary>
        /// Sets the points awarded per click.
        /// </summary>
        public void SetPointsPerClick(int points)
        {
            pointsPerClick = points;
        }

        /// <summary>
        /// Sets the session manager reference.
        /// </summary>
        public void SetSessionManager(GameSessionManager manager)
        {
            sessionManager = manager;
        }
    }
}
