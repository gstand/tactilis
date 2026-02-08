using UnityEngine;

/// <summary>
/// Attach this to a finger tip to give it a visual color indicator.
/// Create a small sphere/orb child object and assign it as the visual indicator.
/// </summary>
public class FingerColorIndicator : MonoBehaviour {
    public enum FingerColorType { Blue, Red }
    
    [Header("Settings")]
    public FingerColorType fingerColor = FingerColorType.Blue;
    
    [Header("Visual Indicator")]
    public GameObject colorIndicator;  // A small sphere attached to fingertip
    public float indicatorSize = 0.015f;  // Size of the indicator sphere
    
    [Header("Colors")]
    public Color blueColor = new Color(0.2f, 0.4f, 1f, 0.8f);
    public Color redColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    
    private Renderer indicatorRenderer;
    
    void Start() {
        SetupIndicator();
    }
    
    void SetupIndicator() {
        // If no indicator assigned, create one
        if (colorIndicator == null) {
            colorIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            colorIndicator.name = "ColorIndicator";
            colorIndicator.transform.SetParent(transform);
            colorIndicator.transform.localPosition = Vector3.zero;
            colorIndicator.transform.localScale = Vector3.one * indicatorSize;
            
            // Remove collider so it doesn't interfere
            Collider col = colorIndicator.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
        
        indicatorRenderer = colorIndicator.GetComponent<Renderer>();
        
        if (indicatorRenderer != null) {
            // Create a new material for this indicator
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = fingerColor == FingerColorType.Blue ? blueColor : redColor;
            
            // Make it glow/emissive
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", mat.color * 2f);
            
            indicatorRenderer.material = mat;
        }
    }
    
    public void SetColor(FingerColorType newColor) {
        fingerColor = newColor;
        
        if (indicatorRenderer != null) {
            Color targetColor = fingerColor == FingerColorType.Blue ? blueColor : redColor;
            indicatorRenderer.material.color = targetColor;
            indicatorRenderer.material.SetColor("_EmissionColor", targetColor * 2f);
        }
    }
}
