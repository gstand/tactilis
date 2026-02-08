using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace Tactilis
{
    /// <summary>
    /// TactilisHandTracker - Tracks hand joints using XR Hands subsystem (Meta Quest 3).
    /// Provides finger tip positions and detects gestures (pinch, tap).
    /// Falls back to simulated input in Editor for testing.
    /// </summary>
    public class TactilisHandTracker : MonoBehaviour
    {
        #region Enums
        public enum Hand { Left, Right }
        #endregion

        #region Serialized Fields
        [Header("=== FINGER TIP COLLIDERS ===")]
        [Tooltip("Left index finger tip collider (auto-created if null)")]
        public TactilisFingerTip leftIndexTip;

        [Tooltip("Right index finger tip collider (auto-created if null)")]
        public TactilisFingerTip rightIndexTip;

        [Header("=== GESTURE SETTINGS ===")]
        [Tooltip("Distance threshold for pinch detection (thumb to index)")]
        public float pinchThreshold = 0.02f;

        [Tooltip("Minimum time between pinch events")]
        public float pinchCooldown = 0.3f;

        [Header("=== VISUAL FEEDBACK ===")]
        public Color leftHandColor = new Color(0.2f, 0.5f, 1f, 1f);
        public Color rightHandColor = new Color(1f, 0.3f, 0.3f, 1f);
        public float fingerTipRadius = 0.008f;

        [Header("=== EVENTS ===")]
        public UnityEvent<Hand, TactilisButton> OnFingerTap;
        public UnityEvent<Hand> OnPinchGesture;
        public UnityEvent<Hand> OnPinchReleased;
        #endregion

        #region Private State
        private XRHandSubsystem handSubsystem;
        private bool isTracking = false;

        // Joint positions
        private Vector3 leftIndexTipPos;
        private Vector3 leftThumbTipPos;
        private Vector3 rightIndexTipPos;
        private Vector3 rightThumbTipPos;

        // Pinch state
        private bool leftPinching = false;
        private bool rightPinching = false;
        private float leftPinchCooldownTimer = 0f;
        private float rightPinchCooldownTimer = 0f;

        // Editor simulation
        private Vector3 simulatedLeftFingerPos;
        private Vector3 simulatedRightFingerPos;
        #endregion

        #region Properties
        public bool IsTracking => isTracking;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializeHandTracking();
            CreateFingerTipColliders();
        }

        private void Update()
        {
            UpdateCooldowns();

#if UNITY_EDITOR
            if (handSubsystem == null || !handSubsystem.running)
            {
                UpdateEditorSimulation();
                return;
            }
#endif

            if (handSubsystem != null && handSubsystem.running)
            {
                UpdateHandTracking();
            }
        }

        private void OnDestroy()
        {
            if (leftIndexTip != null) leftIndexTip.OnButtonTouch.RemoveListener(OnLeftFingerTouch);
            if (rightIndexTip != null) rightIndexTip.OnButtonTouch.RemoveListener(OnRightFingerTouch);
        }
        #endregion

        #region Initialization
        private void InitializeHandTracking()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if (subsystems.Count > 0)
            {
                handSubsystem = subsystems[0];
                isTracking = true;
                Debug.Log("[TactilisHandTracker] XR Hand subsystem found");
            }
            else
            {
                Debug.LogWarning("[TactilisHandTracker] No XR Hand subsystem found - using editor simulation");
                isTracking = false;
            }
        }

        private void CreateFingerTipColliders()
        {
            if (leftIndexTip == null)
            {
                GameObject leftTipObj = new GameObject("LeftIndexTip");
                leftTipObj.transform.SetParent(transform);
                leftIndexTip = leftTipObj.AddComponent<TactilisFingerTip>();
                leftIndexTip.hand = Hand.Left;
                leftIndexTip.fingerColor = leftHandColor;
                leftIndexTip.radius = fingerTipRadius;
            }

            if (rightIndexTip == null)
            {
                GameObject rightTipObj = new GameObject("RightIndexTip");
                rightTipObj.transform.SetParent(transform);
                rightIndexTip = rightTipObj.AddComponent<TactilisFingerTip>();
                rightIndexTip.hand = Hand.Right;
                rightIndexTip.fingerColor = rightHandColor;
                rightIndexTip.radius = fingerTipRadius;
            }

            // Subscribe to touch events
            leftIndexTip.OnButtonTouch.AddListener(OnLeftFingerTouch);
            rightIndexTip.OnButtonTouch.AddListener(OnRightFingerTouch);
        }
        #endregion

        #region Hand Tracking Update
        private void UpdateHandTracking()
        {
            // Left hand
            if (handSubsystem.leftHand.isTracked)
            {
                UpdateHandJoints(handSubsystem.leftHand, Hand.Left);
            }

            // Right hand
            if (handSubsystem.rightHand.isTracked)
            {
                UpdateHandJoints(handSubsystem.rightHand, Hand.Right);
            }

            isTracking = handSubsystem.leftHand.isTracked || handSubsystem.rightHand.isTracked;
        }

        private void UpdateHandJoints(XRHand hand, Hand handType)
        {
            // Get index finger tip
            if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
            {
                if (handType == Hand.Left)
                {
                    leftIndexTipPos = indexPose.position;
                    if (leftIndexTip != null) leftIndexTip.transform.position = leftIndexTipPos;
                }
                else
                {
                    rightIndexTipPos = indexPose.position;
                    if (rightIndexTip != null) rightIndexTip.transform.position = rightIndexTipPos;
                }
            }

            // Get thumb tip for pinch detection
            if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose))
            {
                if (handType == Hand.Left)
                {
                    leftThumbTipPos = thumbPose.position;
                }
                else
                {
                    rightThumbTipPos = thumbPose.position;
                }
            }

            // Check for pinch gesture
            CheckPinchGesture(handType);
        }

        private void CheckPinchGesture(Hand handType)
        {
            Vector3 indexPos = handType == Hand.Left ? leftIndexTipPos : rightIndexTipPos;
            Vector3 thumbPos = handType == Hand.Left ? leftThumbTipPos : rightThumbTipPos;
            float distance = Vector3.Distance(indexPos, thumbPos);

            bool isPinching = distance < pinchThreshold;
            bool wasPinching = handType == Hand.Left ? leftPinching : rightPinching;
            float cooldown = handType == Hand.Left ? leftPinchCooldownTimer : rightPinchCooldownTimer;

            if (isPinching && !wasPinching && cooldown <= 0f)
            {
                // Pinch started
                if (handType == Hand.Left)
                {
                    leftPinching = true;
                    leftPinchCooldownTimer = pinchCooldown;
                }
                else
                {
                    rightPinching = true;
                    rightPinchCooldownTimer = pinchCooldown;
                }

                OnPinchGesture?.Invoke(handType);
                Debug.Log($"[TactilisHandTracker] Pinch detected: {handType}");
            }
            else if (!isPinching && wasPinching)
            {
                // Pinch released
                if (handType == Hand.Left)
                {
                    leftPinching = false;
                }
                else
                {
                    rightPinching = false;
                }

                OnPinchReleased?.Invoke(handType);
            }
        }

        private void UpdateCooldowns()
        {
            if (leftPinchCooldownTimer > 0f) leftPinchCooldownTimer -= Time.deltaTime;
            if (rightPinchCooldownTimer > 0f) rightPinchCooldownTimer -= Time.deltaTime;
        }
        #endregion

        #region Editor Simulation
        private void UpdateEditorSimulation()
        {
            isTracking = true; // Pretend we're tracking in editor

            // Simulate finger positions based on mouse
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                Plane tablePlane = new Plane(Vector3.up, new Vector3(0, 0.75f, 0));

                if (tablePlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    simulatedLeftFingerPos = hitPoint + Vector3.left * 0.05f;
                    simulatedRightFingerPos = hitPoint + Vector3.right * 0.05f;
                }
            }

            // Update finger tip positions
            if (leftIndexTip != null) leftIndexTip.transform.position = simulatedLeftFingerPos;
            if (rightIndexTip != null) rightIndexTip.transform.position = simulatedRightFingerPos;

            leftIndexTipPos = simulatedLeftFingerPos;
            rightIndexTipPos = simulatedRightFingerPos;

            // Simulate pinch with P key
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.pKey.wasPressedThisFrame && leftPinchCooldownTimer <= 0f)
            {
                leftPinchCooldownTimer = pinchCooldown;
                OnPinchGesture?.Invoke(Hand.Left);
            }
        }
        #endregion

        #region Public Methods
        public Vector3 GetIndexFingerTipPosition(Hand hand)
        {
            return hand == Hand.Left ? leftIndexTipPos : rightIndexTipPos;
        }

        public bool IsPinching(Hand hand)
        {
            return hand == Hand.Left ? leftPinching : rightPinching;
        }
        #endregion

        #region Event Handlers
        private void OnLeftFingerTouch(TactilisButton button)
        {
            OnFingerTap?.Invoke(Hand.Left, button);
        }

        private void OnRightFingerTouch(TactilisButton button)
        {
            OnFingerTap?.Invoke(Hand.Right, button);
        }
        #endregion
    }
}
