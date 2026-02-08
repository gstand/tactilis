using UnityEngine;

namespace Sollertia
{
    /// <summary>
    /// Finger collider that detects button presses.
    /// Attach to fingertip GameObjects.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class SollertiaFinger : MonoBehaviour
    {
        [Header("Settings")]
        public bool isLeftHand = true;
        public float colliderRadius = 0.01f;
        
        [Header("Visual")]
        public bool showVisual = true;
        public Color leftColor = new Color(0.2f, 0.5f, 1f);
        public Color rightColor = new Color(1f, 0.3f, 0.3f);
        
        private SphereCollider sphereCollider;
        private Rigidbody rb;
        private GameObject visual;
        
        // Reference to game manager for scoring
        public static System.Action<SollertiaButton, bool> OnAnyButtonHit;
        
        private void Awake()
        {
            // Setup collider
            sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = colliderRadius;
            
            // Setup rigidbody
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            // Create visual
            if (showVisual)
            {
                CreateVisual();
            }
        }
        
        private void CreateVisual()
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "FingerVisual";
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * colliderRadius * 2f;
            
            // Remove collider from visual
            Destroy(visual.GetComponent<Collider>());
            
            // Set color
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = isLeftHand ? leftColor : rightColor;
                rend.material = mat;
            }
        }
        
        public void OnButtonHit(SollertiaButton button, bool correctHand)
        {
            OnAnyButtonHit?.Invoke(button, correctHand);
        }
    }
}
