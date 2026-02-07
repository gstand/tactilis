/*
 * FRS (Force Resistive Sensor) Arduino Sketch
 * For Stroke Rehabilitation Game
 * 
 * Wiring:
 * - Index finger FRS: A0 (with 10K pull-down resistor)
 * - Middle finger FRS: A1 (with 10K pull-down resistor)
 * 
 * Circuit for each FRS:
 * VCC (5V) --- [FRS] --- Analog Pin
 *                    |
 *                   [10K Resistor]
 *                    |
 *                   GND
 * 
 * Output format: "INDEX:512\n" or "MIDDLE:480\n"
 * Values range from 0 (no pressure) to 1023 (max pressure)
 */

const int INDEX_PIN = A0;
const int MIDDLE_PIN = A1;

// Sampling rate (ms between readings)
const int SAMPLE_INTERVAL = 20; // 50Hz

// Smoothing (simple moving average)
const int SMOOTH_SAMPLES = 3;
int indexReadings[SMOOTH_SAMPLES];
int middleReadings[SMOOTH_SAMPLES];
int readIndex = 0;

unsigned long lastSampleTime = 0;

void setup() {
  Serial.begin(9600);
  while (!Serial) {
    ; // Wait for serial port to connect (needed for native USB)
  }
  
  // Initialize smoothing arrays
  for (int i = 0; i < SMOOTH_SAMPLES; i++) {
    indexReadings[i] = 0;
    middleReadings[i] = 0;
  }
  
  Serial.println("FRS_READY");
}

void loop() {
  unsigned long currentTime = millis();
  
  if (currentTime - lastSampleTime >= SAMPLE_INTERVAL) {
    lastSampleTime = currentTime;
    
    // Read raw values
    int indexRaw = analogRead(INDEX_PIN);
    int middleRaw = analogRead(MIDDLE_PIN);
    
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
    
    // Send data to Unity
    Serial.print("INDEX:");
    Serial.println(indexSmooth);
    
    Serial.print("MIDDLE:");
    Serial.println(middleSmooth);
  }
}

/*
 * CALIBRATION NOTES:
 * 
 * 1. With no pressure, readings should be near 0
 * 2. Light touch: ~100-300
 * 3. Medium press: ~300-600
 * 4. Hard press: ~600-900
 * 5. Max pressure: ~900-1023
 * 
 * Adjust the pressThreshold in Unity's FRSSerialManager based on your sensors.
 * Recommended starting values:
 * - pressThreshold: 300 (triggers a "press")
 * - releaseThreshold: 200 (triggers a "release" - hysteresis prevents jitter)
 * 
 * If readings are inverted (high when no pressure), swap VCC and GND on the FRS.
 */
