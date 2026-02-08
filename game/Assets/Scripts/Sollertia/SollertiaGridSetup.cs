using UnityEngine;

namespace Sollertia
{
    /// <summary>
    /// Creates a grid of buttons at runtime.
    /// Attach to an empty GameObject where you want the grid.
    /// </summary>
    public class SollertiaGridSetup : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int rows = 3;
        public int columns = 3;
        public float buttonSpacing = 0.1f;
        public float buttonSize = 0.08f;
        
        [Header("Button Appearance")]
        public Material buttonMaterial;
        
        private SollertiaButton[] createdButtons;
        
        public SollertiaButton[] Buttons => createdButtons;
        
        private void Awake()
        {
            CreateGrid();
        }
        
        public void CreateGrid()
        {
            createdButtons = new SollertiaButton[rows * columns];
            
            float startX = -(columns - 1) * buttonSpacing / 2f;
            float startZ = -(rows - 1) * buttonSpacing / 2f;
            
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Vector3 localPos = new Vector3(
                        startX + col * buttonSpacing,
                        0,
                        startZ + row * buttonSpacing
                    );
                    
                    GameObject buttonObj = CreateButton(index, localPos);
                    createdButtons[index] = buttonObj.GetComponent<SollertiaButton>();
                    index++;
                }
            }
            
            Debug.Log($"[SollertiaGridSetup] Created {createdButtons.Length} buttons");
        }
        
        private GameObject CreateButton(int index, Vector3 localPos)
        {
            // Create button object
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = $"Button_{index}";
            button.transform.SetParent(transform);
            button.transform.localPosition = localPos;
            button.transform.localScale = new Vector3(buttonSize, 0.01f, buttonSize);
            
            // Add SollertiaButton component
            SollertiaButton sb = button.AddComponent<SollertiaButton>();
            sb.buttonRenderer = button.GetComponent<Renderer>();
            
            // Set material
            if (buttonMaterial != null)
            {
                sb.buttonRenderer.material = new Material(buttonMaterial);
            }
            
            // Make collider a trigger
            Collider col = button.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            
            return button;
        }
    }
}
