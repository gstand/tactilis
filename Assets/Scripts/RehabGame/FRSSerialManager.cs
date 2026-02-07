using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages serial communication with Arduino for FRS (Force Resistive Sensor) input.
/// Expects data format: "INDEX:512\n" or "MIDDLE:480\n" (finger name + analog value 0-1023)
/// </summary>
public class FRSSerialManager : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [Tooltip("COM port name (e.g., COM3 on Windows, /dev/tty.usbmodem* on Mac)")]
    public string portName = "COM3";
    public int baudRate = 9600;
    public int readTimeout = 50;

    [Header("Pressure Thresholds")]
    [Tooltip("Analog value (0-1023) above which a press is registered")]
    public int pressThreshold = 300;
    [Tooltip("Analog value below which a release is registered (hysteresis)")]
    public int releaseThreshold = 200;

    [Header("Debug")]
    public bool logRawData = false;
    public bool simulateInEditor = true;

    [Header("Events")]
    public UnityEvent<FRSFingerData> onFingerDataReceived;
    public UnityEvent<FingerType> onFingerPressed;
    public UnityEvent<FingerType> onFingerReleased;

    public enum FingerType { Index, Middle, Unknown }

    [Serializable]
    public struct FRSFingerData
    {
        public FingerType finger;
        public int rawValue;
        public float normalizedValue; // 0-1
        public bool isPressed;
    }

    public FRSFingerData IndexFingerData => _indexData;
    public FRSFingerData MiddleFingerData => _middleData;
    public bool IsConnected => _isConnected;

    SerialPort _serialPort;
    Thread _readThread;
    bool _isRunning;
    bool _isConnected;

    FRSFingerData _indexData;
    FRSFingerData _middleData;
    bool _indexWasPressed;
    bool _middleWasPressed;

    readonly object _lock = new object();
    string _pendingLine;

    void Start()
    {
        _indexData = new FRSFingerData { finger = FingerType.Index };
        _middleData = new FRSFingerData { finger = FingerType.Middle };

#if UNITY_EDITOR
        if (simulateInEditor)
        {
            Debug.Log("[FRSSerialManager] Running in Editor with simulation mode. Press I/M keys to simulate finger presses.");
            return;
        }
#endif
        OpenConnection();
    }

    void OnDestroy()
    {
        CloseConnection();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (simulateInEditor)
        {
            SimulateInput();
            return;
        }
#endif
        ProcessPendingData();
    }

    void SimulateInput()
    {
        // Simulate index finger with I key
        if (Input.GetKeyDown(KeyCode.I))
        {
            _indexData.rawValue = 800;
            _indexData.normalizedValue = 0.78f;
            _indexData.isPressed = true;
            onFingerDataReceived?.Invoke(_indexData);
            onFingerPressed?.Invoke(FingerType.Index);
            _indexWasPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.I) && _indexWasPressed)
        {
            _indexData.rawValue = 100;
            _indexData.normalizedValue = 0.1f;
            _indexData.isPressed = false;
            onFingerDataReceived?.Invoke(_indexData);
            onFingerReleased?.Invoke(FingerType.Index);
            _indexWasPressed = false;
        }

        // Simulate middle finger with M key
        if (Input.GetKeyDown(KeyCode.M))
        {
            _middleData.rawValue = 750;
            _middleData.normalizedValue = 0.73f;
            _middleData.isPressed = true;
            onFingerDataReceived?.Invoke(_middleData);
            onFingerPressed?.Invoke(FingerType.Middle);
            _middleWasPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.M) && _middleWasPressed)
        {
            _middleData.rawValue = 80;
            _middleData.normalizedValue = 0.08f;
            _middleData.isPressed = false;
            onFingerDataReceived?.Invoke(_middleData);
            onFingerReleased?.Invoke(FingerType.Middle);
            _middleWasPressed = false;
        }
    }

    public void OpenConnection()
    {
        if (_isConnected) return;

        try
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = readTimeout,
                DtrEnable = true,
                RtsEnable = true
            };
            _serialPort.Open();
            _isConnected = true;
            _isRunning = true;

            _readThread = new Thread(ReadThread) { IsBackground = true };
            _readThread.Start();

            Debug.Log($"[FRSSerialManager] Connected to {portName} at {baudRate} baud");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FRSSerialManager] Failed to open {portName}: {e.Message}");
            _isConnected = false;
        }
    }

    public void CloseConnection()
    {
        _isRunning = false;

        if (_readThread != null && _readThread.IsAlive)
        {
            _readThread.Join(500);
        }

        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
            Debug.Log("[FRSSerialManager] Serial port closed");
        }

        _isConnected = false;
    }

    void ReadThread()
    {
        while (_isRunning && _serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                string line = _serialPort.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    lock (_lock)
                    {
                        _pendingLine = line.Trim();
                    }
                }
            }
            catch (TimeoutException)
            {
                // Expected when no data available
            }
            catch (Exception e)
            {
                if (_isRunning)
                    Debug.LogWarning($"[FRSSerialManager] Read error: {e.Message}");
            }
        }
    }

    void ProcessPendingData()
    {
        string line = null;
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(_pendingLine))
            {
                line = _pendingLine;
                _pendingLine = null;
            }
        }

        if (string.IsNullOrEmpty(line)) return;

        if (logRawData)
            Debug.Log($"[FRS Raw] {line}");

        // Parse format: "INDEX:512" or "MIDDLE:480"
        var parts = line.Split(':');
        if (parts.Length != 2) return;

        string fingerName = parts[0].ToUpper();
        if (!int.TryParse(parts[1], out int rawValue)) return;

        FingerType finger = fingerName switch
        {
            "INDEX" => FingerType.Index,
            "MIDDLE" => FingerType.Middle,
            _ => FingerType.Unknown
        };

        if (finger == FingerType.Unknown) return;

        float normalized = Mathf.Clamp01(rawValue / 1023f);
        bool isPressed = rawValue >= pressThreshold;

        var data = new FRSFingerData
        {
            finger = finger,
            rawValue = rawValue,
            normalizedValue = normalized,
            isPressed = isPressed
        };

        // Update stored data and check for state changes
        if (finger == FingerType.Index)
        {
            _indexData = data;
            CheckPressStateChange(ref _indexWasPressed, isPressed, rawValue, FingerType.Index);
        }
        else
        {
            _middleData = data;
            CheckPressStateChange(ref _middleWasPressed, isPressed, rawValue, FingerType.Middle);
        }

        onFingerDataReceived?.Invoke(data);
    }

    void CheckPressStateChange(ref bool wasPressed, bool isPressed, int rawValue, FingerType finger)
    {
        // Press detection with hysteresis
        if (!wasPressed && isPressed)
        {
            wasPressed = true;
            onFingerPressed?.Invoke(finger);
        }
        else if (wasPressed && rawValue <= releaseThreshold)
        {
            wasPressed = false;
            onFingerReleased?.Invoke(finger);
        }
    }

    /// <summary>
    /// Get list of available serial ports (useful for UI dropdown)
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }
}
