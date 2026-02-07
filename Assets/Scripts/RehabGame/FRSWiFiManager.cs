using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages WiFi UDP communication with Arduino/ESP32 for FRS (Force Resistive Sensor) input.
/// This is the Quest 3 compatible version - USB Serial does NOT work on Android/Quest.
/// 
/// Use ESP32 or Arduino with WiFi shield. ESP32 is recommended.
/// Expects data format: "INDEX:512\n" or "MIDDLE:480\n"
/// </summary>
public class FRSWiFiManager : MonoBehaviour
{
    [Header("WiFi Settings")]
    [Tooltip("UDP port to listen on (must match Arduino/ESP32 code)")]
    public int listenPort = 8888;
    [Tooltip("Enable broadcast discovery of ESP32")]
    public bool enableDiscovery = true;

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
    public UnityEvent onConnected;
    public UnityEvent onDisconnected;

    public enum FingerType { Index, Middle, Unknown }

    [Serializable]
    public struct FRSFingerData
    {
        public FingerType finger;
        public int rawValue;
        public float normalizedValue;
        public bool isPressed;
    }

    public FRSFingerData IndexFingerData => _indexData;
    public FRSFingerData MiddleFingerData => _middleData;
    public bool IsConnected => _isConnected;
    public string ConnectedDeviceIP => _connectedIP;

    UdpClient _udpClient;
    Thread _receiveThread;
    bool _isRunning;
    bool _isConnected;
    string _connectedIP = "";

    FRSFingerData _indexData;
    FRSFingerData _middleData;
    bool _indexWasPressed;
    bool _middleWasPressed;

    readonly object _lock = new object();
    string _pendingData;
    float _lastDataTime;
    const float CONNECTION_TIMEOUT = 2f;

    void Start()
    {
        _indexData = new FRSFingerData { finger = FingerType.Index };
        _middleData = new FRSFingerData { finger = FingerType.Middle };

#if UNITY_EDITOR
        if (simulateInEditor)
        {
            Debug.Log("[FRSWiFiManager] Running in Editor with simulation mode. Press I/M keys to simulate finger presses.");
            return;
        }
#endif
        StartListening();
    }

    void OnDestroy()
    {
        StopListening();
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
        CheckConnectionStatus();
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

    public void StartListening()
    {
        if (_isRunning) return;

        try
        {
            _udpClient = new UdpClient(listenPort);
            _udpClient.Client.ReceiveTimeout = 100;
            _isRunning = true;

            _receiveThread = new Thread(ReceiveThread) { IsBackground = true };
            _receiveThread.Start();

            Debug.Log($"[FRSWiFiManager] Listening on UDP port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FRSWiFiManager] Failed to start UDP listener: {e.Message}");
        }
    }

    public void StopListening()
    {
        _isRunning = false;

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join(500);
        }

        if (_udpClient != null)
        {
            _udpClient.Close();
            _udpClient = null;
        }

        _isConnected = false;
    }

    void ReceiveThread()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (_isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data).Trim();

                if (!string.IsNullOrEmpty(message))
                {
                    lock (_lock)
                    {
                        _pendingData = message;
                        _connectedIP = remoteEP.Address.ToString();
                    }
                }
            }
            catch (SocketException)
            {
                // Timeout - expected
            }
            catch (Exception e)
            {
                if (_isRunning)
                    Debug.LogWarning($"[FRSWiFiManager] Receive error: {e.Message}");
            }
        }
    }

    void ProcessPendingData()
    {
        string data = null;
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(_pendingData))
            {
                data = _pendingData;
                _pendingData = null;
            }
        }

        if (string.IsNullOrEmpty(data)) return;

        _lastDataTime = Time.time;

        // Handle multiple lines (in case of buffered data)
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            ProcessLine(line);
        }
    }

    void ProcessLine(string line)
    {
        if (logRawData)
            Debug.Log($"[FRS WiFi] {line}");

        // Parse format: "INDEX:512" or "MIDDLE:480"
        var parts = line.Split(':');
        if (parts.Length != 2) return;

        string fingerName = parts[0].ToUpper().Trim();
        if (!int.TryParse(parts[1].Trim(), out int rawValue)) return;

        FingerType finger = fingerName switch
        {
            "INDEX" => FingerType.Index,
            "MIDDLE" => FingerType.Middle,
            _ => FingerType.Unknown
        };

        if (finger == FingerType.Unknown) return;

        float normalized = Mathf.Clamp01(rawValue / 1023f);
        bool isPressed = rawValue >= pressThreshold;

        var fingerData = new FRSFingerData
        {
            finger = finger,
            rawValue = rawValue,
            normalizedValue = normalized,
            isPressed = isPressed
        };

        if (finger == FingerType.Index)
        {
            _indexData = fingerData;
            CheckPressStateChange(ref _indexWasPressed, isPressed, rawValue, FingerType.Index);
        }
        else
        {
            _middleData = fingerData;
            CheckPressStateChange(ref _middleWasPressed, isPressed, rawValue, FingerType.Middle);
        }

        onFingerDataReceived?.Invoke(fingerData);
    }

    void CheckPressStateChange(ref bool wasPressed, bool isPressed, int rawValue, FingerType finger)
    {
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

    void CheckConnectionStatus()
    {
        bool wasConnected = _isConnected;
        _isConnected = (Time.time - _lastDataTime) < CONNECTION_TIMEOUT && _lastDataTime > 0;

        if (_isConnected && !wasConnected)
        {
            Debug.Log($"[FRSWiFiManager] Connected to ESP32 at {_connectedIP}");
            onConnected?.Invoke();
        }
        else if (!_isConnected && wasConnected)
        {
            Debug.Log("[FRSWiFiManager] Disconnected from ESP32");
            onDisconnected?.Invoke();
        }
    }
}
