using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

/// <summary>
/// Manages the calibration flow for placing the button grid at the player's fingertip.
/// Supports both controller input and hand gestures using Unity XR APIs.
/// </summary>
public class CalibrationManager : MonoBehaviour
{
    public enum CalibrationState
    {
        Idle,           // Grid hidden, waiting for grip press or pinch
        Positioning,    // Grid visible at fingertip, waiting for confirm/cancel
        Confirmed       // Calibration complete, grid locked in place
    }

    [Header("References")]
    public GameObject buttonGrid;
    public Transform indexFingerTip;
    public ARGameUI arUI;
    public XRInputWatcher inputWatcher;

    [Header("Positioning Settings")]
    [Tooltip("Vertical offset from fingertip (negative = below finger)")]
    public float verticalOffset = -0.05f;
    
    [Tooltip("How much to smooth grid movement while positioning")]
    [Range(0f, 1f)]
    public float positionSmoothness = 0.1f;
    
    [Tooltip("Distance in front of player if no valid position found")]
    public float fallbackDistance = 0.5f;
    
    [Tooltip("Height for fallback position")]
    public float fallbackHeight = 0.8f;

    [Header("Gesture Settings")]
    [Tooltip("Pinch distance threshold (meters)")]
    public float pinchThreshold = 0.02f;
    
    [Tooltip("Time to hold gesture before it triggers")]
    public float gestureHoldTime = 0.5f;

    [Header("Events")]
    public UnityEvent OnCalibrationStarted;
    public UnityEvent OnCalibrationConfirmed;
    public UnityEvent OnCalibrationCancelled;

    [Header("Debug")]
    [SerializeField] private CalibrationState currentState = CalibrationState.Idle;
    [SerializeField] private bool useHandTracking = false;

    private Vector3 targetGridPosition;
    private Quaternion targetGridRotation;
    private float pinchStartTime = -1f;
    private bool wasLeftPinching = false;
    private bool wasRightPinching = false;
    
    // XR Hand tracking
    private XRHandSubsystem handSubsystem;
    private Vector3 leftIndexTipPos;
    private Vector3 leftThumbTipPos;
    private Vector3 rightIndexTipPos;
    private Vector3 rightThumbTipPos;

    public CalibrationState CurrentState => currentState;
    public bool IsCalibrated => currentState == CalibrationState.Confirmed;

    private void Start()
    {
        if (buttonGrid != null)
        {
            buttonGrid.SetActive(false);
        }

        currentState = CalibrationState.Idle;
        
        if (inputWatcher == null)
        {
            inputWatcher = FindFirstObjectByType<XRInputWatcher>();
        }

        SubscribeToInput();
        UpdateUIForState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInput();
    }

    private void SubscribeToInput()
    {
        if (inputWatcher == null) return;

        inputWatcher.OnLeftGripPressed.AddListener(OnLeftGripPressed);
        inputWatcher.OnPrimaryButtonPressed.AddListener(OnPrimaryButtonPressed);
        inputWatcher.OnSecondaryButtonPressed.AddListener(OnSecondaryButtonPressed);
    }

    private void UnsubscribeFromInput()
    {
        if (inputWatcher == null) return;

        inputWatcher.OnLeftGripPressed.RemoveListener(OnLeftGripPressed);
        inputWatcher.OnPrimaryButtonPressed.RemoveListener(OnPrimaryButtonPressed);
        inputWatcher.OnSecondaryButtonPressed.RemoveListener(OnSecondaryButtonPressed);
    }

    private void Update()
    {
        // Initialize hand subsystem if needed
        if (handSubsystem == null)
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
            {
                handSubsystem = subsystems[0];
            }
        }
        
        // Check for hand tracking availability
        useHandTracking = handSubsystem != null && handSubsystem.running;
        
        // Update hand positions if tracking
        if (useHandTracking)
        {
            UpdateHandPositions();
            DetectHandGestures();
        }
        
        if (currentState == CalibrationState.Positioning)
        {
            UpdateGridPosition();
        }
    }
    
    private void UpdateHandPositions()
    {
        if (handSubsystem == null) return;
        
        // Get left hand joints
        if (handSubsystem.leftHand.isTracked)
        {
            if (handSubsystem.leftHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
            {
                leftIndexTipPos = indexPose.position;
            }
            if (handSubsystem.leftHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose))
            {
                leftThumbTipPos = thumbPose.position;
            }
        }
        
        // Get right hand joints
        if (handSubsystem.rightHand.isTracked)
        {
            if (handSubsystem.rightHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
            {
                rightIndexTipPos = indexPose.position;
            }
            if (handSubsystem.rightHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose))
            {
                rightThumbTipPos = thumbPose.position;
            }
        }
    }
    
    private void DetectHandGestures()
    {
        if (handSubsystem == null) return;
        
        // LEFT HAND PINCH = Start positioning OR Confirm
        bool leftPinching = handSubsystem.leftHand.isTracked && 
                           Vector3.Distance(leftIndexTipPos, leftThumbTipPos) < pinchThreshold;
        
        // RIGHT HAND PINCH = Cancel
        bool rightPinching = handSubsystem.rightHand.isTracked && 
                            Vector3.Distance(rightIndexTipPos, rightThumbTipPos) < pinchThreshold;
        
        switch (currentState)
        {
            case CalibrationState.Idle:
                // Left pinch to start positioning
                if (leftPinching && !wasLeftPinching)
                {
                    StartPositioning();
                }
                break;
                
            case CalibrationState.Positioning:
                // Left pinch to confirm (hold gesture)
                if (leftPinching)
                {
                    if (pinchStartTime < 0)
                    {
                        pinchStartTime = Time.time;
                    }
                    else if (Time.time - pinchStartTime >= gestureHoldTime)
                    {
                        ConfirmCalibration();
                        pinchStartTime = -1f;
                    }
                }
                else
                {
                    pinchStartTime = -1f;
                }
                
                // Right pinch to cancel
                if (rightPinching && !wasRightPinching)
                {
                    CancelCalibration();
                }
                break;
        }
        
        wasLeftPinching = leftPinching;
        wasRightPinching = rightPinching;
    }

    private void OnLeftGripPressed()
    {
        if (currentState == CalibrationState.Idle)
        {
            StartPositioning();
        }
    }

    private void OnPrimaryButtonPressed()
    {
        if (currentState == CalibrationState.Positioning)
        {
            ConfirmCalibration();
        }
    }

    private void OnSecondaryButtonPressed()
    {
        if (currentState == CalibrationState.Positioning)
        {
            CancelCalibration();
        }
    }

    private void StartPositioning()
    {
        currentState = CalibrationState.Positioning;

        if (buttonGrid != null)
        {
            CalculateGridTransform();
            buttonGrid.transform.position = targetGridPosition;
            buttonGrid.transform.rotation = targetGridRotation;
            buttonGrid.SetActive(true);
        }

        OnCalibrationStarted?.Invoke();
        UpdateUIForState();
        Debug.Log("[CalibrationManager] Positioning started - grid visible at fingertip");
    }

    private void ConfirmCalibration()
    {
        currentState = CalibrationState.Confirmed;

        UnsubscribeFromInput();

        OnCalibrationConfirmed?.Invoke();
        UpdateUIForState();
        Debug.Log($"[CalibrationManager] Calibration confirmed at position: {buttonGrid.transform.position}");
    }

    private void CancelCalibration()
    {
        currentState = CalibrationState.Idle;

        if (buttonGrid != null)
        {
            buttonGrid.SetActive(false);
        }

        OnCalibrationCancelled?.Invoke();
        UpdateUIForState();
        Debug.Log("[CalibrationManager] Calibration cancelled - grid hidden");
    }

    private void UpdateGridPosition()
    {
        if (buttonGrid == null || indexFingerTip == null) return;

        CalculateGridTransform();

        if (positionSmoothness > 0f)
        {
            buttonGrid.transform.position = Vector3.Lerp(
                buttonGrid.transform.position,
                targetGridPosition,
                1f - positionSmoothness
            );
            buttonGrid.transform.rotation = Quaternion.Slerp(
                buttonGrid.transform.rotation,
                targetGridRotation,
                1f - positionSmoothness
            );
        }
        else
        {
            buttonGrid.transform.position = targetGridPosition;
            buttonGrid.transform.rotation = targetGridRotation;
        }
    }

    private void CalculateGridTransform()
    {
        if (Camera.main == null) return;

        // Get the best available position for the grid
        Vector3 handPosition = GetBestHandPosition();
        
        // Position at hand location with vertical offset
        targetGridPosition = handPosition + Vector3.up * verticalOffset;

        // Calculate flat rotation facing player horizontally
        Vector3 toPlayer = Camera.main.transform.position - targetGridPosition;
        toPlayer.y = 0f;
        
        if (toPlayer.sqrMagnitude < 0.001f)
        {
            toPlayer = -Camera.main.transform.forward;
            toPlayer.y = 0f;
        }
        toPlayer.Normalize();

        // Create rotation: grid faces player, but lies flat (parallel to ground)
        Quaternion facePlayer = Quaternion.LookRotation(toPlayer, Vector3.up);
        targetGridRotation = facePlayer * Quaternion.Euler(90f, 0f, 0f);
    }
    
    private Vector3 GetBestHandPosition()
    {
        Vector3 position = Vector3.zero;
        bool foundValidPosition = false;
        
        // Priority 1: Use XR hand tracking index finger tip if available
        if (useHandTracking && handSubsystem != null && handSubsystem.leftHand.isTracked)
        {
            position = leftIndexTipPos;
            foundValidPosition = IsValidPosition(position);
            if (foundValidPosition)
            {
                return position;
            }
        }
        
        // Priority 2: Use assigned indexFingerTip transform
        if (indexFingerTip != null)
        {
            position = indexFingerTip.position;
            foundValidPosition = IsValidPosition(position);
            if (foundValidPosition)
            {
                return position;
            }
        }
        
        // Priority 3: Use left controller position via Unity XR
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftHandDevices
        );
        
        if (leftHandDevices.Count > 0)
        {
            if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPos))
            {
                // Convert local controller position to world space
                Transform cameraRig = Camera.main.transform.parent;
                if (cameraRig != null)
                {
                    position = cameraRig.TransformPoint(controllerPos);
                }
                else
                {
                    position = Camera.main.transform.TransformPoint(controllerPos);
                }
                
                foundValidPosition = IsValidPosition(position);
                if (foundValidPosition)
                {
                    return position;
                }
            }
        }
        
        // Priority 4: Fallback - position in front of player
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        position = Camera.main.transform.position + forward * fallbackDistance;
        position.y = fallbackHeight;
        
        return position;
    }
    
    private bool IsValidPosition(Vector3 pos)
    {
        // Check if position is reasonable (not at origin, not too far, not too high/low)
        if (pos.sqrMagnitude < 0.01f) return false;  // Too close to origin
        if (pos.magnitude > 50f) return false;        // Too far away
        
        // Check relative to camera
        if (Camera.main != null)
        {
            Vector3 relativePos = pos - Camera.main.transform.position;
            if (relativePos.magnitude > 5f) return false;  // More than 5m from camera
            if (relativePos.y > 2f) return false;          // More than 2m above camera
            if (relativePos.y < -3f) return false;         // More than 3m below camera
        }
        
        return true;
    }

    private void UpdateUIForState()
    {
        if (arUI == null) return;

        switch (currentState)
        {
            case CalibrationState.Idle:
                if (useHandTracking)
                {
                    arUI.SetRulesText(
                        "<size=120%><b>CALIBRATION</b></size>\n\n" +
                        "Position your <color=#4488FF>LEFT HAND</color> where you want the button grid.\n\n" +
                        "<color=#44FF44>PINCH</color> with left hand to place the grid."
                    );
                }
                else
                {
                    arUI.SetRulesText(
                        "<size=120%><b>CALIBRATION</b></size>\n\n" +
                        "Position your <color=#4488FF>LEFT CONTROLLER</color> where you want the button grid.\n\n" +
                        "Press <b>LEFT GRIP</b> to place the grid."
                    );
                }
                arUI.ShowRules(true);
                break;

            case CalibrationState.Positioning:
                if (useHandTracking)
                {
                    arUI.SetRulesText(
                        "<size=120%><b>ADJUST POSITION</b></size>\n\n" +
                        "Move your left hand to adjust the grid position.\n\n" +
                        "<color=#44FF44>HOLD PINCH</color> (left hand) to confirm\n" +
                        "<color=#FF4444>PINCH</color> (right hand) to cancel"
                    );
                }
                else
                {
                    arUI.SetRulesText(
                        "<size=120%><b>ADJUST POSITION</b></size>\n\n" +
                        "Move your controller to adjust the grid position.\n\n" +
                        "Press <color=#44FF44><b>A</b></color> to confirm\n" +
                        "Press <color=#FF4444><b>B</b></color> to cancel"
                    );
                }
                break;

            case CalibrationState.Confirmed:
                // UI will be handled by GameController for countdown
                arUI.ShowRules(false);
                break;
        }
    }

    /// <summary>
    /// Resets calibration to allow repositioning.
    /// Call this to restart the calibration flow.
    /// </summary>
    public void ResetCalibration()
    {
        currentState = CalibrationState.Idle;

        if (buttonGrid != null)
        {
            buttonGrid.SetActive(false);
        }

        SubscribeToInput();
        UpdateUIForState();
        Debug.Log("[CalibrationManager] Calibration reset");
    }

    private void OnDrawGizmos()
    {
        // Draw fallback position area
        if (Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 fallbackPos = Camera.main.transform.position + forward * fallbackDistance;
            fallbackPos.y = fallbackHeight;
            
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(fallbackPos, new Vector3(0.3f, 0.01f, 0.3f));
        }
        
        // Draw fingertip position if assigned
        if (indexFingerTip != null)
        {
            Vector3 gizmoPos = indexFingerTip.position + Vector3.up * verticalOffset;
            
            Gizmos.color = currentState == CalibrationState.Confirmed ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(gizmoPos, new Vector3(0.3f, 0.01f, 0.3f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(indexFingerTip.position, 0.02f);
        }
    }
}