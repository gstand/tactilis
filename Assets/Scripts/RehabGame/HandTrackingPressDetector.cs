using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

/// <summary>
/// Combines Quest 3 hand tracking with FRS sensor data to detect grid cell presses.
/// Uses hand tracking for position (which cell) and FRS for pressure confirmation.
/// </summary>
public class HandTrackingPressDetector : MonoBehaviour
{
    [Header("References")]
    public RehabGrid grid;
    public FRSSerialManager frsSerialManager;
    public FRSWiFiManager frsWiFiManager;
    public FRSAndroidUSBManager frsAndroidUSBManager;

    [Header("Hand Tracking")]
    [Tooltip("Which hand to track (typically the affected hand for rehab)")]
    public Handedness trackedHand = Handedness.Right;

    [Header("Detection Settings")]
    [Tooltip("Maximum height above grid surface to register a press (meters)")]
    public float maxPressHeight = 0.05f;
    [Tooltip("Require both FRS pressure AND hand position to register hit")]
    public bool requireDualVerification = true;

    [Header("Debug")]
    public bool showDebugVisuals = true;
    public GameObject indexFingerDebugSphere;
    public GameObject middleFingerDebugSphere;

    [Header("Events")]
    public UnityEvent<GridCell, FRSSerialManager.FingerType> onCellPressed;

    XRHandSubsystem _handSubsystem;
    bool _indexPressedThisFrame;
    bool _middlePressedThisFrame;
    Vector3 _indexTipPosition;
    Vector3 _middleTipPosition;
    bool _hasIndexPosition;
    bool _hasMiddlePosition;

    // Prevent double-hits
    GridCell _lastHitCellIndex;
    GridCell _lastHitCellMiddle;
    float _hitCooldown = 0.3f;
    float _lastHitTimeIndex;
    float _lastHitTimeMiddle;

    void Start()
    {
        if (!grid)
            grid = FindFirstObjectByType<RehabGrid>();

        // Try Android USB manager first (Quest 3 with OTG), then WiFi, then Serial (editor only)
        if (!frsAndroidUSBManager)
            frsAndroidUSBManager = FindFirstObjectByType<FRSAndroidUSBManager>();

        if (!frsWiFiManager)
            frsWiFiManager = FindFirstObjectByType<FRSWiFiManager>();

        if (!frsSerialManager)
            frsSerialManager = FindFirstObjectByType<FRSSerialManager>();

        // Subscribe to whichever manager is available
        if (frsAndroidUSBManager)
        {
            frsAndroidUSBManager.onFingerPressed.AddListener(OnFRSFingerPressedAndroidUSB);
        }
        if (frsWiFiManager)
        {
            frsWiFiManager.onFingerPressed.AddListener(OnFRSFingerPressedWiFi);
        }
        if (frsSerialManager)
        {
            frsSerialManager.onFingerPressed.AddListener(OnFRSFingerPressedSerial);
        }

        // Create debug spheres if needed
        if (showDebugVisuals)
        {
            if (!indexFingerDebugSphere)
            {
                indexFingerDebugSphere = CreateDebugSphere(Color.blue, "IndexFingerDebug");
            }
            if (!middleFingerDebugSphere)
            {
                middleFingerDebugSphere = CreateDebugSphere(Color.magenta, "MiddleFingerDebug");
            }
        }
    }

    void OnEnable()
    {
        // Get hand subsystem
        var handSubsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);
        if (handSubsystems.Count > 0)
        {
            _handSubsystem = handSubsystems[0];
            _handSubsystem.updatedHands += OnHandsUpdated;
        }
    }

    void OnDisable()
    {
        if (_handSubsystem != null)
        {
            _handSubsystem.updatedHands -= OnHandsUpdated;
        }
    }

    void OnHandsUpdated(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        XRHand hand = trackedHand == Handedness.Left ? subsystem.leftHand : subsystem.rightHand;

        if (!hand.isTracked)
        {
            _hasIndexPosition = false;
            _hasMiddlePosition = false;
            return;
        }

        // Get index fingertip
        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
        {
            _indexTipPosition = indexPose.position;
            _hasIndexPosition = true;

            if (showDebugVisuals && indexFingerDebugSphere)
            {
                indexFingerDebugSphere.transform.position = _indexTipPosition;
                indexFingerDebugSphere.SetActive(true);
            }
        }

        // Get middle fingertip
        if (hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose middlePose))
        {
            _middleTipPosition = middlePose.position;
            _hasMiddlePosition = true;

            if (showDebugVisuals && middleFingerDebugSphere)
            {
                middleFingerDebugSphere.transform.position = _middleTipPosition;
                middleFingerDebugSphere.SetActive(true);
            }
        }
    }

    void OnFRSFingerPressedSerial(FRSSerialManager.FingerType finger)
    {
        // Convert Serial finger type and process
        ProcessFingerPress(finger == FRSSerialManager.FingerType.Index, finger == FRSSerialManager.FingerType.Middle);
    }

    void OnFRSFingerPressedWiFi(FRSWiFiManager.FingerType finger)
    {
        // Convert WiFi finger type and process
        ProcessFingerPress(finger == FRSWiFiManager.FingerType.Index, finger == FRSWiFiManager.FingerType.Middle);
    }

    void OnFRSFingerPressedAndroidUSB(FRSAndroidUSBManager.FingerType finger)
    {
        // Convert Android USB finger type and process
        ProcessFingerPress(finger == FRSAndroidUSBManager.FingerType.Index, finger == FRSAndroidUSBManager.FingerType.Middle);
    }

    void ProcessFingerPress(bool isIndex, bool isMiddle)
    {
        if (!grid) return;
        if (!isIndex && !isMiddle) return;

        Vector3 fingerPosition;
        bool hasPosition;
        GridCell lastHitCell;
        float lastHitTime;
        string fingerName;

        if (isIndex)
        {
            fingerPosition = _indexTipPosition;
            hasPosition = _hasIndexPosition;
            lastHitCell = _lastHitCellIndex;
            lastHitTime = _lastHitTimeIndex;
            fingerName = "Index";
        }
        else
        {
            fingerPosition = _middleTipPosition;
            hasPosition = _hasMiddlePosition;
            lastHitCell = _lastHitCellMiddle;
            lastHitTime = _lastHitTimeMiddle;
            fingerName = "Middle";
        }

        // If requiring dual verification, we need hand tracking position
        if (requireDualVerification && !hasPosition)
        {
            Debug.Log($"[HandTrackingPressDetector] FRS press detected for {fingerName} but no hand tracking position available");
            return;
        }

        // Find which cell the finger is over
        GridCell cell = null;

        if (hasPosition)
        {
            // Check if finger is close enough to grid surface
            float heightAboveGrid = fingerPosition.y - grid.transform.position.y;
            if (heightAboveGrid > maxPressHeight || heightAboveGrid < -0.02f)
            {
                Debug.Log($"[HandTrackingPressDetector] Finger too far from grid surface: {heightAboveGrid:F3}m");
                return;
            }

            cell = grid.GetCellAtPosition(fingerPosition);
        }
        else
        {
            // Fallback: if no hand tracking, can't determine cell
            return;
        }

        if (cell == null)
        {
            Debug.Log($"[HandTrackingPressDetector] {fingerName} press outside grid bounds");
            return;
        }

        // Cooldown check to prevent double-hits
        if (cell == lastHitCell && Time.time - lastHitTime < _hitCooldown)
        {
            return;
        }

        // Check if cell is active (is a target)
        if (!cell.IsActive)
        {
            Debug.Log($"[HandTrackingPressDetector] {fingerName} pressed inactive cell [{cell.row},{cell.column}]");
            return;
        }

        // Register the hit!
        float reactionTime = cell.RegisterHit();
        Debug.Log($"[HandTrackingPressDetector] HIT! {fingerName} on cell [{cell.row},{cell.column}] - Reaction: {reactionTime:F3}s");

        // Update cooldown tracking
        if (isIndex)
        {
            _lastHitCellIndex = cell;
            _lastHitTimeIndex = Time.time;
        }
        else
        {
            _lastHitCellMiddle = cell;
            _lastHitTimeMiddle = Time.time;
        }

        // Fire event with Serial finger type for compatibility
        var fingerType = isIndex ? FRSSerialManager.FingerType.Index : FRSSerialManager.FingerType.Middle;
        onCellPressed?.Invoke(cell, fingerType);
    }

    GameObject CreateDebugSphere(Color color, string name)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.localScale = Vector3.one * 0.015f;
        sphere.GetComponent<Collider>().enabled = false;

        // Try URP shader first, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            sphere.GetComponent<MeshRenderer>().material = mat;
        }

        sphere.SetActive(false);
        return sphere;
    }

    /// <summary>
    /// For testing without FRS - directly check if finger is pressing a cell
    /// </summary>
    public void SimulatePressAtPosition(Vector3 worldPosition, FRSSerialManager.FingerType finger)
    {
        if (!grid) return;

        GridCell cell = grid.GetCellAtPosition(worldPosition);
        if (cell != null && cell.IsActive)
        {
            cell.RegisterHit();
            onCellPressed?.Invoke(cell, finger);
        }
    }
}
