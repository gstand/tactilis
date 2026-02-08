using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Tactilis
{
    /// <summary>
    /// SollertiaCalibrationSystem - Hybrid calibration for Meta Quest 3.
    /// Attempts automatic table detection via AR Foundation plane detection (requires Room Setup).
    /// Falls back to manual fingertip-based calibration if no planes are detected.
    /// </summary>
    public class SollertiaCalibrationSystem : MonoBehaviour
    {
        public enum CalibrationMode
        {
            Auto,       // Trying automatic detection
            Manual,     // User is manually placing
            Completed   // Calibration finished
        }

        public enum CalibrationResult
        {
            None,
            AutoDetected,
            ManualPlacement
        }

        [Header("References")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private GameObject buttonGrid;
        [SerializeField] private Transform indexFingerTip;
        [SerializeField] private Camera mainCamera;

        [Header("Auto Detection Settings")]
        [Tooltip("Time to wait for automatic plane detection before offering manual fallback")]
        [SerializeField] private float autoDetectionTimeout = 5f;
        [Tooltip("Minimum plane area to consider as a valid table (m²)")]
        [SerializeField] private float minTableArea = 0.1f;
        [Tooltip("Expected table height range (meters)")]
        [SerializeField] private float minTableHeight = 0.5f;
        [SerializeField] private float maxTableHeight = 1.0f;
        [Tooltip("Prefer planes classified as 'Table' by Meta Quest")]
        [SerializeField] private bool preferClassifiedTables = true;

        [Header("Manual Calibration Settings")]
        [SerializeField] private float verticalOffset = 0.02f;
        [SerializeField] private float positionSmoothness = 0.1f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject autoDetectionIndicator;
        [SerializeField] private GameObject manualPlacementIndicator;
        [SerializeField] private Color validSurfaceColor = new Color(0.2f, 0.8f, 0.4f, 0.3f);
        [SerializeField] private Color searchingColor = new Color(0.8f, 0.8f, 0.2f, 0.3f);

        [Header("Events")]
        public UnityEvent OnCalibrationStarted;
        public UnityEvent<Vector3, Quaternion> OnCalibrationCompleted;
        public UnityEvent OnAutoDetectionFailed;
        public UnityEvent OnManualCalibrationStarted;
        public UnityEvent<string> OnStatusChanged;

        // State
        private CalibrationMode currentMode = CalibrationMode.Auto;
        private CalibrationResult lastResult = CalibrationResult.None;
        private bool isCalibrating = false;
        private float autoDetectionTimer = 0f;
        private ARPlane detectedTablePlane;
        private Vector3 targetGridPosition;
        private Quaternion targetGridRotation;
        private bool hasValidPlane = false;
        private bool manualPositioningActive = false;

        // Properties
        public CalibrationMode CurrentMode => currentMode;
        public CalibrationResult LastResult => lastResult;
        public bool IsCalibrating => isCalibrating;
        public bool HasValidPlane => hasValidPlane;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.AddListener(OnPlanesChanged);
            }
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
            }
        }

        /// <summary>
        /// Start the calibration process. Attempts auto-detection first.
        /// </summary>
        public void StartCalibration()
        {
            if (isCalibrating) return;

            isCalibrating = true;
            currentMode = CalibrationMode.Auto;
            autoDetectionTimer = 0f;
            hasValidPlane = false;
            detectedTablePlane = null;

            // Enable plane detection
            if (planeManager != null)
            {
                planeManager.enabled = true;
            }

            // Show searching indicator
            SetIndicatorState(true, false);
            UpdateStatus("Scanning for table surface...");

            OnCalibrationStarted?.Invoke();
            Debug.Log("[SollertiaCalibration] Calibration started - attempting auto-detection");
        }

        /// <summary>
        /// Force switch to manual calibration mode.
        /// </summary>
        public void SwitchToManualCalibration()
        {
            if (currentMode == CalibrationMode.Manual) return;

            currentMode = CalibrationMode.Manual;
            manualPositioningActive = false;

            // Disable plane detection to save resources
            if (planeManager != null)
            {
                planeManager.enabled = false;
            }

            SetIndicatorState(false, true);
            UpdateStatus("Manual mode: Press grip to position grid on your fingertip");

            OnManualCalibrationStarted?.Invoke();
            Debug.Log("[SollertiaCalibration] Switched to manual calibration");
        }

        /// <summary>
        /// Start manual positioning (called when user presses grip).
        /// </summary>
        public void StartManualPositioning()
        {
            if (currentMode != CalibrationMode.Manual) return;
            manualPositioningActive = true;
            UpdateStatus("Move your finger to position the grid, then press A to confirm");
        }

        /// <summary>
        /// Stop manual positioning without confirming.
        /// </summary>
        public void StopManualPositioning()
        {
            manualPositioningActive = false;
        }

        /// <summary>
        /// Confirm the current placement (auto or manual).
        /// </summary>
        public void ConfirmPlacement()
        {
            if (!isCalibrating) return;

            if (currentMode == CalibrationMode.Auto && hasValidPlane)
            {
                // Confirm auto-detected placement
                lastResult = CalibrationResult.AutoDetected;
                CompleteCalibration();
            }
            else if (currentMode == CalibrationMode.Manual && buttonGrid != null)
            {
                // Confirm manual placement
                lastResult = CalibrationResult.ManualPlacement;
                CompleteCalibration();
            }
        }

        /// <summary>
        /// Cancel calibration and reset.
        /// </summary>
        public void CancelCalibration()
        {
            isCalibrating = false;
            currentMode = CalibrationMode.Auto;
            manualPositioningActive = false;

            if (planeManager != null)
            {
                planeManager.enabled = false;
            }

            SetIndicatorState(false, false);
            Debug.Log("[SollertiaCalibration] Calibration cancelled");
        }

        private void Update()
        {
            if (!isCalibrating) return;

            switch (currentMode)
            {
                case CalibrationMode.Auto:
                    UpdateAutoDetection();
                    break;
                case CalibrationMode.Manual:
                    UpdateManualPlacement();
                    break;
            }
        }

        private void UpdateAutoDetection()
        {
            autoDetectionTimer += Time.deltaTime;

            // Check for timeout
            if (!hasValidPlane && autoDetectionTimer >= autoDetectionTimeout)
            {
                Debug.Log("[SollertiaCalibration] Auto-detection timeout - no planes found");
                UpdateStatus("No table detected. Room Setup may be required.");
                OnAutoDetectionFailed?.Invoke();
                
                // Automatically switch to manual after timeout
                SwitchToManualCalibration();
                return;
            }

            // If we have a valid plane, update grid position
            if (hasValidPlane && detectedTablePlane != null)
            {
                UpdateGridFromPlane();
            }
        }

        private void UpdateManualPlacement()
        {
            if (!manualPositioningActive) return;
            if (buttonGrid == null || indexFingerTip == null) return;

            CalculateManualGridTransform();

            // Apply smoothed position
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

        private void CalculateManualGridTransform()
        {
            if (indexFingerTip == null || mainCamera == null) return;

            // Position based on fingertip
            targetGridPosition = indexFingerTip.position + Vector3.up * verticalOffset;

            // Rotation: face the player
            Vector3 playerForward = mainCamera.transform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.001f)
            {
                playerForward = Vector3.forward;
            }
            playerForward.Normalize();

            targetGridRotation = Quaternion.LookRotation(playerForward, Vector3.up);
        }

        private void UpdateGridFromPlane()
        {
            if (buttonGrid == null || detectedTablePlane == null || mainCamera == null) return;

            // Position grid at plane center, in front of user
            Vector3 planeCenter = detectedTablePlane.center;
            
            // Calculate position that's visible to the user
            Vector3 userPosition = mainCamera.transform.position;
            Vector3 toPlane = planeCenter - userPosition;
            toPlane.y = 0;
            
            // Place grid on the plane, facing the user
            targetGridPosition = new Vector3(planeCenter.x, detectedTablePlane.center.y, planeCenter.z);

            // Rotation: face the player
            Vector3 playerForward = mainCamera.transform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.001f)
            {
                playerForward = Vector3.forward;
            }
            playerForward.Normalize();

            targetGridRotation = Quaternion.LookRotation(playerForward, Vector3.up);

            // Apply to grid
            buttonGrid.transform.position = targetGridPosition;
            buttonGrid.transform.rotation = targetGridRotation;
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (!isCalibrating || currentMode != CalibrationMode.Auto) return;

            // Check added and updated planes
            foreach (var plane in args.added)
            {
                EvaluatePlane(plane);
            }
            foreach (var plane in args.updated)
            {
                EvaluatePlane(plane);
            }
        }

        private void EvaluatePlane(ARPlane plane)
        {
            if (!IsValidTablePlane(plane)) return;

            float score = CalculatePlaneScore(plane);

            // If this is better than current best, use it
            if (detectedTablePlane == null || score > CalculatePlaneScore(detectedTablePlane))
            {
                detectedTablePlane = plane;
                hasValidPlane = true;

                SetIndicatorState(true, false, true);
                UpdateStatus($"Table detected! Height: {plane.center.y:F2}m. Press A to confirm or wait...");
                
                Debug.Log($"[SollertiaCalibration] Found valid table plane: {plane.trackableId}, " +
                         $"height={plane.center.y:F2}m, area={plane.size.x * plane.size.y:F2}m², " +
                         $"classification={plane.classification}");
            }
        }

        private bool IsValidTablePlane(ARPlane plane)
        {
            // Must be horizontal (table-like)
            if (plane.alignment != PlaneAlignment.HorizontalUp)
                return false;

            // Check minimum area
            float area = plane.size.x * plane.size.y;
            if (area < minTableArea)
                return false;

            // Check height range
            float height = plane.center.y;
            if (height < minTableHeight || height > maxTableHeight)
                return false;

            return true;
        }

        private float CalculatePlaneScore(ARPlane plane)
        {
            float score = 0f;

            // Area score (larger is better, up to a point)
            float area = plane.size.x * plane.size.y;
            score += Mathf.Min(area, 2f) * 10f;

            // Height score (prefer typical table height ~0.75m)
            float heightDiff = Mathf.Abs(plane.center.y - 0.75f);
            score += (1f - Mathf.Clamp01(heightDiff / 0.25f)) * 20f;

            // Classification bonus (if Meta Quest classified it as a table)
            if (preferClassifiedTables && plane.classification == PlaneClassification.Table)
            {
                score += 50f;
            }

            // Proximity to user bonus
            if (mainCamera != null)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, plane.center);
                score += Mathf.Max(0, 10f - distance) * 2f;
            }

            return score;
        }

        private void CompleteCalibration()
        {
            isCalibrating = false;
            currentMode = CalibrationMode.Completed;
            manualPositioningActive = false;

            // Disable plane detection
            if (planeManager != null)
            {
                planeManager.enabled = false;
            }

            // Hide indicators
            SetIndicatorState(false, false);

            Vector3 finalPosition = buttonGrid != null ? buttonGrid.transform.position : Vector3.zero;
            Quaternion finalRotation = buttonGrid != null ? buttonGrid.transform.rotation : Quaternion.identity;

            string resultStr = lastResult == CalibrationResult.AutoDetected ? "auto-detected" : "manually placed";
            UpdateStatus($"Calibration complete ({resultStr})");
            
            OnCalibrationCompleted?.Invoke(finalPosition, finalRotation);
            Debug.Log($"[SollertiaCalibration] Calibration completed via {resultStr} at {finalPosition}");
        }

        private void SetIndicatorState(bool autoIndicator, bool manualIndicator, bool valid = false)
        {
            if (autoDetectionIndicator != null)
            {
                autoDetectionIndicator.SetActive(autoIndicator);
                
                // Update color based on validity
                var renderer = autoDetectionIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = valid ? validSurfaceColor : searchingColor;
                }
            }

            if (manualPlacementIndicator != null)
            {
                manualPlacementIndicator.SetActive(manualIndicator);
            }
        }

        private void UpdateStatus(string message)
        {
            OnStatusChanged?.Invoke(message);
            Debug.Log($"[SollertiaCalibration] Status: {message}");
        }

        /// <summary>
        /// Check if Room Setup data is available (planes can be detected).
        /// Call this before starting calibration to inform the user.
        /// </summary>
        public bool CheckRoomSetupAvailable()
        {
            // On Quest, if planeManager has no trackables after a brief period,
            // Room Setup likely hasn't been completed
            if (planeManager == null) return false;
            
            // This is a heuristic - actual check would require Meta XR SDK
            return planeManager.trackables.count > 0;
        }

        /// <summary>
        /// Get instructions for the user based on current state.
        /// </summary>
        public string GetUserInstructions()
        {
            switch (currentMode)
            {
                case CalibrationMode.Auto:
                    if (hasValidPlane)
                        return "Table detected! Press A to confirm placement.";
                    else
                        return "Looking for table surface... Make sure Room Setup is complete.";
                
                case CalibrationMode.Manual:
                    if (manualPositioningActive)
                        return "Move your finger to position the grid. Press A to confirm.";
                    else
                        return "Press and hold grip to start positioning.";
                
                case CalibrationMode.Completed:
                    return "Calibration complete!";
                
                default:
                    return "";
            }
        }
    }
}
