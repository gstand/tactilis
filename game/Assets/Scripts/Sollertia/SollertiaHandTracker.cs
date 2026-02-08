using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Sollertia
{
    /// <summary>
    /// Tracks hand/controller positions and creates finger colliders.
    /// Works with controllers as fallback when hand tracking unavailable.
    /// </summary>
    public class SollertiaHandTracker : MonoBehaviour
    {
        [Header("Finger Prefab")]
        public GameObject fingerPrefab;
        
        [Header("Settings")]
        public float fingerRadius = 0.01f;
        
        [Header("Debug")]
        [SerializeField] private bool leftHandTracked = false;
        [SerializeField] private bool rightHandTracked = false;
        
        private GameObject leftFinger;
        private GameObject rightFinger;
        private SollertiaFinger leftFingerScript;
        private SollertiaFinger rightFingerScript;
        
        private InputDevice leftController;
        private InputDevice rightController;
        
        private void Start()
        {
            CreateFingers();
            RefreshControllers();
            
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }
        
        private void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }
        
        private void OnDeviceConnected(InputDevice device) => RefreshControllers();
        private void OnDeviceDisconnected(InputDevice device) => RefreshControllers();
        
        private void RefreshControllers()
        {
            var leftDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftDevices
            );
            if (leftDevices.Count > 0) leftController = leftDevices[0];
            
            var rightDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightDevices
            );
            if (rightDevices.Count > 0) rightController = rightDevices[0];
        }
        
        private void CreateFingers()
        {
            // Create left finger
            leftFinger = new GameObject("LeftFinger");
            leftFinger.transform.SetParent(transform);
            leftFingerScript = leftFinger.AddComponent<SollertiaFinger>();
            leftFingerScript.isLeftHand = true;
            leftFingerScript.colliderRadius = fingerRadius;
            
            // Create right finger
            rightFinger = new GameObject("RightFinger");
            rightFinger.transform.SetParent(transform);
            rightFingerScript = rightFinger.AddComponent<SollertiaFinger>();
            rightFingerScript.isLeftHand = false;
            rightFingerScript.colliderRadius = fingerRadius;
        }
        
        private void Update()
        {
            UpdateLeftHand();
            UpdateRightHand();
        }
        
        private void UpdateLeftHand()
        {
            if (!leftController.isValid)
            {
                RefreshControllers();
                leftHandTracked = false;
                return;
            }
            
            if (leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                // Convert to world space
                Transform origin = Camera.main?.transform.parent;
                if (origin != null)
                {
                    leftFinger.transform.position = origin.TransformPoint(pos);
                    leftFinger.transform.rotation = origin.rotation * rot;
                }
                else
                {
                    leftFinger.transform.localPosition = pos;
                    leftFinger.transform.localRotation = rot;
                }
                
                // Offset forward to simulate fingertip
                leftFinger.transform.position += leftFinger.transform.forward * 0.05f;
                leftHandTracked = true;
            }
        }
        
        private void UpdateRightHand()
        {
            if (!rightController.isValid)
            {
                RefreshControllers();
                rightHandTracked = false;
                return;
            }
            
            if (rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                // Convert to world space
                Transform origin = Camera.main?.transform.parent;
                if (origin != null)
                {
                    rightFinger.transform.position = origin.TransformPoint(pos);
                    rightFinger.transform.rotation = origin.rotation * rot;
                }
                else
                {
                    rightFinger.transform.localPosition = pos;
                    rightFinger.transform.localRotation = rot;
                }
                
                // Offset forward to simulate fingertip
                rightFinger.transform.position += rightFinger.transform.forward * 0.05f;
                rightHandTracked = true;
            }
        }
        
        public Vector3 GetLeftFingerPosition() => leftFinger != null ? leftFinger.transform.position : Vector3.zero;
        public Vector3 GetRightFingerPosition() => rightFinger != null ? rightFinger.transform.position : Vector3.zero;
    }
}
