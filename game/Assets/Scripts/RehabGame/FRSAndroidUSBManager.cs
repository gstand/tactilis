using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// USB Serial communication for Android/Quest using native Java bridge.
/// Works with Elegoo Uno R3 (CH340 chip) and other Arduino boards.
/// 
/// REQUIRES: usb-serial-for-android library (.aar) in Plugins/Android folder
/// See DEV_ONBOARDING.md for setup instructions.
/// </summary>
public class FRSAndroidUSBManager : MonoBehaviour
{
    [Header("USB Settings")]
    [Tooltip("Baud rate - must match Arduino sketch")]
    public int baudRate = 9600;
    [Tooltip("Vendor ID for CH340 chip (Elegoo Uno R3). 0 = auto-detect")]
    public int vendorId = 0x1A86; // CH340 default VID
    [Tooltip("Product ID for CH340. 0 = auto-detect")]
    public int productId = 0x7523; // CH340 default PID

    [Header("Pressure Thresholds")]
    public int pressThreshold = 300;
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

    public bool IsConnected => _isConnected;
    public FRSFingerData IndexFingerData => _indexData;
    public FRSFingerData MiddleFingerData => _middleData;

    bool _isConnected;
    FRSFingerData _indexData;
    FRSFingerData _middleData;
    bool _indexWasPressed;
    bool _middleWasPressed;

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject _usbManager;
    AndroidJavaObject _activity;
    string _pendingData = "";
#endif

    void Start()
    {
        _indexData = new FRSFingerData { finger = FingerType.Index };
        _middleData = new FRSFingerData { finger = FingerType.Middle };

#if UNITY_EDITOR
        if (simulateInEditor)
        {
            Debug.Log("[FRSAndroidUSB] Editor mode - Press I/M keys to simulate");
            return;
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeUSB();
#endif
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

#if UNITY_ANDROID && !UNITY_EDITOR
        PollUSBData();
#endif
    }

    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CloseUSB();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void InitializeUSB()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                
                // Call our custom activity method to initialize USB
                bool success = _activity.Call<bool>("initUSBSerial", baudRate, vendorId, productId);
                
                if (success)
                {
                    _isConnected = true;
                    Debug.Log("[FRSAndroidUSB] USB Serial connected!");
                    onConnected?.Invoke();
                }
                else
                {
                    Debug.LogWarning("[FRSAndroidUSB] Failed to connect USB Serial");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FRSAndroidUSB] Init error: {e.Message}");
            Debug.LogError("[FRSAndroidUSB] Make sure you have the custom Activity and usb-serial-for-android library installed!");
        }
    }

    void PollUSBData()
    {
        if (!_isConnected || _activity == null) return;

        try
        {
            string data = _activity.Call<string>("readUSBSerial");
            if (!string.IsNullOrEmpty(data))
            {
                ProcessData(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FRSAndroidUSB] Read error: {e.Message}");
        }
    }

    void CloseUSB()
    {
        if (_activity != null)
        {
            try
            {
                _activity.Call("closeUSBSerial");
            }
            catch { }
        }
        _isConnected = false;
    }
#endif

    void ProcessData(string data)
    {
        if (logRawData)
            Debug.Log($"[FRSAndroidUSB] {data}");

        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            ProcessLine(line.Trim());
        }
    }

    void ProcessLine(string line)
    {
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

    void SimulateInput()
    {
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

    /// <summary>
    /// Manually trigger reconnection attempt
    /// </summary>
    public void Reconnect()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CloseUSB();
        InitializeUSB();
#endif
    }
}
