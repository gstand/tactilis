/*
 * ESP32 FRS (Force Resistive Sensor) WiFi Sketch
 * For Stroke Rehabilitation Game - Quest 3 Compatible
 * 
 * IMPORTANT: USB Serial does NOT work on Quest 3 (Android).
 * This sketch uses WiFi UDP to send data to the Quest.
 * 
 * Hardware: ESP32 DevKit (recommended) or ESP8266
 * 
 * Wiring:
 * - Index finger FRS: GPIO 34 (ADC1_CH6) with 10K pull-down resistor
 * - Middle finger FRS: GPIO 35 (ADC1_CH7) with 10K pull-down resistor
 * 
 * Circuit for each FRS:
 * 3.3V --- [FRS] --- GPIO Pin
 *                |
 *             [10K Resistor]
 *                |
 *               GND
 */

#include <WiFi.h>
#include <WiFiUdp.h>

// ============ CONFIGURE THESE ============
const char* WIFI_SSID = "YourWiFiNetwork";      // Your WiFi network name
const char* WIFI_PASSWORD = "YourWiFiPassword"; // Your WiFi password
const int QUEST_UDP_PORT = 8888;                // Must match Unity FRSWiFiManager.listenPort

// For static Quest IP (recommended for demo stability)
// Set to empty string "" to use broadcast
const char* QUEST_IP = "";  // e.g., "192.168.1.100" or "" for broadcast
// =========================================

const int INDEX_PIN = 34;  // ADC1 pins only (GPIO 32-39)
const int MIDDLE_PIN = 35;

const int SAMPLE_INTERVAL_MS = 20;  // 50Hz sampling rate

WiFiUDP udp;
IPAddress broadcastIP;
unsigned long lastSampleTime = 0;

// Smoothing
const int SMOOTH_SAMPLES = 3;
int indexReadings[SMOOTH_SAMPLES];
int middleReadings[SMOOTH_SAMPLES];
int readIndex = 0;

void setup() {
  Serial.begin(115200);
  Serial.println("\n=== ESP32 FRS WiFi Bridge ===");
  
  // Initialize ADC pins
  pinMode(INDEX_PIN, INPUT);
  pinMode(MIDDLE_PIN, INPUT);
  
  // Initialize smoothing arrays
  for (int i = 0; i < SMOOTH_SAMPLES; i++) {
    indexReadings[i] = 0;
    middleReadings[i] = 0;
  }
  
  // Connect to WiFi
  connectToWiFi();
  
  // Calculate broadcast IP
  IPAddress localIP = WiFi.localIP();
  IPAddress subnet = WiFi.subnetMask();
  broadcastIP = IPAddress(
    localIP[0] | ~subnet[0],
    localIP[1] | ~subnet[1],
    localIP[2] | ~subnet[2],
    localIP[3] | ~subnet[3]
  );
  
  Serial.print("Broadcast IP: ");
  Serial.println(broadcastIP);
  Serial.print("Sending to port: ");
  Serial.println(QUEST_UDP_PORT);
  Serial.println("\nReady! Sending FRS data...\n");
}

void connectToWiFi() {
  Serial.print("Connecting to WiFi: ");
  Serial.println(WIFI_SSID);
  
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  
  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 30) {
    delay(500);
    Serial.print(".");
    attempts++;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi Connected!");
    Serial.print("IP Address: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("\nWiFi Connection Failed! Restarting...");
    delay(1000);
    ESP.restart();
  }
}

void loop() {
  // Check WiFi connection
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi disconnected! Reconnecting...");
    connectToWiFi();
  }
  
  unsigned long currentTime = millis();
  
  if (currentTime - lastSampleTime >= SAMPLE_INTERVAL_MS) {
    lastSampleTime = currentTime;
    
    // Read raw ADC values (ESP32 ADC is 12-bit: 0-4095)
    int indexRaw = analogRead(INDEX_PIN);
    int middleRaw = analogRead(MIDDLE_PIN);
    
    // Scale to 10-bit (0-1023) to match Arduino Uno format
    indexRaw = indexRaw / 4;
    middleRaw = middleRaw / 4;
    
    // Update smoothing arrays
    indexReadings[readIndex] = indexRaw;
    middleReadings[readIndex] = middleRaw;
    readIndex = (readIndex + 1) % SMOOTH_SAMPLES;
    
    // Calculate smoothed values
    int indexSmooth = 0;
    int middleSmooth = 0;
    for (int i = 0; i < SMOOTH_SAMPLES; i++) {
      indexSmooth += indexReadings[i];
      middleSmooth += middleReadings[i];
    }
    indexSmooth /= SMOOTH_SAMPLES;
    middleSmooth /= SMOOTH_SAMPLES;
    
    // Send data via UDP
    sendFRSData("INDEX", indexSmooth);
    sendFRSData("MIDDLE", middleSmooth);
    
    // Also print to Serial for debugging
    Serial.print("INDEX:");
    Serial.print(indexSmooth);
    Serial.print(" MIDDLE:");
    Serial.println(middleSmooth);
  }
}

void sendFRSData(const char* finger, int value) {
  char buffer[32];
  snprintf(buffer, sizeof(buffer), "%s:%d\n", finger, value);
  
  IPAddress targetIP;
  if (strlen(QUEST_IP) > 0) {
    targetIP.fromString(QUEST_IP);
  } else {
    targetIP = broadcastIP;
  }
  
  udp.beginPacket(targetIP, QUEST_UDP_PORT);
  udp.print(buffer);
  udp.endPacket();
}

/*
 * SETUP INSTRUCTIONS:
 * 
 * 1. Install ESP32 board support in Arduino IDE:
 *    - File > Preferences > Additional Board Manager URLs
 *    - Add: https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
 *    - Tools > Board > Boards Manager > Search "ESP32" > Install
 * 
 * 2. Select your board:
 *    - Tools > Board > ESP32 Arduino > ESP32 Dev Module
 * 
 * 3. Configure WiFi credentials above (WIFI_SSID and WIFI_PASSWORD)
 * 
 * 4. Upload sketch to ESP32
 * 
 * 5. Open Serial Monitor (115200 baud) to verify:
 *    - WiFi connection
 *    - IP address
 *    - FRS data being sent
 * 
 * 6. In Unity, use FRSWiFiManager instead of FRSSerialManager
 *    - Set listenPort to 8888 (or match QUEST_UDP_PORT)
 * 
 * DEMO TIP: For most reliable demo:
 * - Use a mobile hotspot from your phone
 * - Connect both ESP32 and Quest 3 to the same hotspot
 * - Set QUEST_IP to the Quest's IP address (find in Quest WiFi settings)
 * 
 * TROUBLESHOOTING:
 * - If no data received: Check firewall, ensure same network
 * - If values are inverted: Swap VCC and GND on FRS
 * - If values are noisy: Add 0.1uF capacitor between ADC pin and GND
 */
