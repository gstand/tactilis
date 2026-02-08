using UnityEngine;
using UnityEngine.Events;

namespace Sollertia
{
    /// <summary>
    /// A simple button that lights up and can be pressed.
    /// Blue = left hand, Red = right hand.
    /// </summary>
    public class SollertiaButton : MonoBehaviour
    {
        public enum ButtonColor { None, Blue, Red }
        
        [Header("Visual")]
        public Renderer buttonRenderer;
        public Color blueColor = new Color(0.2f, 0.5f, 1f);
        public Color redColor = new Color(1f, 0.3f, 0.3f);
        public Color offColor = new Color(0.2f, 0.2f, 0.2f);
        
        [Header("State")]
        [SerializeField] private bool isActive = false;
        [SerializeField] private ButtonColor currentColor = ButtonColor.None;
        
        [Header("Events")]
        public UnityEvent<SollertiaButton> OnPressed;
        public UnityEvent<SollertiaButton> OnTimeout;
        
        private float activationTime;
        private float duration;
        private Material material;
        
        public bool IsActive => isActive;
        public ButtonColor CurrentColor => currentColor;
        public float TimeRemaining => isActive ? Mathf.Max(0, duration - (Time.time - activationTime)) : 0;
        
        private void Awake()
        {
            if (buttonRenderer == null)
                buttonRenderer = GetComponent<Renderer>();
            
            if (buttonRenderer != null)
                material = buttonRenderer.material;
            
            Deactivate();
        }
        
        public void Activate(ButtonColor color, float time)
        {
            isActive = true;
            currentColor = color;
            activationTime = Time.time;
            duration = time;
            
            SetColor(color == ButtonColor.Blue ? blueColor : redColor);
            Debug.Log($"[SollertiaButton] {name} activated as {color}");
        }
        
        public void Deactivate()
        {
            isActive = false;
            currentColor = ButtonColor.None;
            SetColor(offColor);
        }
        
        public void Press()
        {
            if (!isActive) return;
            
            OnPressed?.Invoke(this);
            Deactivate();
        }
        
        private void Update()
        {
            if (isActive && Time.time - activationTime >= duration)
            {
                OnTimeout?.Invoke(this);
                Deactivate();
            }
        }
        
        private void SetColor(Color color)
        {
            if (material != null)
            {
                material.color = color;
                
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * (isActive ? 2f : 0f));
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            
            SollertiaFinger finger = other.GetComponent<SollertiaFinger>();
            if (finger != null)
            {
                // Check if correct hand
                bool correctHand = (currentColor == ButtonColor.Blue && finger.isLeftHand) ||
                                   (currentColor == ButtonColor.Red && !finger.isLeftHand);
                
                finger.OnButtonHit(this, correctHand);
                Press();
            }
        }
    }
}
