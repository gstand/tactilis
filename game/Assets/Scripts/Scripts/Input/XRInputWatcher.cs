using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

/// <summary>
/// Monitors XR controller buttons and fires UnityEvents when pressed/released.
/// Uses Unity's InputDevices API (compatible with Unity 6).
/// Attach to any GameObject in the scene.
/// </summary>
public class XRInputWatcher : MonoBehaviour
{
    [System.Serializable]
    public class ButtonEvent : UnityEvent<bool> { }

    [Header("Left Controller Events")]
    public UnityEvent OnLeftGripPressed;
    public UnityEvent OnLeftGripReleased;
    public UnityEvent OnLeftTriggerPressed;
    public UnityEvent OnLeftTriggerReleased;

    [Header("Right Controller Events")]
    public UnityEvent OnRightGripPressed;
    public UnityEvent OnRightGripReleased;
    public UnityEvent OnRightTriggerPressed;
    public UnityEvent OnRightTriggerReleased;

    [Header("Face Button Events (Either Controller)")]
    public UnityEvent OnPrimaryButtonPressed;
    public UnityEvent OnPrimaryButtonReleased;
    public UnityEvent OnSecondaryButtonPressed;
    public UnityEvent OnSecondaryButtonReleased;

    [Header("Settings")]
    [Tooltip("Threshold for analog inputs to register as pressed")]
    [Range(0.1f, 0.9f)]
    public float pressThreshold = 0.5f;

    private InputDevice leftController;
    private InputDevice rightController;

    private bool lastLeftGrip;
    private bool lastLeftTrigger;
    private bool lastRightGrip;
    private bool lastRightTrigger;
    private bool lastPrimaryButton;
    private bool lastSecondaryButton;

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        RefreshDevices();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    private void OnDeviceConnected(InputDevice device)
    {
        RefreshDevices();
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        RefreshDevices();
    }

    private void RefreshDevices()
    {
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftHandDevices
        );
        if (leftHandDevices.Count > 0)
        {
            leftController = leftHandDevices[0];
        }

        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightHandDevices
        );
        if (rightHandDevices.Count > 0)
        {
            rightController = rightHandDevices[0];
        }
    }

    private void Update()
    {
        CheckLeftController();
        CheckRightController();
        CheckFaceButtons();
    }

    private void CheckLeftController()
    {
        if (!leftController.isValid) return;

        // Grip
        if (leftController.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            bool gripPressed = gripValue >= pressThreshold;
            if (gripPressed && !lastLeftGrip)
            {
                OnLeftGripPressed?.Invoke();
            }
            else if (!gripPressed && lastLeftGrip)
            {
                OnLeftGripReleased?.Invoke();
            }
            lastLeftGrip = gripPressed;
        }
        else if (leftController.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton))
        {
            if (gripButton && !lastLeftGrip)
            {
                OnLeftGripPressed?.Invoke();
            }
            else if (!gripButton && lastLeftGrip)
            {
                OnLeftGripReleased?.Invoke();
            }
            lastLeftGrip = gripButton;
        }

        // Trigger
        if (leftController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            bool triggerPressed = triggerValue >= pressThreshold;
            if (triggerPressed && !lastLeftTrigger)
            {
                OnLeftTriggerPressed?.Invoke();
            }
            else if (!triggerPressed && lastLeftTrigger)
            {
                OnLeftTriggerReleased?.Invoke();
            }
            lastLeftTrigger = triggerPressed;
        }
        else if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton))
        {
            if (triggerButton && !lastLeftTrigger)
            {
                OnLeftTriggerPressed?.Invoke();
            }
            else if (!triggerButton && lastLeftTrigger)
            {
                OnLeftTriggerReleased?.Invoke();
            }
            lastLeftTrigger = triggerButton;
        }
    }

    private void CheckRightController()
    {
        if (!rightController.isValid) return;

        // Grip
        if (rightController.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            bool gripPressed = gripValue >= pressThreshold;
            if (gripPressed && !lastRightGrip)
            {
                OnRightGripPressed?.Invoke();
            }
            else if (!gripPressed && lastRightGrip)
            {
                OnRightGripReleased?.Invoke();
            }
            lastRightGrip = gripPressed;
        }
        else if (rightController.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton))
        {
            if (gripButton && !lastRightGrip)
            {
                OnRightGripPressed?.Invoke();
            }
            else if (!gripButton && lastRightGrip)
            {
                OnRightGripReleased?.Invoke();
            }
            lastRightGrip = gripButton;
        }

        // Trigger
        if (rightController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            bool triggerPressed = triggerValue >= pressThreshold;
            if (triggerPressed && !lastRightTrigger)
            {
                OnRightTriggerPressed?.Invoke();
            }
            else if (!triggerPressed && lastRightTrigger)
            {
                OnRightTriggerReleased?.Invoke();
            }
            lastRightTrigger = triggerPressed;
        }
        else if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton))
        {
            if (triggerButton && !lastRightTrigger)
            {
                OnRightTriggerPressed?.Invoke();
            }
            else if (!triggerButton && lastRightTrigger)
            {
                OnRightTriggerReleased?.Invoke();
            }
            lastRightTrigger = triggerButton;
        }
    }

    private void CheckFaceButtons()
    {
        // Check primary button (A/X) on either controller
        bool primaryPressed = false;
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPrimary))
        {
            primaryPressed |= rightPrimary;
        }
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimary))
        {
            primaryPressed |= leftPrimary;
        }

        if (primaryPressed && !lastPrimaryButton)
        {
            OnPrimaryButtonPressed?.Invoke();
        }
        else if (!primaryPressed && lastPrimaryButton)
        {
            OnPrimaryButtonReleased?.Invoke();
        }
        lastPrimaryButton = primaryPressed;

        // Check secondary button (B/Y) on either controller
        bool secondaryPressed = false;
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightSecondary))
        {
            secondaryPressed |= rightSecondary;
        }
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftSecondary))
        {
            secondaryPressed |= leftSecondary;
        }

        if (secondaryPressed && !lastSecondaryButton)
        {
            OnSecondaryButtonPressed?.Invoke();
        }
        else if (!secondaryPressed && lastSecondaryButton)
        {
            OnSecondaryButtonReleased?.Invoke();
        }
        lastSecondaryButton = secondaryPressed;
    }

    /// <summary>
    /// Returns true if the left grip is currently pressed.
    /// </summary>
    public bool IsLeftGripPressed()
    {
        if (!leftController.isValid) return false;
        if (leftController.TryGetFeatureValue(CommonUsages.grip, out float value))
        {
            return value >= pressThreshold;
        }
        if (leftController.TryGetFeatureValue(CommonUsages.gripButton, out bool button))
        {
            return button;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the primary button (A/X) is currently pressed.
    /// </summary>
    public bool IsPrimaryButtonPressed()
    {
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPrimary) && rightPrimary)
        {
            return true;
        }
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimary) && leftPrimary)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the secondary button (B/Y) is currently pressed.
    /// </summary>
    public bool IsSecondaryButtonPressed()
    {
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightSecondary) && rightSecondary)
        {
            return true;
        }
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftSecondary) && leftSecondary)
        {
            return true;
        }
        return false;
    }
}
