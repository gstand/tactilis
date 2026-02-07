package com.rehabtool.frs;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.os.Build;
import android.util.Log;

import com.hoho.android.usbserial.driver.UsbSerialDriver;
import com.hoho.android.usbserial.driver.UsbSerialPort;
import com.hoho.android.usbserial.driver.UsbSerialProber;
import com.hoho.android.usbserial.util.SerialInputOutputManager;

import com.unity3d.player.UnityPlayerActivity;

import java.io.IOException;
import java.util.List;
import java.util.concurrent.Executors;

/**
 * Custom Unity Activity for USB Serial communication with Arduino/Elegoo Uno R3.
 * Uses usb-serial-for-android library for CH340 chip support.
 * 
 * SETUP:
 * 1. Download usb-serial-for-android-3.9.0.aar from GitHub releases
 * 2. Place in Assets/Plugins/Android/
 * 3. Update AndroidManifest.xml to use this activity
 */
public class FRSUnityActivity extends UnityPlayerActivity implements SerialInputOutputManager.Listener {
    
    private static final String TAG = "FRSUnityActivity";
    private static final String ACTION_USB_PERMISSION = "com.rehabtool.frs.USB_PERMISSION";
    
    private UsbManager usbManager;
    private UsbSerialPort serialPort;
    private SerialInputOutputManager ioManager;
    private StringBuilder dataBuffer = new StringBuilder();
    private boolean isConnected = false;
    
    private int targetBaudRate = 9600;
    private int targetVendorId = 0;
    private int targetProductId = 0;
    
    private final BroadcastReceiver usbReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if (ACTION_USB_PERMISSION.equals(action)) {
                synchronized (this) {
                    UsbDevice device = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);
                    if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                        if (device != null) {
                            connectToDevice(device);
                        }
                    } else {
                        Log.w(TAG, "USB permission denied");
                    }
                }
            } else if (UsbManager.ACTION_USB_DEVICE_DETACHED.equals(action)) {
                closeUSBSerial();
            }
        }
    };
    
    @Override
    protected void onCreate(android.os.Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        usbManager = (UsbManager) getSystemService(Context.USB_SERVICE);
        
        IntentFilter filter = new IntentFilter();
        filter.addAction(ACTION_USB_PERMISSION);
        filter.addAction(UsbManager.ACTION_USB_DEVICE_DETACHED);
        
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            registerReceiver(usbReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
        } else {
            registerReceiver(usbReceiver, filter);
        }
    }
    
    @Override
    protected void onDestroy() {
        closeUSBSerial();
        unregisterReceiver(usbReceiver);
        super.onDestroy();
    }
    
    /**
     * Called from Unity to initialize USB serial connection
     */
    public boolean initUSBSerial(int baudRate, int vendorId, int productId) {
        this.targetBaudRate = baudRate;
        this.targetVendorId = vendorId;
        this.targetProductId = productId;
        
        List<UsbSerialDriver> availableDrivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);
        
        if (availableDrivers.isEmpty()) {
            Log.w(TAG, "No USB serial devices found");
            return false;
        }
        
        // Find matching device or use first available
        UsbSerialDriver driver = null;
        for (UsbSerialDriver d : availableDrivers) {
            UsbDevice device = d.getDevice();
            if (vendorId == 0 || (device.getVendorId() == vendorId && device.getProductId() == productId)) {
                driver = d;
                break;
            }
        }
        
        if (driver == null) {
            driver = availableDrivers.get(0);
        }
        
        UsbDevice device = driver.getDevice();
        
        if (!usbManager.hasPermission(device)) {
            int flags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.S ? PendingIntent.FLAG_MUTABLE : 0;
            PendingIntent permissionIntent = PendingIntent.getBroadcast(this, 0, 
                new Intent(ACTION_USB_PERMISSION), flags);
            usbManager.requestPermission(device, permissionIntent);
            return false; // Will connect after permission granted
        }
        
        return connectToDevice(device);
    }
    
    private boolean connectToDevice(UsbDevice device) {
        try {
            List<UsbSerialDriver> drivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);
            UsbSerialDriver driver = null;
            
            for (UsbSerialDriver d : drivers) {
                if (d.getDevice().equals(device)) {
                    driver = d;
                    break;
                }
            }
            
            if (driver == null) {
                Log.e(TAG, "Driver not found for device");
                return false;
            }
            
            UsbDeviceConnection connection = usbManager.openDevice(device);
            if (connection == null) {
                Log.e(TAG, "Could not open device connection");
                return false;
            }
            
            serialPort = driver.getPorts().get(0);
            serialPort.open(connection);
            serialPort.setParameters(targetBaudRate, 8, UsbSerialPort.STOPBITS_1, UsbSerialPort.PARITY_NONE);
            
            ioManager = new SerialInputOutputManager(serialPort, this);
            Executors.newSingleThreadExecutor().submit(ioManager);
            
            isConnected = true;
            Log.i(TAG, "USB Serial connected: " + device.getDeviceName());
            return true;
            
        } catch (IOException e) {
            Log.e(TAG, "Error connecting: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Called from Unity to read buffered serial data
     */
    public String readUSBSerial() {
        synchronized (dataBuffer) {
            String data = dataBuffer.toString();
            dataBuffer.setLength(0);
            return data;
        }
    }
    
    /**
     * Called from Unity to write data to serial port
     */
    public boolean writeUSBSerial(String data) {
        if (serialPort == null || !isConnected) return false;
        
        try {
            serialPort.write(data.getBytes(), 100);
            return true;
        } catch (IOException e) {
            Log.e(TAG, "Write error: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * Called from Unity to close connection
     */
    public void closeUSBSerial() {
        isConnected = false;
        
        if (ioManager != null) {
            ioManager.stop();
            ioManager = null;
        }
        
        if (serialPort != null) {
            try {
                serialPort.close();
            } catch (IOException e) {
                Log.e(TAG, "Error closing port: " + e.getMessage());
            }
            serialPort = null;
        }
    }
    
    public boolean isUSBConnected() {
        return isConnected;
    }
    
    // SerialInputOutputManager.Listener callbacks
    @Override
    public void onNewData(byte[] data) {
        synchronized (dataBuffer) {
            dataBuffer.append(new String(data));
        }
    }
    
    @Override
    public void onRunError(Exception e) {
        Log.e(TAG, "Serial run error: " + e.getMessage());
        isConnected = false;
    }
}
