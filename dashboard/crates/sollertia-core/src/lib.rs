use std::fmt::Display;

use serde::{Deserialize, Serialize};

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
