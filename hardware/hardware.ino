const uint8_t sensors[2] = { A0, A1 };

void setup() {
  Serial.begin(9600);
}

void loop() {
  if (Serial.available() > 0 && Serial.read() == 83) {
    Serial.write(255);
    Serial.write(0);
    Serial.write(0);
    Serial.write(255);

    bool running = true;
    while (running) {
      while (Serial.available()) {
        int incoming = Serial.read();
        if (incoming == 83) {
          running = true;
        } else if (incoming == 115) {
          running = false;
        }
      }

      uint16_t pressure0 = analogRead(A0);
      Serial.write(0xFF);
      Serial.write((pressure0 >> 8) & 0xFF);  // High byte
      Serial.write(pressure0 & 0xFF);          // Low byte
      Serial.write(0);
      Serial.flush();

      uint16_t pressure1 = analogRead(A1);
      Serial.write(0xFF);
      Serial.write((pressure1 >> 8) & 0xFF);  // High byte
      Serial.write(pressure1 & 0xFF);          // Low byte
      Serial.write(1);
      Serial.flush();

      delay(100);  // Small delay for stability
    }
  }
}
