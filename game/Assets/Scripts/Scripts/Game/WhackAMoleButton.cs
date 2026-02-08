using UnityEngine;

public class WhackAMoleButton : MonoBehaviour {
    public enum ButtonColor { None, Blue, Red }
    
    [Header("Visual References")]
    public Renderer buttonRenderer;
    public Light buttonLight;  // Optional: for glow effect
    
    [Header("Colors")]
    public Color blueColor = new Color(0.2f, 0.4f, 1f);
    public Color redColor = new Color(1f, 0.2f, 0.2f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f);
    
    private ButtonColor currentColor = ButtonColor.None;
    private bool isActive = false;
    private float activationTime;
    private float timeLimit;
    
    public bool IsActive => isActive;
    public ButtonColor CurrentColor => currentColor;
    public float TimeRemaining => isActive ? Mathf.Max(0, timeLimit - (Time.time - activationTime)) : 0;
    
    void Start() {
        if (buttonRenderer == null) {
            buttonRenderer = GetComponent<Renderer>();
        }
        Deactivate();
    }
    
    public void Activate(ButtonColor color, float duration) {
        currentColor = color;
        isActive = true;
        activationTime = Time.time;
        timeLimit = duration;
        
        // Set visual color
        Color targetColor = color == ButtonColor.Blue ? blueColor : redColor;
        
        if (buttonRenderer != null) {
            buttonRenderer.material.color = targetColor;
            buttonRenderer.material.SetColor("_EmissionColor", targetColor * 2f);
        }
        
        if (buttonLight != null) {
            buttonLight.enabled = true;
            buttonLight.color = targetColor;
        }
    }
    
    public void Deactivate() {
        currentColor = ButtonColor.None;
        isActive = false;
        
        if (buttonRenderer != null) {
            buttonRenderer.material.color = inactiveColor;
            buttonRenderer.material.SetColor("_EmissionColor", Color.black);
        }
        
        if (buttonLight != null) {
            buttonLight.enabled = false;
        }
    }
    
    public bool HasTimedOut() {
        return isActive && (Time.time - activationTime) >= timeLimit;
    }
}
