using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tactilis
{
    /// <summary>
    /// UI component for displaying calibration status and instructions.
    /// </summary>
    public class SollertiaCalibrationUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SollertiaCalibrationSystem calibrationSystem;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject calibrationPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI instructionsText;
        [SerializeField] private Button manualModeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject scanningIndicator;
        [SerializeField] private GameObject roomSetupWarning;

        [Header("Messages")]
        [SerializeField] private string scanningMessage = "Scanning for table surface...";
        [SerializeField] private string tableFoundMessage = "Table detected!";
        [SerializeField] private string manualModeMessage = "Manual placement mode";
        [SerializeField] private string roomSetupRequiredMessage = "Room Setup required for automatic detection.\nGo to Settings > Physical Space > Space Setup on your Quest.";

        private void OnEnable()
        {
            if (calibrationSystem != null)
            {
                calibrationSystem.OnCalibrationStarted.AddListener(OnCalibrationStarted);
                calibrationSystem.OnCalibrationCompleted.AddListener(OnCalibrationCompleted);
                calibrationSystem.OnAutoDetectionFailed.AddListener(OnAutoDetectionFailed);
                calibrationSystem.OnManualCalibrationStarted.AddListener(OnManualCalibrationStarted);
                calibrationSystem.OnStatusChanged.AddListener(OnStatusChanged);
            }

            if (manualModeButton != null)
                manualModeButton.onClick.AddListener(OnManualModeClicked);
            
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDisable()
        {
            if (calibrationSystem != null)
            {
                calibrationSystem.OnCalibrationStarted.RemoveListener(OnCalibrationStarted);
                calibrationSystem.OnCalibrationCompleted.RemoveListener(OnCalibrationCompleted);
                calibrationSystem.OnAutoDetectionFailed.RemoveListener(OnAutoDetectionFailed);
                calibrationSystem.OnManualCalibrationStarted.RemoveListener(OnManualCalibrationStarted);
                calibrationSystem.OnStatusChanged.RemoveListener(OnStatusChanged);
            }

            if (manualModeButton != null)
                manualModeButton.onClick.RemoveListener(OnManualModeClicked);
            
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        private void Update()
        {
            if (calibrationSystem == null) return;

            // Update instructions based on current state
            if (instructionsText != null)
            {
                instructionsText.text = calibrationSystem.GetUserInstructions();
            }

            // Update button states
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (calibrationSystem == null) return;

            bool isCalibrating = calibrationSystem.IsCalibrating;
            var mode = calibrationSystem.CurrentMode;

            // Manual mode button - only show during auto detection
            if (manualModeButton != null)
            {
                manualModeButton.gameObject.SetActive(
                    isCalibrating && mode == SollertiaCalibrationSystem.CalibrationMode.Auto
                );
            }

            // Confirm button - show when there's something to confirm
            if (confirmButton != null)
            {
                bool canConfirm = isCalibrating && (
                    (mode == SollertiaCalibrationSystem.CalibrationMode.Auto && calibrationSystem.HasValidPlane) ||
                    mode == SollertiaCalibrationSystem.CalibrationMode.Manual
                );
                confirmButton.gameObject.SetActive(canConfirm);
            }

            // Cancel button - always show during calibration
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(isCalibrating);
            }
        }

        private void OnCalibrationStarted()
        {
            if (calibrationPanel != null)
                calibrationPanel.SetActive(true);

            if (scanningIndicator != null)
                scanningIndicator.SetActive(true);

            if (roomSetupWarning != null)
                roomSetupWarning.SetActive(false);

            if (statusText != null)
                statusText.text = scanningMessage;
        }

        private void OnCalibrationCompleted(Vector3 position, Quaternion rotation)
        {
            if (calibrationPanel != null)
                calibrationPanel.SetActive(false);

            if (scanningIndicator != null)
                scanningIndicator.SetActive(false);
        }

        private void OnAutoDetectionFailed()
        {
            if (scanningIndicator != null)
                scanningIndicator.SetActive(false);

            if (roomSetupWarning != null)
                roomSetupWarning.SetActive(true);

            if (statusText != null)
                statusText.text = roomSetupRequiredMessage;
        }

        private void OnManualCalibrationStarted()
        {
            if (scanningIndicator != null)
                scanningIndicator.SetActive(false);

            if (roomSetupWarning != null)
                roomSetupWarning.SetActive(false);

            if (statusText != null)
                statusText.text = manualModeMessage;
        }

        private void OnStatusChanged(string status)
        {
            if (statusText != null)
                statusText.text = status;
        }

        private void OnManualModeClicked()
        {
            if (calibrationSystem != null)
                calibrationSystem.SwitchToManualCalibration();
        }

        private void OnConfirmClicked()
        {
            if (calibrationSystem != null)
                calibrationSystem.ConfirmPlacement();
        }

        private void OnCancelClicked()
        {
            if (calibrationSystem != null)
                calibrationSystem.CancelCalibration();
        }

        /// <summary>
        /// Show the calibration UI and start calibration.
        /// </summary>
        public void Show()
        {
            if (calibrationPanel != null)
                calibrationPanel.SetActive(true);

            if (calibrationSystem != null)
                calibrationSystem.StartCalibration();
        }

        /// <summary>
        /// Hide the calibration UI.
        /// </summary>
        public void Hide()
        {
            if (calibrationPanel != null)
                calibrationPanel.SetActive(false);
        }
    }
}
