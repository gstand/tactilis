using UnityEngine;

public class FingerTipVisual : MonoBehaviour {
    public enum FingerColor { Blue, Red }

    [Header("Visual References")]
    public Renderer fingerRenderer;
    public Light fingerLight;

    [Header("Settings")]
    public FingerColor fingerColor = FingerColor.Blue;
    public Color blueColor = new Color(0.2f, 0.4f, 1f);
    public Color redColor = new Color(1f, 0.2f, 0.2f);
    public float emissionIntensity = 2f;
    public float lightIntensity = 2f;

    void Start() {
        if (fingerRenderer == null) {
            fingerRenderer = GetComponent<Renderer>();
        }
        ApplyVisuals();
    }

    void ApplyVisuals() {
        Color targetColor = fingerColor == FingerColor.Blue ? blueColor : redColor;

        if (fingerRenderer != null) {
            Material mat = fingerRenderer.material;
            if (mat != null) {
                mat.color = targetColor;
                if (mat.HasProperty("_EmissionColor")) {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", targetColor * emissionIntensity);
                }
            }
        }

        if (fingerLight != null) {
            fingerLight.color = targetColor;
            fingerLight.intensity = lightIntensity;
            fingerLight.enabled = true;
        }
    }
}
