using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// </summary>
    [CreateAssetMenu(fileName = "BlueColorScheme", menuName = "GameUI/Color Scheme")]
    public class UIColorScheme : ScriptableObject
    {
        [Header("Primary Colors")]
        [Tooltip("Main blue color for primary elements")]
        public Color primaryBlue = new Color(0.2f, 0.5f, 0.8f, 1f);
        
        [Tooltip("Lighter blue for text and highlights")]
        public Color lightBlue = new Color(0.4f, 0.7f, 0.95f, 1f);
        
        [Tooltip("Darker blue for backgrounds and shadows")]
        public Color darkBlue = new Color(0.1f, 0.25f, 0.45f, 1f);

        [Header("Panel Colors")]
        [Tooltip("Semi-transparent panel background")]
        public Color panelBackground = new Color(0.12f, 0.18f, 0.28f, 0.95f);
        
        [Tooltip("Panel border/outline color")]
        public Color panelBorder = new Color(0.3f, 0.5f, 0.7f, 0.8f);

        [Header("Text Colors")]
        [Tooltip("Primary text color")]
        public Color textPrimary = new Color(0.6f, 0.8f, 0.95f, 1f);
        
        [Tooltip("Secondary/label text color")]
        public Color textSecondary = new Color(0.5f, 0.65f, 0.8f, 0.8f);

        [Header("Button Colors")]
        [Tooltip("Button background color")]
        public Color buttonNormal = new Color(0.25f, 0.55f, 0.8f, 1f);
        
        [Tooltip("Button hover color")]
        public Color buttonHover = new Color(0.35f, 0.65f, 0.9f, 1f);
        
        [Tooltip("Button pressed color")]
        public Color buttonPressed = new Color(0.15f, 0.45f, 0.7f, 1f);
        
        [Tooltip("Button text color")]
        public Color buttonText = Color.white;

        [Header("Accent Colors")]
        [Tooltip("Warning color for low time")]
        public Color warning = new Color(0.9f, 0.6f, 0.3f, 1f);
        
        [Tooltip("Success color")]
        public Color success = new Color(0.3f, 0.8f, 0.5f, 1f);

        /// <summary>
        /// Returns a color with modified alpha.
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Returns a lighter version of the color.
        /// </summary>
        public static Color Lighten(Color color, float amount = 0.1f)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                color.a
            );
        }

        /// <summary>
        /// Returns a darker version of the color.
        /// </summary>
        public static Color Darken(Color color, float amount = 0.1f)
        {
            return new Color(
                Mathf.Max(0f, color.r - amount),
                Mathf.Max(0f, color.g - amount),
                Mathf.Max(0f, color.b - amount),
                color.a
            );
        }
    }
}
