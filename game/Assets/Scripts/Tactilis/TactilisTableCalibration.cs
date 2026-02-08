using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

namespace Tactilis
{
    /// <summary>
    /// TactilisTableCalibration - Detects and selects table surfaces using AR Foundation.
    /// On Meta Quest 3, uses Room Setup data for plane detection.
    /// Falls back to manual height setting in Editor.
    /// </summary>
    public class TactilisTableCalibration : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== AR REFERENCES ===")]
        [Tooltip("AR Plane Manager for detecting surfaces (optional - auto-found)")]
        public ARPlaneManager planeManager;

        [Tooltip("AR Raycast Manager for selecting surfaces (optional - auto-found)")]
        public ARRaycastManager raycastManager;

        [Header("=== TABLE SETTINGS ===")]
        [Tooltip("Default table height if no AR plane detected (meters)")]
        public float defaultTableHeight = 0.75f;

        [Tooltip("Only accept horizontal planes")]
        public bool horizontalOnly = true;

        [Tooltip("Minimum plane area to be considered a table (m²)")]
        public float minPlaneArea = 0.1f;

        [Header("=== VISUAL FEEDBACK ===")]
        [Tooltip("Prefab to show where table is detected (optional)")]
        public GameObject tableSurfaceIndicator;

        [Tooltip("Color for valid table surface")]
        public Color validSurfaceColor = new Color(0.2f, 0.8f, 0.4f, 0.3f);

        [Tooltip("Color for invalid surface")]
        public Color invalidSurfaceColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);

        [Header("=== EVENTS ===")]
        public UnityEvent<Vector3, float> OnTableSelected;
        public UnityEvent OnCalibrationStarted;
        public UnityEvent OnCalibrationCancelled;
        #endregion

        #region Private State
        [Header("=== DEBUG (Read Only) ===")]
        [SerializeField] private bool isCalibrating = false;
        [SerializeField] private float detectedTableHeight = 0.75f;
        [SerializeField] private Vector3 detectedTablePosition = Vector3.zero;
        [SerializeField] private bool hasValidTable = false;

        private ARPlane currentPlane;
        private GameObject indicatorInstance;
        #endregion

        #region Properties
        public bool IsCalibrating => isCalibrating;
        public float TableHeight => hasValidTable ? detectedTableHeight : defaultTableHeight;
        public Vector3 TablePosition => detectedTablePosition;
        public bool HasValidTable => hasValidTable;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Auto-find AR components
            if (planeManager == null) planeManager = FindFirstObjectByType<ARPlaneManager>();
            if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();

            // Create indicator if prefab provided
            if (tableSurfaceIndicator != null)
            {
                indicatorInstance = Instantiate(tableSurfaceIndicator);
                indicatorInstance.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isCalibrating) return;

#if UNITY_EDITOR
            UpdateEditorCalibration();
#else
            UpdateARCalibration();
#endif
        }
        #endregion

        #region Public Methods
        public void StartCalibration()
        {
            isCalibrating = true;
            hasValidTable = false;

            // Enable plane detection
            if (planeManager != null)
            {
                planeManager.enabled = true;
            }

            if (indicatorInstance != null)
            {
                indicatorInstance.SetActive(true);
            }

            OnCalibrationStarted?.Invoke();
            Debug.Log("[TactilisTableCalibration] Calibration started");
        }

        public void CancelCalibration()
        {
            isCalibrating = false;
            hasValidTable = false;

            if (indicatorInstance != null)
            {
                indicatorInstance.SetActive(false);
            }

            OnCalibrationCancelled?.Invoke();
            Debug.Log("[TactilisTableCalibration] Calibration cancelled");
        }

        public void ConfirmTableSelection()
        {
            if (!hasValidTable)
            {
                Debug.LogWarning("[TactilisTableCalibration] No valid table to confirm");
                return;
            }

            isCalibrating = false;

            // Disable plane detection to save resources
            if (planeManager != null)
            {
                planeManager.enabled = false;
            }

            if (indicatorInstance != null)
            {
                indicatorInstance.SetActive(false);
            }

            OnTableSelected?.Invoke(detectedTablePosition, detectedTableHeight);
            Debug.Log($"[TactilisTableCalibration] Table confirmed at height {detectedTableHeight}m");
        }

        public void SetTableManually(Vector3 position, float height)
        {
            detectedTablePosition = position;
            detectedTableHeight = height;
            hasValidTable = true;

            OnTableSelected?.Invoke(position, height);
            Debug.Log($"[TactilisTableCalibration] Table set manually at height {height}m");
        }
        #endregion

        #region AR Calibration
        private void UpdateARCalibration()
        {
            if (planeManager == null) return;

            // Find the best horizontal plane (likely a table)
            ARPlane bestPlane = null;
            float bestScore = 0f;

            foreach (var plane in planeManager.trackables)
            {
                if (!IsValidTablePlane(plane)) continue;

                // Score based on size and height (tables are typically 0.6-0.9m)
                float area = plane.size.x * plane.size.y;
                float heightScore = 1f - Mathf.Abs(plane.center.y - 0.75f) / 0.5f;
                float score = area * Mathf.Max(0.1f, heightScore);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPlane = plane;
                }
            }

            if (bestPlane != null)
            {
                currentPlane = bestPlane;
                detectedTablePosition = bestPlane.center;
                detectedTableHeight = bestPlane.center.y;
                hasValidTable = true;

                UpdateIndicator(detectedTablePosition, true);
            }
            else
            {
                hasValidTable = false;
                if (indicatorInstance != null)
                {
                    indicatorInstance.SetActive(false);
                }
            }
        }

        private bool IsValidTablePlane(ARPlane plane)
        {
            // Check alignment
            if (horizontalOnly && plane.alignment != UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp)
            {
                return false;
            }

            // Check size
            float area = plane.size.x * plane.size.y;
            if (area < minPlaneArea)
            {
                return false;
            }

            // Check height (tables are typically between 0.5m and 1.0m)
            if (plane.center.y < 0.4f || plane.center.y > 1.2f)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Editor Calibration
        private void UpdateEditorCalibration()
        {
            // In editor, use mouse position on a virtual table plane
            if (Camera.main == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            Plane tablePlane = new Plane(Vector3.up, new Vector3(0, defaultTableHeight, 0));

            if (tablePlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                detectedTablePosition = hitPoint;
                detectedTableHeight = defaultTableHeight;
                hasValidTable = true;

                UpdateIndicator(hitPoint, true);
            }
        }
        #endregion

        #region Visual Feedback
        private void UpdateIndicator(Vector3 position, bool isValid)
        {
            if (indicatorInstance == null) return;

            indicatorInstance.SetActive(true);
            indicatorInstance.transform.position = position;

            // Update color
            Renderer rend = indicatorInstance.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = isValid ? validSurfaceColor : invalidSurfaceColor;
            }
        }
        #endregion
    }
}
