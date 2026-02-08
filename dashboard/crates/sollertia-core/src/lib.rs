use serde::{Deserialize, Serialize};
use std::fmt::Display;

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
pub enum Finger {
    Index,
    Middle,
    StartMessage,
    BufferAlignment,
}

impl Finger {
    pub fn from_u8(input: u8) -> Result<Self, u8> {
        match input {
            0 => Ok(Self::Index),
            1 => Ok(Self::Middle),
            // u8::MAX => Ok(Self::StartMessage),
            _ => Err(input),
        }
    }
}

impl Display for Finger {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        let msg = match self {
            Finger::Index => "Index",
            Finger::Middle => "Middle",
            Finger::StartMessage => "Start message, not a",
            Finger::BufferAlignment => "Buffer alignment, not a",
        };
        write!(f, "{msg} finger")
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Pressure {
    pub pressure: u16,
    pub finger: Finger,
}

/// Message sent from dashboard to Unity when a press is detected
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PressEvent {
    pub finger: Finger,
    pub pressure: u16,
    pub timestamp_ms: u64,
}

/// 3D position in AR space
#[derive(Debug, Clone, Copy, Serialize, Deserialize, Default)]
pub struct Vec3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

impl Vec3 {
    pub fn distance(&self, other: &Vec3) -> f32 {
        let dx = self.x - other.x;
        let dy = self.y - other.y;
        let dz = self.z - other.z;
        (dx * dx + dy * dy + dz * dz).sqrt()
    }
}

/// Response from Unity with position data for accuracy calculation
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UnityResponse {
    pub finger_position: Vec3,
    pub target_position: Vec3,
    pub target_id: u32,
    pub timestamp_ms: u64,
}

/// A single tap record for tracking dexterity
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TapRecord {
    pub finger: Finger,
    pub pressure: u16,
    pub accuracy_distance: f32,
    pub reaction_time_ms: u64,
    pub timestamp_ms: u64,
}

impl Pressure {
    pub fn from_serial(input: [u8; 4]) -> Result<Self, [u8; 4]> {
        if input == [1, 0, 0, 0] {
            return Ok(Self {
                pressure: 0,
                finger: Finger::BufferAlignment,
            });
        } else if input == [255, 0, 0, 255] {
            return Ok(Self {
                pressure: 0,
                finger: Finger::StartMessage,
            });
        }

        if input[0] != 255 {
            return Err(input);
        }

        Ok(Self {
            pressure: ((input[1] as u16) << 8) | input[2] as u16,
            finger: Finger::from_u8(input[3]).map_err(|_| input)?,
        })
    }

    pub fn is_start(&self) -> bool {
        if self.pressure == 0
            && let Finger::StartMessage = self.finger
        {
            true
        } else {
            false
        }
    }
}

impl Display for Pressure {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{} pressure on {}", self.pressure, self.finger)
    }
}
