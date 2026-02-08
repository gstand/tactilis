using UnityEngine;
using UnityEngine.InputSystem;

namespace Tactilis
{
    /// <summary>
    /// Handles XR controller input for the SollertiaCalibrationSystem.
    /// Connects grip, A button, and B button to calibration actions.
    /// </summary>
    public class SollertiaCalibrationInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SollertiaCalibrationSystem calibrationSystem;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference gripAction;
        [SerializeField] private InputActionReference primaryButtonAction;  // A button
        [SerializeField] private InputActionReference secondaryButtonAction; // B button

        [Header("Settings")]
        [Tooltip("Which hand's grip triggers manual positioning")]
        [SerializeField] private bool useEitherHand = true;
        [Tooltip("Auto-confirm after this many seconds if a table is detected (0 = disabled)")]
        [SerializeField] private float autoConfirmDelay = 3f;

        private bool gripHeld = false;
        private float autoConfirmTimer = 0f;
        private bool autoConfirmStarted = false;

        private void OnEnable()
        {
            if (gripAction != null && gripAction.action != null)
            {
                gripAction.action.Enable();
                gripAction.action.started += OnGripStarted;
                gripAction.action.canceled += OnGripCanceled;
            }

            if (primaryButtonAction != null && primaryButtonAction.action != null)
            {
                primaryButtonAction.action.Enable();
                primaryButtonAction.action.performed += OnPrimaryButtonPressed;
            }

            if (secondaryButtonAction != null && secondaryButtonAction.action != null)
            {
                secondaryButtonAction.action.Enable();
                secondaryButtonAction.action.performed += OnSecondaryButtonPressed;
            }
        }

        private void OnDisable()
        {
            if (gripAction != null && gripAction.action != null)
            {
                gripAction.action.started -= OnGripStarted;
                gripAction.action.canceled -= OnGripCanceled;
            }

            if (primaryButtonAction != null && primaryButtonAction.action != null)
            {
                primaryButtonAction.action.performed -= OnPrimaryButtonPressed;
            }

            if (secondaryButtonAction != null && secondaryButtonAction.action != null)
            {
                secondaryButtonAction.action.performed -= OnSecondaryButtonPressed;
            }
        }

        private void Start()
        {
            if (calibrationSystem == null)
            {
                calibrationSystem = FindFirstObjectByType<SollertiaCalibrationSystem>();
            }
        }

        private void Update()
        {
            if (calibrationSystem == null || !calibrationSystem.IsCalibrating) return;

            // Handle auto-confirm for auto-detected tables
            if (autoConfirmDelay > 0 && 
                calibrationSystem.CurrentMode == SollertiaCalibrationSystem.CalibrationMode.Auto &&
                calibrationSystem.HasValidPlane)
            {
                if (!autoConfirmStarted)
                {
                    autoConfirmStarted = true;
                    autoConfirmTimer = 0f;
                    Debug.Log($"[CalibrationInput] Auto-confirm will trigger in {autoConfirmDelay}s");
                }

                autoConfirmTimer += Time.deltaTime;
                if (autoConfirmTimer >= autoConfirmDelay)
                {
                    Debug.Log("[CalibrationInput] Auto-confirming detected table");
                    calibrationSystem.ConfirmPlacement();
                    autoConfirmStarted = false;
                }
            }
            else
            {
                autoConfirmStarted = false;
                autoConfirmTimer = 0f;
            }

            // Keyboard fallback for editor testing
            HandleKeyboardInput();
        }

        private void OnGripStarted(InputAction.CallbackContext context)
        {
            gripHeld = true;

            if (calibrationSystem != null && 
                calibrationSystem.CurrentMode == SollertiaCalibrationSystem.CalibrationMode.Manual)
            {
                calibrationSystem.StartManualPositioning();
                Debug.Log("[CalibrationInput] Grip pressed - starting manual positioning");
            }
        }

        private void OnGripCanceled(InputAction.CallbackContext context)
        {
            gripHeld = false;

            if (calibrationSystem != null)
            {
                calibrationSystem.StopManualPositioning();
                Debug.Log("[CalibrationInput] Grip released - stopped manual positioning");
            }
        }

        private void OnPrimaryButtonPressed(InputAction.CallbackContext context)
        {
            // A button = Confirm
            if (calibrationSystem != null && calibrationSystem.IsCalibrating)
            {
                calibrationSystem.ConfirmPlacement();
                Debug.Log("[CalibrationInput] A button pressed - confirming placement");
            }
        }

        private void OnSecondaryButtonPressed(InputAction.CallbackContext context)
        {
            // B button = Cancel or switch to manual
            if (calibrationSystem != null && calibrationSystem.IsCalibrating)
            {
                if (calibrationSystem.CurrentMode == SollertiaCalibrationSystem.CalibrationMode.Auto)
                {
                    // In auto mode, B switches to manual
                    calibrationSystem.SwitchToManualCalibration();
                    Debug.Log("[CalibrationInput] B button pressed - switching to manual mode");
                }
                else
                {
                    // In manual mode, B cancels
                    calibrationSystem.CancelCalibration();
                    Debug.Log("[CalibrationInput] B button pressed - cancelling calibration");
                }
            }
        }

        private void HandleKeyboardInput()
        {
#if UNITY_EDITOR
            if (calibrationSystem == null || !calibrationSystem.IsCalibrating) return;

            // Space = Grip (hold)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (calibrationSystem.CurrentMode == SollertiaCalibrationSystem.CalibrationMode.Manual)
                {
                    calibrationSystem.StartManualPositioning();
                    Debug.Log("[CalibrationInput] Space pressed - starting manual positioning (editor)");
                }
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                calibrationSystem.StopManualPositioning();
            }

            // Return/Enter = Confirm (A button)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                calibrationSystem.ConfirmPlacement();
                Debug.Log("[CalibrationInput] Enter pressed - confirming placement (editor)");
            }

            // Escape = Cancel/Switch (B button)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (calibrationSystem.CurrentMode == SollertiaCalibrationSystem.CalibrationMode.Auto)
                {
                    calibrationSystem.SwitchToManualCalibration();
                }
                else
                {
                    calibrationSystem.CancelCalibration();
                }
                Debug.Log("[CalibrationInput] Escape pressed - cancel/switch (editor)");
            }

            // M = Force manual mode
            if (Input.GetKeyDown(KeyCode.M))
            {
                calibrationSystem.SwitchToManualCalibration();
                Debug.Log("[CalibrationInput] M pressed - forcing manual mode (editor)");
            }
#endif
        }
    }
}
