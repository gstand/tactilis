using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the 2x3 grid of cells for the stroke rehab game.
/// Handles grid creation, cell spawning logic, and coordinate mapping.
/// </summary>
public class RehabGrid : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int rows = 2;
    public int columns = 3;
    public float cellSize = 0.1f; // meters
    public float cellSpacing = 0.01f; // gap between cells

    [Header("Cell Prefab")]
    [Tooltip("Prefab for grid cells. If null, creates simple cubes.")]
    public GameObject cellPrefab;

    [Header("Visual Settings")]
    public Material cellMaterial;
    public float cellHeight = 0.02f;

    [Header("Events")]
    public UnityEvent<GridCell> onCellActivated;
    public UnityEvent<GridCell, float> onCellHit; // cell, reaction time
    public UnityEvent<GridCell> onCellMissed;

    public GridCell[] Cells => _cells;
    public int TotalCells => rows * columns;
    public Vector3 GridCenter => transform.position;
    public Vector2 GridSize => new Vector2(
        columns * cellSize + (columns - 1) * cellSpacing,
        rows * cellSize + (rows - 1) * cellSpacing
    );

    GridCell[] _cells;
    List<int> _activeCellIndices = new List<int>();

    void Awake()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        _cells = new GridCell[rows * columns];

        float totalWidth = columns * cellSize + (columns - 1) * cellSpacing;
        float totalHeight = rows * cellSize + (rows - 1) * cellSpacing;

        Vector3 startPos = transform.position - new Vector3(totalWidth / 2f, 0, totalHeight / 2f);
        startPos += new Vector3(cellSize / 2f, 0, cellSize / 2f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int index = r * columns + c;

                Vector3 cellPos = startPos + new Vector3(
                    c * (cellSize + cellSpacing),
                    0,
                    r * (cellSize + cellSpacing)
                );

                GameObject cellObj;
                if (cellPrefab)
                {
                    cellObj = Instantiate(cellPrefab, cellPos, Quaternion.identity, transform);
                }
                else
                {
                    cellObj = CreateDefaultCell(cellPos);
                }

                cellObj.name = $"Cell_{r}_{c}";

                GridCell cell = cellObj.GetComponent<GridCell>();
                if (!cell)
                    cell = cellObj.AddComponent<GridCell>();

                cell.row = r;
                cell.column = c;

                // Wire up events
                cell.onActivated.AddListener(() => onCellActivated?.Invoke(cell));
                cell.onHit.AddListener(() =>
                {
                    _activeCellIndices.Remove(index);
                });
                cell.onMissed.AddListener(() =>
                {
                    _activeCellIndices.Remove(index);
                    onCellMissed?.Invoke(cell);
                });

                _cells[index] = cell;
            }
        }
    }

    GameObject CreateDefaultCell(Vector3 position)
    {
        GameObject cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cellObj.transform.SetParent(transform);
        cellObj.transform.position = position;
        cellObj.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);

        if (cellMaterial)
        {
            cellObj.GetComponent<MeshRenderer>().material = cellMaterial;
        }

        // Add collider for hit detection
        BoxCollider col = cellObj.GetComponent<BoxCollider>();
        if (!col)
            col = cellObj.AddComponent<BoxCollider>();
        col.isTrigger = true;

        return cellObj;
    }

    /// <summary>
    /// Activate a random inactive cell
    /// </summary>
    /// <returns>The activated cell, or null if all cells are active</returns>
    public GridCell ActivateRandomCell()
    {
        List<int> inactiveIndices = new List<int>();
        for (int i = 0; i < _cells.Length; i++)
        {
            if (!_cells[i].IsActive)
                inactiveIndices.Add(i);
        }

        if (inactiveIndices.Count == 0)
            return null;

        int randomIndex = inactiveIndices[Random.Range(0, inactiveIndices.Count)];
        _cells[randomIndex].Activate();
        _activeCellIndices.Add(randomIndex);

        return _cells[randomIndex];
    }

    /// <summary>
    /// Activate a specific cell by index
    /// </summary>
    public GridCell ActivateCell(int index)
    {
        if (index < 0 || index >= _cells.Length)
            return null;

        if (_cells[index].IsActive)
            return null;

        _cells[index].Activate();
        _activeCellIndices.Add(index);
        return _cells[index];
    }

    /// <summary>
    /// Get the cell at a specific row and column
    /// </summary>
    public GridCell GetCell(int row, int column)
    {
        if (row < 0 || row >= rows || column < 0 || column >= columns)
            return null;

        return _cells[row * columns + column];
    }

    /// <summary>
    /// Get the cell at a specific index (0-5 for 2x3)
    /// </summary>
    public GridCell GetCell(int index)
    {
        if (index < 0 || index >= _cells.Length)
            return null;

        return _cells[index];
    }

    /// <summary>
    /// Find which cell contains a world position
    /// </summary>
    /// <returns>The cell containing the point, or null if outside grid</returns>
    public GridCell GetCellAtPosition(Vector3 worldPosition)
    {
        foreach (var cell in _cells)
        {
            if (cell.ContainsPoint(worldPosition))
                return cell;
        }
        return null;
    }

    /// <summary>
    /// Deactivate all cells
    /// </summary>
    public void DeactivateAll()
    {
        foreach (var cell in _cells)
        {
            if (cell.IsActive)
                cell.Deactivate();
        }
        _activeCellIndices.Clear();
    }

    /// <summary>
    /// Mark all active cells as missed
    /// </summary>
    public void TimeoutAllActive()
    {
        foreach (int index in _activeCellIndices.ToArray())
        {
            _cells[index].RegisterMiss();
        }
        _activeCellIndices.Clear();
    }

    /// <summary>
    /// Get all currently active cells
    /// </summary>
    public List<GridCell> GetActiveCells()
    {
        List<GridCell> active = new List<GridCell>();
        foreach (int index in _activeCellIndices)
        {
            active.Add(_cells[index]);
        }
        return active;
    }

    /// <summary>
    /// Check if any cells are currently active
    /// </summary>
    public bool HasActiveCells()
    {
        return _activeCellIndices.Count > 0;
    }

    void OnDrawGizmosSelected()
    {
        // Draw grid bounds
        Gizmos.color = Color.cyan;
        Vector2 size = GridSize;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.01f, size.y));

        // Draw cell positions
        if (_cells != null)
        {
            foreach (var cell in _cells)
            {
                Gizmos.color = cell.IsActive ? Color.green : Color.gray;
                Gizmos.DrawWireCube(cell.transform.position, new Vector3(cellSize, cellHeight, cellSize));
            }
        }
    }
}
