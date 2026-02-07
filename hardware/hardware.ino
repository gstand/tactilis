// www.sciencebuddies.org
// declare variables for FIRST SENSOR
int sensorpin = A0; 
int sensor;          // sensor readings

// declare variables for SECOND SENSOR
int sensorpin2 = A4;  // Second pressure sensor on analog pin A4
int sensor2;          // Variable to store second sensor readings

// LED pins
int led1 = 12;  // LED for first sensor on pin 12
int led2 = 11;  // LED for second sensor on pin 11

void setup() {
  // set LED pins as outputs
  pinMode(led1, OUTPUT);
  pinMode(led2, OUTPUT);
  
  // initialize serial communication
  Serial.begin(9600);
}

void loop() {
  // ===== FIRST SENSOR (A0) controls LED on pin 12 =====
  sensor = analogRead(sensorpin);
  Serial.print("Sensor 1: ");
  Serial.println(sensor);
  
  if(sensor > 500){
    digitalWrite(led1, HIGH);  // Turn on when pressure detected
  }
  else{
    digitalWrite(led1, LOW);   // Turn off when no pressure
  }
  
  // ===== SECOND SENSOR (A4) controls LED on pin 11 =====
  sensor2 = analogRead(sensorpin2);
  Serial.print("Sensor 2: ");
  Serial.println(sensor2);
  
  if(sensor2 > 500){
    digitalWrite(led2, HIGH);  // Turn on when pressure detected
  }
  else{
    digitalWrite(led2, LOW);   // Turn off when no pressure
  }
  
  delay(100);  // Small delay for stability
}

