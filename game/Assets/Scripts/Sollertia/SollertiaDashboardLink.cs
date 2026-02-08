using UnityEngine;
using System;

namespace Sollertia
{
    /// <summary>
    /// Links to the dashboard for receiving pressure sensor data.
    /// When dashboard data is available, it overrides Unity hand tracking.
    /// </summary>
    public class SollertiaDashboardLink : MonoBehaviour
    {
        [Header("Connection")]
        public string serialPort = "COM3";
        public int baudRate = 115200;
        
        [Header("Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private float leftPressure = 0f;
        [SerializeField] private float rightPressure = 0f;
        
        [Header("Thresholds")]
        public float pressureThreshold = 0.5f;
        
        public bool IsConnected => isConnected;
        public float LeftPressure => leftPressure;
        public float RightPressure => rightPressure;
        
        // Events for when pressure sensors detect a press
        public static Action OnLeftPress;
        public static Action OnRightPress;
        
        private void Start()
        {
            // TODO: Implement serial connection to dashboard
            // For now, this is a placeholder that can be connected later
            Debug.Log("[SollertiaDashboardLink] Dashboard link initialized (not connected)");
        }
        
        private void Update()
        {
            // TODO: Read serial data from dashboard
            // Parse pressure values and trigger events
            
            // Placeholder: keyboard simulation for testing
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                SimulateLeftPress();
            }
            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                SimulateRightPress();
            }
#endif
        }
        
        public void SimulateLeftPress()
        {
            leftPressure = 1f;
            OnLeftPress?.Invoke();
            Debug.Log("[SollertiaDashboardLink] Left press simulated");
        }
        
        public void SimulateRightPress()
        {
            rightPressure = 1f;
            OnRightPress?.Invoke();
            Debug.Log("[SollertiaDashboardLink] Right press simulated");
        }
        
        public void Connect()
        {
            // TODO: Open serial port connection
            isConnected = true;
            Debug.Log($"[SollertiaDashboardLink] Connected to {serialPort}");
        }
        
        public void Disconnect()
        {
            isConnected = false;
            Debug.Log("[SollertiaDashboardLink] Disconnected");
        }
    }
}
