//! Shared protocol definitions for communication between components.
//!
//! These message types are used for:
//! - Arduino → Core: Sensor readings
//! - Core → Unity: Visual cue triggers and session data
//! - Unity → Core: Game events and user interactions

use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};

/// Identifies which finger the sensor is attached to.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum Finger {
    Index,
    Middle,
}

/// Raw sensor reading from Arduino.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SensorReading {
    pub finger: Finger,
    pub pressure: f32,       // Normalized 0.0 - 1.0
    pub timestamp_ms: u64,   // Arduino millis()
}

/// A tap event detected from sensor data.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TapEvent {
    pub finger: Finger,
    pub pressure_peak: f32,
    pub duration_ms: u32,
    pub timestamp: DateTime<Utc>,
}

/// Target button in the AR environment.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TargetButton {
    pub id: u32,
    pub position: [f32; 3],  // x, y, z in Unity world space
    pub active: bool,
}

/// Dexterity metrics for a single tap attempt.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TapMetrics {
    pub target_id: u32,
    pub finger: Finger,
    pub reaction_time_ms: u32,   // Time from target activation to tap
    pub accuracy: f32,            // 0.0 - 1.0, distance-based
    pub pressure_consistency: f32,
    pub timestamp: DateTime<Utc>,
}

/// Session summary statistics.
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct SessionStats {
    pub total_taps: u32,
    pub successful_taps: u32,
    pub average_reaction_time_ms: f32,
    pub average_accuracy: f32,
    pub session_duration_secs: u32,
}

// ============================================================================
// Messages: Arduino → Core (over USB Serial)
// ============================================================================

/// Messages received from Arduino over serial.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum ArduinoMessage {
    /// Raw sensor reading
    Sensor(SensorReading),
    /// Arduino is ready/connected
    Ready { firmware_version: String },
    /// Error from Arduino
    Error { message: String },
}

// ============================================================================
// Messages: Core ↔ Unity (over WebSocket)
// ============================================================================

/// Messages sent from Core to Unity.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum CoreToUnityMessage {
    /// Notify Unity of a detected tap
    TapDetected(TapEvent),
    /// Activate a target button in AR
    ActivateTarget { target_id: u32 },
    /// Deactivate a target
    DeactivateTarget { target_id: u32 },
    /// Start a new session
    SessionStart { session_id: String },
    /// End the current session
    SessionEnd { stats: SessionStats },
    /// Current sensor state (for live visualization)
    SensorState {
        index_pressure: f32,
        middle_pressure: f32,
    },
}

/// Messages sent from Unity to Core.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum UnityToCoreMessage {
    /// Unity client connected and ready
    Ready { client_version: String },
    /// User tapped a target in AR
    TargetHit {
        target_id: u32,
        hit_position: [f32; 3],
        timestamp: DateTime<Utc>,
    },
    /// Request to start a session
    RequestSessionStart,
    /// Request to end the session
    RequestSessionEnd,
    /// Unity client disconnecting
    Disconnect,
}
