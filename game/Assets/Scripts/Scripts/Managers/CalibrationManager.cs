using UnityEngine;

public class CalibrationManager : MonoBehaviour {
    [Header("References")]
    public GameObject buttonGrid;           // The grid/table that holds all buttons
    public WhackAMoleGameManager gameManager;  // Reference to the game manager
    
    [Header("Positioning")]
    public float distanceInFront = 0.6f;    // How far in front of player (meters)
    public float tableHeight = 0.8f;        // Height of the table surface
    
    private bool isPositioned = false;

    void Start() {
        // Position the button grid in front of the player ONCE at start
        PositionGridInFrontOfPlayer();
    }
    
    void PositionGridInFrontOfPlayer() {
        if (buttonGrid == null || Camera.main == null) return;
        
        // Get player's forward direction (ignore vertical tilt)
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        // Position grid in front of player
        Vector3 playerPos = Camera.main.transform.position;
        Vector3 gridPosition = new Vector3(
            playerPos.x + forward.x * distanceInFront,
            tableHeight,
            playerPos.z + forward.z * distanceInFront
        );
        
        buttonGrid.transform.position = gridPosition;
        
        // Make grid face the player
        buttonGrid.transform.rotation = Quaternion.LookRotation(-forward);
        
        isPositioned = true;
        Debug.Log($"[CalibrationManager] Button grid positioned at: {gridPosition}");
    }
    
    // Call this to reposition the grid (can hook up to a controller button)
    public void RepositionGrid() {
        PositionGridInFrontOfPlayer();
    }
    
    // Draw the grid position in editor
    void OnDrawGizmos() {
        if (Camera.main == null) return;
        
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 playerPos = Camera.main.transform.position;
        Vector3 center = new Vector3(
            playerPos.x + forward.x * distanceInFront,
            tableHeight,
            playerPos.z + forward.z * distanceInFront
        );
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, new Vector3(0.3f, 0.01f, 0.3f));
    }
}