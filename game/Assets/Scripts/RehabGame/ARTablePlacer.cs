using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles AR table detection and grid placement for Quest 3 Mixed Reality.
/// Uses AR Foundation plane detection to find horizontal surfaces (tables).
/// </summary>
[RequireComponent(typeof(ARPlaneManager))]
public class ARTablePlacer : MonoBehaviour
{
    [Header("References")]
    public RehabGrid gridPrefab;
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    [Header("Placement Settings")]
    [Tooltip("Minimum table size in meters (width x depth)")]
    public Vector2 minTableSize = new Vector2(0.3f, 0.2f);
    [Tooltip("Automatically place grid on first suitable table detected")]
    public bool autoPlaceOnDetection = true;
    [Tooltip("Allow repositioning after initial placement")]
    public bool allowRepositioning = true;

    [Header("Visual Feedback")]
    public GameObject placementIndicator;
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    [Header("Events")]
    public UnityEvent<RehabGrid> onGridPlaced;
    public UnityEvent onTableDetected;
    public UnityEvent onNoTableFound;

    public bool IsGridPlaced => _placedGrid != null;
    public RehabGrid PlacedGrid => _placedGrid;

    RehabGrid _placedGrid;
    ARPlane _selectedPlane;
    bool _isPlacing;
    List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();

    void Awake()
    {
        if (!planeManager)
            planeManager = GetComponent<ARPlaneManager>();

        if (!raycastManager)
            raycastManager = GetComponent<ARRaycastManager>();
    }

    void OnEnable()
    {
        if (planeManager)
            planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        if (planeManager)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Check newly added planes for suitable tables
        foreach (var plane in args.added)
        {
            if (IsSuitableTable(plane))
            {
                onTableDetected?.Invoke();

                if (autoPlaceOnDetection && !IsGridPlaced)
                {
                    PlaceGridOnPlane(plane);
                }
            }
        }

        // Also check updated planes
        foreach (var plane in args.updated)
        {
            if (IsSuitableTable(plane) && autoPlaceOnDetection && !IsGridPlaced)
            {
                PlaceGridOnPlane(plane);
            }
        }
    }

    bool IsSuitableTable(ARPlane plane)
    {
        // Must be horizontal and facing up
        if (plane.alignment != PlaneAlignment.HorizontalUp)
            return false;

        // Check classification if available (Quest 3 supports this)
        if (plane.classification == PlaneClassification.Table)
            return true;

        // Fallback: check size
        Vector2 size = plane.size;
        return size.x >= minTableSize.x && size.y >= minTableSize.y;
    }

    /// <summary>
    /// Place the grid on a specific AR plane
    /// </summary>
    public void PlaceGridOnPlane(ARPlane plane)
    {
        if (_placedGrid != null && !allowRepositioning)
            return;

        // Remove existing grid if repositioning
        if (_placedGrid != null)
        {
            Destroy(_placedGrid.gameObject);
        }

        _selectedPlane = plane;

        // Instantiate grid at plane center
        Vector3 position = plane.center;
        Quaternion rotation = Quaternion.identity; // Grid lies flat

        if (gridPrefab)
        {
            _placedGrid = Instantiate(gridPrefab, position, rotation);
        }
        else
        {
            // Create grid dynamically if no prefab
            GameObject gridObj = new GameObject("RehabGrid");
            gridObj.transform.position = position;
            gridObj.transform.rotation = rotation;
            _placedGrid = gridObj.AddComponent<RehabGrid>();
        }

        // Parent to plane for tracking (optional - may cause issues if plane updates)
        // _placedGrid.transform.SetParent(plane.transform);

        Debug.Log($"[ARTablePlacer] Grid placed on table at {position}");
        onGridPlaced?.Invoke(_placedGrid);

        // Hide placement indicator
        if (placementIndicator)
            placementIndicator.SetActive(false);
    }

    /// <summary>
    /// Place grid at a specific world position (manual placement)
    /// </summary>
    public void PlaceGridAtPosition(Vector3 position, Quaternion rotation)
    {
        if (_placedGrid != null && !allowRepositioning)
            return;

        if (_placedGrid != null)
        {
            Destroy(_placedGrid.gameObject);
        }

        if (gridPrefab)
        {
            _placedGrid = Instantiate(gridPrefab, position, rotation);
        }
        else
        {
            GameObject gridObj = new GameObject("RehabGrid");
            gridObj.transform.position = position;
            gridObj.transform.rotation = rotation;
            _placedGrid = gridObj.AddComponent<RehabGrid>();
        }

        onGridPlaced?.Invoke(_placedGrid);
    }

    /// <summary>
    /// Start manual placement mode (user points at table)
    /// </summary>
    public void StartManualPlacement()
    {
        _isPlacing = true;
        if (placementIndicator)
            placementIndicator.SetActive(true);
    }

    /// <summary>
    /// Confirm manual placement at current indicator position
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!_isPlacing || placementIndicator == null)
            return;

        PlaceGridAtPosition(placementIndicator.transform.position, placementIndicator.transform.rotation);
        _isPlacing = false;
    }

    /// <summary>
    /// Cancel manual placement
    /// </summary>
    public void CancelPlacement()
    {
        _isPlacing = false;
        if (placementIndicator)
            placementIndicator.SetActive(false);
    }

    void Update()
    {
        if (!_isPlacing || raycastManager == null)
            return;

        // Raycast from camera center to find table surface
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, _raycastHits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = _raycastHits[0];
            ARPlane plane = planeManager.GetPlane(hit.trackableId);

            if (plane != null && IsSuitableTable(plane))
            {
                // Valid placement
                if (placementIndicator)
                {
                    placementIndicator.SetActive(true);
                    placementIndicator.transform.position = hit.pose.position;
                    placementIndicator.transform.rotation = hit.pose.rotation;

                    if (validPlacementMaterial)
                    {
                        var renderer = placementIndicator.GetComponent<MeshRenderer>();
                        if (renderer)
                            renderer.material = validPlacementMaterial;
                    }
                }
            }
            else
            {
                // Invalid surface
                if (placementIndicator && invalidPlacementMaterial)
                {
                    var renderer = placementIndicator.GetComponent<MeshRenderer>();
                    if (renderer)
                        renderer.material = invalidPlacementMaterial;
                }
            }
        }
        else
        {
            // No surface found
            if (placementIndicator)
                placementIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Remove placed grid and reset
    /// </summary>
    public void RemoveGrid()
    {
        if (_placedGrid != null)
        {
            Destroy(_placedGrid.gameObject);
            _placedGrid = null;
        }
        _selectedPlane = null;
    }

    /// <summary>
    /// Get all detected table planes
    /// </summary>
    public List<ARPlane> GetDetectedTables()
    {
        List<ARPlane> tables = new List<ARPlane>();

        foreach (var plane in planeManager.trackables)
        {
            if (IsSuitableTable(plane))
                tables.Add(plane);
        }

        return tables;
    }
}
