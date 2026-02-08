using UnityEngine;
using UnityEngine.Events;

namespace Tactilis
{
    /// <summary>
    /// TactilisButton - A physical button on the table surface.
    /// Activates with a color (Blue/Red), detects finger collisions, and provides visual feedback.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TactilisButton : MonoBehaviour
    {
        #region Enums
        public enum ButtonColor { None, Blue, Red }
        public enum ButtonState { Inactive, Active, Pressed, Correct, Wrong }
        #endregion

        #region Serialized Fields
        [Header("=== VISUAL ===")]
        public Renderer buttonRenderer;
        public Light buttonLight;
        
        [Header("=== COLORS ===")]
        public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color blueColor = new Color(0.2f, 0.5f, 1f, 1f);
        public Color redColor = new Color(1f, 0.3f, 0.3f, 1f);
        public Color correctFlashColor = new Color(0.3f, 1f, 0.3f, 1f);
        public Color wrongFlashColor = new Color(1f, 0.1f, 0.1f, 1f);

        [Header("=== ANIMATION ===")]
        public float pressDepth = 0.002f;
        public float pressSpeed = 20f;
        public float flashDuration = 0.15f;

        [Header("=== AUDIO ===")]
        public AudioSource audioSource;
        public AudioClip activateSound;
        public AudioClip pressSound;

        [Header("=== EVENTS ===")]
        public UnityEvent<TactilisButton> OnPressed;
        public UnityEvent<TactilisButton> OnTimeout;
        #endregion

        #region Private State
        [Header("=== DEBUG (Read Only) ===")]
        [SerializeField] private ButtonState currentState = ButtonState.Inactive;
        [SerializeField] private ButtonColor currentColor = ButtonColor.None;
        [SerializeField] private float activeTimer = 0f;
        [SerializeField] private float activeTimeout = 3f;

        private Vector3 originalLocalPosition;
        private Vector3 pressedLocalPosition;
        private Material buttonMaterial;
        private float flashTimer = 0f;
        private Color flashFromColor;
        private Color flashToColor;
        #endregion

        #region Properties
        public bool IsActive => currentState == ButtonState.Active;
        public bool HasTimedOut => currentState == ButtonState.Active && activeTimer <= 0f;
        public ButtonColor CurrentColor => currentColor;
        public ButtonState State => currentState;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            pressedLocalPosition = originalLocalPosition - Vector3.up * pressDepth;

            if (buttonRenderer != null)
            {
                buttonMaterial = buttonRenderer.material;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Update()
        {
            // Update active timer
            if (currentState == ButtonState.Active)
            {
                activeTimer -= Time.deltaTime;
                
                // Pulse effect as timeout approaches
                if (activeTimer < 1f && activeTimer > 0f)
                {
                    float pulse = Mathf.Sin(Time.time * 15f) * 0.3f + 0.7f;
                    SetEmission(GetColorForCurrentState() * pulse);
                }
            }

            // Flash animation
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float t = 1f - (flashTimer / flashDuration);
                Color c = Color.Lerp(flashFromColor, flashToColor, t);
                SetColor(c);
            }

            // Press animation
            UpdatePressAnimation();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentState != ButtonState.Active) return;

            // Check if it's a finger
            TactilisFingerTip finger = other.GetComponent<TactilisFingerTip>();
            if (finger != null)
            {
                OnFingerTouch(finger);
            }
        }
        #endregion

        #region Public Methods
        public void Activate(ButtonColor color, float timeout = 3f)
        {
            currentColor = color;
            currentState = ButtonState.Active;
            activeTimer = timeout;
            activeTimeout = timeout;

            Color targetColor = color == ButtonColor.Blue ? blueColor : redColor;
            SetColor(targetColor);
            SetEmission(targetColor * 0.5f);

            if (buttonLight != null)
            {
                buttonLight.enabled = true;
                buttonLight.color = targetColor;
                buttonLight.intensity = 1f;
            }

            PlaySound(activateSound);
        }

        public void Deactivate()
        {
            currentState = ButtonState.Inactive;
            currentColor = ButtonColor.None;
            activeTimer = 0f;

            SetColor(inactiveColor);
            SetEmission(Color.black);

            if (buttonLight != null)
            {
                buttonLight.enabled = false;
            }
        }

        public void OnCorrectHit()
        {
            currentState = ButtonState.Correct;
            Flash(correctFlashColor);
            PlaySound(pressSound);
        }

        public void OnWrongHit()
        {
            currentState = ButtonState.Wrong;
            Flash(wrongFlashColor);
        }
        #endregion

        #region Private Methods
        private void OnFingerTouch(TactilisFingerTip finger)
        {
            if (currentState != ButtonState.Active) return;

            currentState = ButtonState.Pressed;
            OnPressed?.Invoke(this);
        }

        private void Flash(Color flashColor)
        {
            flashFromColor = flashColor;
            flashToColor = inactiveColor;
            flashTimer = flashDuration;
            SetColor(flashColor);
        }

        private void SetColor(Color color)
        {
            if (buttonMaterial != null)
            {
                buttonMaterial.color = color;
            }
        }

        private void SetEmission(Color emission)
        {
            if (buttonMaterial != null)
            {
                buttonMaterial.SetColor("_EmissionColor", emission);
            }
        }

        private Color GetColorForCurrentState()
        {
            return currentColor switch
            {
                ButtonColor.Blue => blueColor,
                ButtonColor.Red => redColor,
                _ => inactiveColor
            };
        }

        private void UpdatePressAnimation()
        {
            Vector3 targetPos = currentState == ButtonState.Pressed 
                ? pressedLocalPosition 
                : originalLocalPosition;

            transform.localPosition = Vector3.Lerp(
                transform.localPosition, 
                targetPos, 
                Time.deltaTime * pressSpeed
            );
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        #endregion

        #region Editor Gizmos
        private void OnDrawGizmos()
        {
            Gizmos.color = currentState == ButtonState.Active 
                ? (currentColor == ButtonColor.Blue ? Color.blue : Color.red)
                : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.015f);
        }
        #endregion
    }
}
