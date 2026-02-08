using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the calibration flow for placing the button grid at the player's fingertip.
/// Uses XR controller input: Left Grip to place, A to confirm, B to cancel.
/// </summary>
public class CalibrationManager : MonoBehaviour
{
    public enum CalibrationState
    {
        Idle,           // Grid hidden, waiting for grip press
        Positioning,    // Grid visible at fingertip, waiting for A/B
        Confirmed       // Calibration complete, grid locked in place
    }

    [Header("References")]
    public GameObject buttonGrid;
    public Transform indexFingerTip;
    public ARGameUI arUI;
    public XRInputWatcher inputWatcher;

    [Header("Positioning Settings")]
    [Tooltip("Vertical offset from fingertip (negative = below finger)")]
    public float verticalOffset = -0.05f;
    
    [Tooltip("How much to smooth grid movement while positioning")]
    [Range(0f, 1f)]
    public float positionSmoothness = 0.1f;

    [Header("Events")]
    public UnityEvent OnCalibrationStarted;
    public UnityEvent OnCalibrationConfirmed;
    public UnityEvent OnCalibrationCancelled;

    [Header("Debug")]
    [SerializeField] private CalibrationState currentState = CalibrationState.Idle;

    private Vector3 targetGridPosition;
    private Quaternion targetGridRotation;

    public CalibrationState CurrentState => currentState;
    public bool IsCalibrated => currentState == CalibrationState.Confirmed;

    private void Start()
    {
        if (buttonGrid != null)
        {
            buttonGrid.SetActive(false);
        }

        currentState = CalibrationState.Idle;
        
        if (inputWatcher == null)
        {
            inputWatcher = FindFirstObjectByType<XRInputWatcher>();
        }

        SubscribeToInput();
        UpdateUIForState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInput();
    }

    private void SubscribeToInput()
    {
        if (inputWatcher == null) return;

        inputWatcher.OnLeftGripPressed.AddListener(OnLeftGripPressed);
        inputWatcher.OnPrimaryButtonPressed.AddListener(OnPrimaryButtonPressed);
        inputWatcher.OnSecondaryButtonPressed.AddListener(OnSecondaryButtonPressed);
    }

    private void UnsubscribeFromInput()
    {
        if (inputWatcher == null) return;

        inputWatcher.OnLeftGripPressed.RemoveListener(OnLeftGripPressed);
        inputWatcher.OnPrimaryButtonPressed.RemoveListener(OnPrimaryButtonPressed);
        inputWatcher.OnSecondaryButtonPressed.RemoveListener(OnSecondaryButtonPressed);
    }

    private void Update()
    {
        if (currentState == CalibrationState.Positioning)
        {
            UpdateGridPosition();
        }
    }

    private void OnLeftGripPressed()
    {
        if (currentState == CalibrationState.Idle)
        {
            StartPositioning();
        }
    }

    private void OnPrimaryButtonPressed()
    {
        if (currentState == CalibrationState.Positioning)
        {
            ConfirmCalibration();
        }
    }

    private void OnSecondaryButtonPressed()
    {
        if (currentState == CalibrationState.Positioning)
        {
            CancelCalibration();
        }
    }

    private void StartPositioning()
    {
        currentState = CalibrationState.Positioning;

        if (buttonGrid != null)
        {
            CalculateGridTransform();
            buttonGrid.transform.position = targetGridPosition;
            buttonGrid.transform.rotation = targetGridRotation;
            buttonGrid.SetActive(true);
        }

        OnCalibrationStarted?.Invoke();
        UpdateUIForState();
        Debug.Log("[CalibrationManager] Positioning started - grid visible at fingertip");
    }

    private void ConfirmCalibration()
    {
        currentState = CalibrationState.Confirmed;

        UnsubscribeFromInput();

        OnCalibrationConfirmed?.Invoke();
        UpdateUIForState();
        Debug.Log($"[CalibrationManager] Calibration confirmed at position: {buttonGrid.transform.position}");
    }

    private void CancelCalibration()
    {
        currentState = CalibrationState.Idle;

        if (buttonGrid != null)
        {
            buttonGrid.SetActive(false);
        }

        OnCalibrationCancelled?.Invoke();
        UpdateUIForState();
        Debug.Log("[CalibrationManager] Calibration cancelled - grid hidden");
    }

    private void UpdateGridPosition()
    {
        if (buttonGrid == null || indexFingerTip == null) return;

        CalculateGridTransform();

        if (positionSmoothness > 0f)
        {
            buttonGrid.transform.position = Vector3.Lerp(
                buttonGrid.transform.position,
                targetGridPosition,
                1f - positionSmoothness
            );
            buttonGrid.transform.rotation = Quaternion.Slerp(
                buttonGrid.transform.rotation,
                targetGridRotation,
                1f - positionSmoothness
            );
        }
        else
        {
            buttonGrid.transform.position = targetGridPosition;
            buttonGrid.transform.rotation = targetGridRotation;
        }
    }

    private void CalculateGridTransform()
    {
        if (indexFingerTip == null || Camera.main == null) return;

        // Position at fingertip with vertical offset
        targetGridPosition = indexFingerTip.position + Vector3.up * verticalOffset;

        // Calculate flat rotation facing player horizontally
        Vector3 playerForward = Camera.main.transform.forward;
        playerForward.y = 0f;
        
        if (playerForward.sqrMagnitude < 0.001f)
        {
            playerForward = Vector3.forward;
        }
        playerForward.Normalize();

        // Create rotation: grid faces player, but lies flat (parallel to ground)
        // LookRotation creates a rotation facing playerForward with Y-up
        // Then we rotate 90° on X to make the grid lie flat
        Quaternion facePlayer = Quaternion.LookRotation(playerForward, Vector3.up);
        targetGridRotation = facePlayer * Quaternion.Euler(90f, 0f, 0f);
    }

    private void UpdateUIForState()
    {
        if (arUI == null) return;

        switch (currentState)
        {
            case CalibrationState.Idle:
                arUI.SetRulesText(
                    "<size=120%><b>CALIBRATION</b></size>\n\n" +
                    "Position your <color=#4488FF>LEFT HAND</color> where you want the button grid.\n\n" +
                    "Press <b>LEFT GRIP</b> to place the grid."
                );
                arUI.ShowRules(true);
                break;

            case CalibrationState.Positioning:
                arUI.SetRulesText(
                    "<size=120%><b>ADJUST POSITION</b></size>\n\n" +
                    "Move your hand to adjust the grid position.\n\n" +
                    "Press <color=#44FF44><b>A</b></color> to confirm\n" +
                    "Press <color=#FF4444><b>B</b></color> to cancel"
                );
                break;

            case CalibrationState.Confirmed:
                // UI will be handled by GameController for countdown
                arUI.ShowRules(false);
                break;
        }
    }

    /// <summary>
    /// Resets calibration to allow repositioning.
    /// Call this to restart the calibration flow.
    /// </summary>
    public void ResetCalibration()
    {
        currentState = CalibrationState.Idle;

        if (buttonGrid != null)
        {
            buttonGrid.SetActive(false);
        }

        SubscribeToInput();
        UpdateUIForState();
        Debug.Log("[CalibrationManager] Calibration reset");
    }

    private void OnDrawGizmos()
    {
        if (indexFingerTip == null) return;

        Vector3 gizmoPos = indexFingerTip.position + Vector3.up * verticalOffset;

        Gizmos.color = currentState == CalibrationState.Confirmed ? Color.green : Color.cyan;
        Gizmos.DrawWireCube(gizmoPos, new Vector3(0.3f, 0.01f, 0.3f));

        // Draw fingertip position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(indexFingerTip.position, 0.02f);
    }
}