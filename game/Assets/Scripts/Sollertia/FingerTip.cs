using UnityEngine;

namespace Sollertia
{
    /// <summary>
    /// Simple finger tip collider that detects button presses.
    /// Attach to a small sphere that follows the controller/hand.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class FingerTip : MonoBehaviour
    {
        [Header("Settings")]
        public bool isLeftHand = true;
        
        private void Awake()
        {
            // Setup collider as trigger
            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.02f;
            
            // Setup rigidbody as kinematic
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Try to press any button we touch
            GameButton button = other.GetComponent<GameButton>();
            if (button != null && button.IsActive)
            {
                button.Press();
            }
        }
    }
}
