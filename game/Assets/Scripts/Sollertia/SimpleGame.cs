using UnityEngine;
using TMPro;

namespace Sollertia
{
    /// <summary>
    /// Simple game manager. Activates random buttons, tracks score.
    /// </summary>
    public class SimpleGame : MonoBehaviour
    {
        [Header("Buttons")]
        public GameButton[] buttons;
        
        [Header("Timing")]
        public float minDelay = 1f;
        public float maxDelay = 2f;
        
        [Header("UI")]
        public TextMeshProUGUI scoreText;
        
        [Header("State")]
        [SerializeField] private int score = 0;
        [SerializeField] private bool isPlaying = true;
        
        private float nextActivationTime;
        
        private void Start()
        {
            // Find all buttons if not assigned
            if (buttons == null || buttons.Length == 0)
            {
                buttons = FindObjectsByType<GameButton>(FindObjectsSortMode.None);
            }
            
            // Subscribe to button events
            GameButton.OnButtonPressed += OnButtonPressed;
            
            // Schedule first button
            nextActivationTime = Time.time + 1f;
            
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            GameButton.OnButtonPressed -= OnButtonPressed;
        }
        
        private void Update()
        {
            if (!isPlaying) return;
            
            // Time to activate a button?
            if (Time.time >= nextActivationTime)
            {
                ActivateRandomButton();
                nextActivationTime = Time.time + Random.Range(minDelay, maxDelay);
            }
        }
        
        private void ActivateRandomButton()
        {
            if (buttons == null || buttons.Length == 0) return;
            
            // Find inactive buttons
            var inactive = new System.Collections.Generic.List<GameButton>();
            foreach (var btn in buttons)
            {
                if (btn != null && !btn.IsActive)
                    inactive.Add(btn);
            }
            
            if (inactive.Count == 0) return;
            
            // Activate random one
            int index = Random.Range(0, inactive.Count);
            inactive[index].Activate();
        }
        
        private void OnButtonPressed(bool success)
        {
            if (success)
            {
                score++;
                Debug.Log($"[SimpleGame] +1 point! Score: {score}");
            }
            else
            {
                Debug.Log($"[SimpleGame] Timeout. Score: {score}");
            }
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }
    }
}
