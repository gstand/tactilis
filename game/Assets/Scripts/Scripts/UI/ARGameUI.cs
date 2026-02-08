using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR-friendly UI that follows the player's view.
/// Includes auto-sizing background for rules text and persistent score display.
/// </summary>
public class ARGameUI : MonoBehaviour {
    [Header("UI Canvas Settings")]
    public Canvas uiCanvas;
    public float distanceFromCamera = 2f;      // How far the UI is from the camera
    public float heightOffset = 0.3f;           // Height above eye level
    public float followSpeed = 5f;              // How fast UI follows player view
    
    [Header("Rules Panel")]
    public RectTransform rulesPanel;            // The background panel
    public TextMeshProUGUI rulesText;           // The rules text
    public Image rulesPanelBackground;          // Background image component
    public float paddingHorizontal = 40f;       // Padding around text
    public float paddingVertical = 30f;
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    
    [Header("Score Display (Top Right)")]
    public RectTransform scorePanel;
    public TextMeshProUGUI scoreText;
    public Image scorePanelBackground;
    public Vector2 scoreAnchorOffset = new Vector2(-100f, -50f);  // Offset from top-right in local units
    
    [Header("Timer Display")]
    public RectTransform timerPanel;
    public TextMeshProUGUI timerText;
    public Image timerPanelBackground;
    
    [Header("Game Over Panel")]
    public RectTransform gameOverPanel;
    public TextMeshProUGUI gameOverText;
    
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    void Start() {
        // Ensure canvas is in world space for AR
        if (uiCanvas != null) {
            uiCanvas.renderMode = RenderMode.WorldSpace;
            uiCanvas.transform.localScale = Vector3.one * 0.001f; // Scale down for world space
        }
        
        // Set up backgrounds
        if (rulesPanelBackground != null) {
            rulesPanelBackground.color = backgroundColor;
        }
        if (scorePanelBackground != null) {
            scorePanelBackground.color = backgroundColor;
        }
        if (timerPanelBackground != null) {
            timerPanelBackground.color = backgroundColor;
        }
        
        // Hide game over initially
        if (gameOverPanel != null) {
            gameOverPanel.gameObject.SetActive(false);
        }
        
        // Initial positioning
        UpdateTargetPosition();
        if (uiCanvas != null) {
            uiCanvas.transform.position = targetPosition;
            uiCanvas.transform.rotation = targetRotation;
        }
    }
    
    void LateUpdate() {
        UpdateTargetPosition();
        
        // Smoothly follow the player
        if (uiCanvas != null) {
            uiCanvas.transform.position = Vector3.Lerp(
                uiCanvas.transform.position, 
                targetPosition, 
                Time.deltaTime * followSpeed
            );
            uiCanvas.transform.rotation = Quaternion.Slerp(
                uiCanvas.transform.rotation, 
                targetRotation, 
                Time.deltaTime * followSpeed
            );
        }
        
        // Auto-size the panel backgrounds
        UpdateRulesPanelSize();
        UpdateScorePanelSize();
        UpdateTimerPanelSize();
        
        // Position score panel in top-right area
        PositionScorePanel();
    }
    
    void UpdateTargetPosition() {
        if (Camera.main == null) return;
        
        Vector3 forward = Camera.main.transform.forward;
        targetPosition = Camera.main.transform.position + forward * distanceFromCamera;
        targetPosition.y += heightOffset;
        
        // Face the camera
        targetRotation = Quaternion.LookRotation(forward);
    }
    
    void UpdateRulesPanelSize() {
        if (rulesPanel == null || rulesText == null) return;
        
        // Force text to update its preferred values
        rulesText.ForceMeshUpdate();
        
        // Calculate new size based on text
        Vector2 textSize = new Vector2(
            rulesText.preferredWidth + paddingHorizontal * 2,
            rulesText.preferredHeight + paddingVertical * 2
        );
        
        // Apply to panel
        rulesPanel.sizeDelta = textSize;
    }
    
    void UpdateScorePanelSize() {
        if (scorePanel == null || scoreText == null) return;
        
        scoreText.ForceMeshUpdate();
        
        Vector2 textSize = new Vector2(
            scoreText.preferredWidth + paddingHorizontal,
            scoreText.preferredHeight + paddingVertical
        );
        
        scorePanel.sizeDelta = textSize;
    }
    
    void UpdateTimerPanelSize() {
        if (timerPanel == null || timerText == null) return;
        
        timerText.ForceMeshUpdate();
        
        Vector2 textSize = new Vector2(
            timerText.preferredWidth + paddingHorizontal,
            timerText.preferredHeight + paddingVertical
        );
        
        timerPanel.sizeDelta = textSize;
    }
    
    void PositionScorePanel() {
        if (scorePanel == null || uiCanvas == null) return;
        
        // Position score panel at top-right of canvas
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (canvasRect != null) {
            // Set anchors to top-right
            scorePanel.anchorMin = new Vector2(1, 1);
            scorePanel.anchorMax = new Vector2(1, 1);
            scorePanel.pivot = new Vector2(1, 1);
            scorePanel.anchoredPosition = scoreAnchorOffset;
        }
    }
    
    // Public methods to update UI
    public void SetRulesText(string text) {
        if (rulesText != null) {
            rulesText.text = text;
        }
    }
    
    public void ShowRules(bool show) {
        if (rulesPanel != null) {
            rulesPanel.gameObject.SetActive(show);
        }
    }
    
    public void SetScore(int score) {
        if (scoreText != null) {
            scoreText.text = $"Score: {score}";
        }
    }
    
    public void SetTimer(float timeRemaining) {
        if (timerText != null) {
            timerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}";
        }
    }
    
    public void ShowGameOver(int finalScore) {
        if (gameOverPanel != null) {
            gameOverPanel.gameObject.SetActive(true);
        }
        if (gameOverText != null) {
            gameOverText.text = $"Game Over!\n\nFinal Score: {finalScore}";
        }
        ShowRules(false);
    }
    
    public void HideGameOver() {
        if (gameOverPanel != null) {
            gameOverPanel.gameObject.SetActive(false);
        }
    }
}
