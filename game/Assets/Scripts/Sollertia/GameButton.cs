using UnityEngine;

namespace Sollertia
{
    /// <summary>
    /// Simple button that lights up and can be pressed.
    /// When active, it glows. Press it before timeout to score!
    /// </summary>
    public class GameButton : MonoBehaviour
    {
        [Header("Colors")]
        public Color activeColor = new Color(0.3f, 0.7f, 1f);   // Bright blue when active
        public Color inactiveColor = new Color(0.1f, 0.2f, 0.3f); // Dark blue when inactive
        public Color pressedColor = new Color(0.2f, 1f, 0.5f);  // Green flash on press
        
        [Header("Settings")]
        public float activeTime = 2f;
        
        [Header("State")]
        [SerializeField] private bool isActive = false;
        
        private Renderer buttonRenderer;
        private Material material;
        private float activationTime;
        
        // Static event for game manager to listen to
        public static System.Action<bool> OnButtonPressed;
        
        public bool IsActive => isActive;
        
        private void Awake()
        {
            buttonRenderer = GetComponent<Renderer>();
            if (buttonRenderer != null)
            {
                material = buttonRenderer.material;
            }
            SetInactive();
        }
        
        private void Update()
        {
            // Check for timeout
            if (isActive && Time.time - activationTime >= activeTime)
            {
                // Timed out - no points
                OnButtonPressed?.Invoke(false);
                SetInactive();
            }
        }
        
        public void Activate()
        {
            isActive = true;
            activationTime = Time.time;
            SetColor(activeColor, true);
        }
        
        public void Press()
        {
            if (!isActive) return;
            
            // Success! Player pressed in time
            OnButtonPressed?.Invoke(true);
            
            // Flash green briefly
            SetColor(pressedColor, false);
            Invoke(nameof(SetInactive), 0.1f);
        }
        
        private void SetInactive()
        {
            isActive = false;
            SetColor(inactiveColor, false);
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
    }
}
