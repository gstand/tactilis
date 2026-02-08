using UnityEngine;
using UnityEngine.Events;

namespace Tactilis
{
    /// <summary>
    /// TactilisFingerTip - Collider attached to a finger tip for detecting button touches.
    /// Creates a sphere collider and visual indicator.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class TactilisFingerTip : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== SETTINGS ===")]
        public TactilisHandTracker.Hand hand = TactilisHandTracker.Hand.Left;
        public Color fingerColor = Color.blue;
        public float radius = 0.008f;

        [Header("=== VISUAL ===")]
        public bool showVisual = true;
        public GameObject visualIndicator;

        [Header("=== EVENTS ===")]
        public UnityEvent<TactilisButton> OnButtonTouch;
        #endregion

        #region Private
        private SphereCollider sphereCollider;
        private Rigidbody rb;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupCollider();
            SetupRigidbody();
            SetupVisual();
        }

        private void OnTriggerEnter(Collider other)
        {
            TactilisButton button = other.GetComponent<TactilisButton>();
            if (button != null && button.IsActive)
            {
                OnButtonTouch?.Invoke(button);
            }
        }
        #endregion

        #region Setup
        private void SetupCollider()
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
            }
            sphereCollider.isTrigger = true;
            sphereCollider.radius = radius;
        }

        private void SetupRigidbody()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void SetupVisual()
        {
            if (!showVisual) return;

            if (visualIndicator == null)
            {
                visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visualIndicator.name = "FingerVisual";
                visualIndicator.transform.SetParent(transform);
                visualIndicator.transform.localPosition = Vector3.zero;
                visualIndicator.transform.localScale = Vector3.one * radius * 2f;

                // Remove collider from visual
                Collider visualCollider = visualIndicator.GetComponent<Collider>();
                if (visualCollider != null) Destroy(visualCollider);

                // Set material
                Renderer rend = visualIndicator.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = fingerColor;
                    mat.SetFloat("_Smoothness", 0.8f);
                    rend.material = mat;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetColor(Color color)
        {
            fingerColor = color;
            if (visualIndicator != null)
            {
                Renderer rend = visualIndicator.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = color;
                }
            }
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            Gizmos.color = fingerColor;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        #endregion
    }
}
