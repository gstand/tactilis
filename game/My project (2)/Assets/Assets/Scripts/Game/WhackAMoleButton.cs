using UnityEngine;

public class WhackAMoleButton : MonoBehaviour {
    public enum ButtonColor { None, Blue, Red }
    
    [Header("Visual References")]
    public Renderer buttonRenderer;
    public Light buttonLight;  // Optional: for glow effect
    public float lightIntensity = 2f;
    
    [Header("Colors")]
    public Color blueColor = new Color(0.2f, 0.4f, 1f);
    public Color redColor = new Color(1f, 0.2f, 0.2f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Materials (Optional)")]
    public bool useMaterials = false;
    public Material blueMaterial;
    public Material redMaterial;
    public Material inactiveMaterial;
    public float emissionIntensity = 2f;
    
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
        ApplyVisuals(targetColor, color == ButtonColor.Blue ? blueMaterial : redMaterial);
    }
    
    public void Deactivate() {
        currentColor = ButtonColor.None;
        isActive = false;
        ApplyVisuals(inactiveColor, inactiveMaterial, true);
    }
    
    public bool HasTimedOut() {
        return isActive && (Time.time - activationTime) >= timeLimit;
    }

    void ApplyVisuals(Color targetColor, Material targetMaterial, bool isInactive = false) {
        if (buttonRenderer != null) {
            if (useMaterials && targetMaterial != null) {
                buttonRenderer.material = targetMaterial;
            } else {
                buttonRenderer.material.color = targetColor;
            }

            Color emissionColor = isInactive ? Color.black : targetColor * emissionIntensity;
            Material mat = buttonRenderer.material;
            if (mat != null && mat.HasProperty("_EmissionColor")) {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
        }

        if (buttonLight != null) {
            buttonLight.enabled = !isInactive;
            if (!isInactive) {
                buttonLight.color = targetColor;
                buttonLight.intensity = lightIntensity;
            }
        }
    }
}
