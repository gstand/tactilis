//! Arduino serial communication handler.
//!
//! Manages USB serial connection to the Arduino, parsing sensor readings
//! and forwarding them to the main application.

use crate::protocol::{ArduinoMessage, Finger, SensorReading};
use std::io::{BufRead, BufReader};
use std::sync::mpsc;
use std::time::Duration;
use thiserror::Error;

#[derive(Error, Debug)]
pub enum ArduinoError {
    #[error("Serial port error: {0}")]
    Serial(#[from] serialport::Error),
    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),
    #[error("JSON parse error: {0}")]
    Parse(#[from] serde_json::Error),
    #[error("Port not found: {0}")]
    PortNotFound(String),
}

/// Lists available serial ports that might be Arduino devices.
pub fn list_available_ports() -> Vec<String> {
    serialport::available_ports()
        .unwrap_or_default()
        .into_iter()
        .map(|p| p.port_name)
        .collect()
}

/// Handle to the Arduino connection thread.
pub struct ArduinoHandle {
    pub receiver: mpsc::Receiver<ArduinoMessage>,
    _thread: std::thread::JoinHandle<()>,
}

/// Configuration for Arduino connection.
#[derive(Clone)]
pub struct ArduinoConfig {
    pub port_name: String,
    pub baud_rate: u32,
}

impl Default for ArduinoConfig {
    fn default() -> Self {
        Self {
            port_name: String::new(),
            baud_rate: 115200,
        }
    }
}

/// Starts the Arduino communication thread.
///
/// Returns a handle with a receiver for incoming messages.
pub fn connect(config: ArduinoConfig) -> Result<ArduinoHandle, ArduinoError> {
    let port = serialport::new(&config.port_name, config.baud_rate)
        .timeout(Duration::from_millis(100))
        .open()?;

    let (sender, receiver) = mpsc::channel();

    let thread = std::thread::spawn(move || {
        let mut reader = BufReader::new(port);
        let mut line_buf = String::new();

        loop {
            line_buf.clear();
            match reader.read_line(&mut line_buf) {
                Ok(0) => break, // EOF
                Ok(_) => {
                    let line = line_buf.trim();
                    if line.is_empty() {
                        continue;
                    }

                    // Try to parse as JSON message
                    match parse_arduino_message(line) {
                        Ok(msg) => {
                            if sender.send(msg).is_err() {
                                break; // Receiver dropped
                            }
                        }
                        Err(e) => {
                            tracing::warn!("Failed to parse Arduino message: {}", e);
                        }
                    }
                }
                Err(ref e) if e.kind() == std::io::ErrorKind::TimedOut => {
                    continue;
                }
                Err(e) => {
                    tracing::error!("Serial read error: {}", e);
                    break;
                }
            }
        }

        tracing::info!("Arduino communication thread exiting");
    });

    Ok(ArduinoHandle {
        receiver,
        _thread: thread,
    })
}

/// Parse a line from Arduino into a message.
fn parse_arduino_message(line: &str) -> Result<ArduinoMessage, ArduinoError> {
    // First try JSON parsing
    if line.starts_with('{') {
        return Ok(serde_json::from_str(line)?);
    }

    // Fallback: simple comma-separated format for sensor data
    // Format: "I,1023,12345" or "M,512,12346"
    // (finger, raw_value, timestamp)
    let parts: Vec<&str> = line.split(',').collect();
    if parts.len() == 3 {
        let finger = match parts[0] {
            "I" => Finger::Index,
            "M" => Finger::Middle,
            _ => return Err(ArduinoError::Parse(serde_json::Error::io(
                std::io::Error::new(std::io::ErrorKind::InvalidData, "Unknown finger")
            ))),
        };

        let raw_value: u16 = parts[1].parse().map_err(|_| {
            ArduinoError::Parse(serde_json::Error::io(
                std::io::Error::new(std::io::ErrorKind::InvalidData, "Invalid pressure value")
            ))
        })?;

        let timestamp_ms: u64 = parts[2].parse().map_err(|_| {
            ArduinoError::Parse(serde_json::Error::io(
                std::io::Error::new(std::io::ErrorKind::InvalidData, "Invalid timestamp")
            ))
        })?;

        // Normalize 10-bit ADC (0-1023) to 0.0-1.0
        let pressure = raw_value as f32 / 1023.0;

        return Ok(ArduinoMessage::Sensor(SensorReading {
            finger,
            pressure,
            timestamp_ms,
        }));
    }

    // Check for ready message
    if line.starts_with("READY") {
        let version = line.strip_prefix("READY ").unwrap_or("unknown").to_string();
        return Ok(ArduinoMessage::Ready {
            firmware_version: version,
        });
    }

    Err(ArduinoError::Parse(serde_json::Error::io(
        std::io::Error::new(std::io::ErrorKind::InvalidData, format!("Unknown format: {}", line))
    )))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_simple_format() {
        let msg = parse_arduino_message("I,512,1000").unwrap();
        if let ArduinoMessage::Sensor(reading) = msg {
            assert_eq!(reading.finger, Finger::Index);
            assert!((reading.pressure - 0.5).abs() < 0.01);
            assert_eq!(reading.timestamp_ms, 1000);
        } else {
            panic!("Expected Sensor message");
        }
    }

    #[test]
    fn test_parse_ready() {
        let msg = parse_arduino_message("READY v1.0.0").unwrap();
        if let ArduinoMessage::Ready { firmware_version } = msg {
            assert_eq!(firmware_version, "v1.0.0");
        } else {
            panic!("Expected Ready message");
        }
    }
}
