using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Sollertia
{
    /// <summary>
    /// Tracks Quest controllers and creates finger tip colliders.
    /// Works with Meta Quest 3 via USB.
    /// </summary>
    public class HandTracker : MonoBehaviour
    {
        [Header("Finger Tips")]
        public GameObject leftFingerTip;
        public GameObject rightFingerTip;
        
        [Header("Settings")]
        public float fingerOffset = 0.05f;
        
        private InputDevice leftController;
        private InputDevice rightController;
        
        private void Start()
        {
            // Create finger tips if not assigned
            if (leftFingerTip == null)
            {
                leftFingerTip = CreateFingerTip("LeftFingerTip", true);
            }
            if (rightFingerTip == null)
            {
                rightFingerTip = CreateFingerTip("RightFingerTip", false);
            }
            
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
            
            Debug.Log($"[HandTracker] Controllers: Left={leftController.isValid}, Right={rightController.isValid}");
        }
        
        private GameObject CreateFingerTip(string name, bool isLeft)
        {
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = name;
            tip.transform.SetParent(transform);
            tip.transform.localScale = Vector3.one * 0.03f;
            
            // Add FingerTip component
            FingerTip ft = tip.AddComponent<FingerTip>();
            ft.isLeftHand = isLeft;
            
            // Set color (blue theme)
            Renderer rend = tip.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = isLeft ? new Color(0.3f, 0.6f, 1f) : new Color(0.5f, 0.8f, 1f);
                rend.material = mat;
            }
            
            return tip;
        }
        
        private void Update()
        {
            UpdateFingerTip(leftController, leftFingerTip);
            UpdateFingerTip(rightController, rightFingerTip);
        }
        
        private void UpdateFingerTip(InputDevice controller, GameObject fingerTip)
        {
            if (fingerTip == null) return;
            
            if (!controller.isValid)
            {
                RefreshControllers();
                return;
            }
            
            if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                // Get XR origin for world space conversion
                Transform origin = Camera.main?.transform.parent;
                
                Vector3 worldPos;
                Quaternion worldRot;
                
                if (origin != null)
                {
                    worldPos = origin.TransformPoint(pos);
                    worldRot = origin.rotation * rot;
                }
                else
                {
                    worldPos = pos;
                    worldRot = rot;
                }
                
                // Offset forward to simulate fingertip
                worldPos += worldRot * Vector3.forward * fingerOffset;
                
                fingerTip.transform.position = worldPos;
                fingerTip.transform.rotation = worldRot;
            }
        }
    }
}
