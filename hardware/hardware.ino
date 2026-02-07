const uint8_t sensors = {A0, A1};

void setup() {
  Serial.begin(9600);
  write_pressure(0, UINT32_MAX);
}

void loop() {
  for (int i = 0; i <= 1; i++) {
    write_pressure(analogRead(i), i);
  }
  delay(100);  // Small delay for stability
}

size_t write_pressure(uint16_t pressure, uint8_t sensor) {
  Serial.write(pressure);
  Serial.write(sensor);
}
