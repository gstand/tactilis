//! Main application state and UI.

use crate::arduino::{self, ArduinoConfig, ArduinoHandle};
use crate::protocol::{ArduinoMessage, CoreToUnityMessage, Finger, SessionStats, TapEvent, UnityToCoreMessage};
use crate::unity::{self, UnityServerConfig, UnityServerHandle};
use chrono::Utc;
use eframe::egui;
use std::collections::VecDeque;

/// Maximum number of sensor readings to keep in history.
const SENSOR_HISTORY_SIZE: usize = 200;

/// Pressure threshold to detect a tap.
const TAP_THRESHOLD: f32 = 0.3;

/// Main application state.
pub struct TactilisApp {
    // Connection state
    arduino_config: ArduinoConfig,
    arduino_handle: Option<ArduinoHandle>,
    available_ports: Vec<String>,

    unity_config: UnityServerConfig,
    unity_handle: Option<UnityServerHandle>,
    unity_connected: bool,

    // Tokio runtime for async operations
    runtime: tokio::runtime::Runtime,

    // Sensor data
    index_pressure: f32,
    middle_pressure: f32,
    index_history: VecDeque<f32>,
    middle_history: VecDeque<f32>,

    // Tap detection state
    index_was_pressed: bool,
    middle_was_pressed: bool,

    // Session state
    session_active: bool,
    session_stats: SessionStats,
    tap_log: Vec<TapEvent>,

    // UI state
    status_messages: VecDeque<String>,
}

impl TactilisApp {
    pub fn new(_cc: &eframe::CreationContext<'_>) -> Self {
        let runtime = tokio::runtime::Runtime::new().expect("Failed to create Tokio runtime");

        let mut app = Self {
            arduino_config: ArduinoConfig::default(),
            arduino_handle: None,
            available_ports: Vec::new(),

            unity_config: UnityServerConfig::default(),
            unity_handle: None,
            unity_connected: false,

            runtime,

            index_pressure: 0.0,
            middle_pressure: 0.0,
            index_history: VecDeque::with_capacity(SENSOR_HISTORY_SIZE),
            middle_history: VecDeque::with_capacity(SENSOR_HISTORY_SIZE),

            index_was_pressed: false,
            middle_was_pressed: false,

            session_active: false,
            session_stats: SessionStats::default(),
            tap_log: Vec::new(),

            status_messages: VecDeque::with_capacity(50),
        };

        app.refresh_ports();
        app.log_status("Tactilis Dashboard initialized");

        // Auto-start Unity server
        app.start_unity_server();

        app
    }

    fn log_status(&mut self, msg: &str) {
        let timestamp = Utc::now().format("%H:%M:%S");
        self.status_messages.push_front(format!("[{}] {}", timestamp, msg));
        if self.status_messages.len() > 50 {
            self.status_messages.pop_back();
        }
        tracing::info!("{}", msg);
    }

    fn refresh_ports(&mut self) {
        self.available_ports = arduino::list_available_ports();
    }

    fn connect_arduino(&mut self) {
        if self.arduino_config.port_name.is_empty() {
            self.log_status("Please select a serial port");
            return;
        }

        match arduino::connect(self.arduino_config.clone()) {
            Ok(handle) => {
                self.arduino_handle = Some(handle);
                self.log_status(&format!("Connected to Arduino on {}", self.arduino_config.port_name));
            }
            Err(e) => {
                self.log_status(&format!("Failed to connect: {}", e));
            }
        }
    }

    fn disconnect_arduino(&mut self) {
        self.arduino_handle = None;
        self.log_status("Disconnected from Arduino");
    }

    fn start_unity_server(&mut self) {
        let config = self.unity_config.clone();
        let handle = self.runtime.handle().clone();

        match self.runtime.block_on(unity::start_server(config, &handle)) {
            Ok(server_handle) => {
                self.unity_handle = Some(server_handle);
                self.log_status(&format!(
                    "Unity server started on ws://{}:{}",
                    self.unity_config.host, self.unity_config.port
                ));
            }
            Err(e) => {
                self.log_status(&format!("Failed to start Unity server: {}", e));
            }
        }
    }

    fn process_arduino_messages(&mut self) {
        // Collect messages first to avoid borrow issues
        let messages: Vec<ArduinoMessage> = self
            .arduino_handle
            .as_ref()
            .map(|h| h.receiver.try_iter().collect())
            .unwrap_or_default();

        for msg in messages {
            match msg {
                ArduinoMessage::Sensor(reading) => {
                    match reading.finger {
                        Finger::Index => {
                            self.index_pressure = reading.pressure;
                            self.index_history.push_back(reading.pressure);
                            if self.index_history.len() > SENSOR_HISTORY_SIZE {
                                self.index_history.pop_front();
                            }

                            // Tap detection
                            let pressed = reading.pressure > TAP_THRESHOLD;
                            if pressed && !self.index_was_pressed {
                                self.on_tap_detected(Finger::Index, reading.pressure);
                            }
                            self.index_was_pressed = pressed;
                        }
                        Finger::Middle => {
                            self.middle_pressure = reading.pressure;
                            self.middle_history.push_back(reading.pressure);
                            if self.middle_history.len() > SENSOR_HISTORY_SIZE {
                                self.middle_history.pop_front();
                            }

                            let pressed = reading.pressure > TAP_THRESHOLD;
                            if pressed && !self.middle_was_pressed {
                                self.on_tap_detected(Finger::Middle, reading.pressure);
                            }
                            self.middle_was_pressed = pressed;
                        }
                    }

                    // Forward sensor state to Unity
                    self.send_to_unity(CoreToUnityMessage::SensorState {
                        index_pressure: self.index_pressure,
                        middle_pressure: self.middle_pressure,
                    });
                }
                ArduinoMessage::Ready { firmware_version } => {
                    self.log_status(&format!("Arduino ready (firmware: {})", firmware_version));
                }
                ArduinoMessage::Error { message } => {
                    self.log_status(&format!("Arduino error: {}", message));
                }
            }
        }
    }

    fn process_unity_messages(&mut self) {
        // Update connection state
        if let Some(handle) = &self.unity_handle {
            if let Ok(state) = handle.state.try_read() {
                self.unity_connected = state.connected;
            }
        }

        // Collect messages first to avoid borrow issues
        let mut messages = Vec::new();
        if let Some(handle) = &mut self.unity_handle {
            while let Ok(msg) = handle.receiver.try_recv() {
                messages.push(msg);
            }
        }

        for msg in messages {
            match msg {
                UnityToCoreMessage::Ready { client_version } => {
                    self.log_status(&format!("Unity client connected (v{})", client_version));
                }
                UnityToCoreMessage::TargetHit { target_id, .. } => {
                    self.log_status(&format!("Target {} hit", target_id));
                    if self.session_active {
                        self.session_stats.total_taps += 1;
                        self.session_stats.successful_taps += 1;
                    }
                }
                UnityToCoreMessage::RequestSessionStart => {
                    self.start_session();
                }
                UnityToCoreMessage::RequestSessionEnd => {
                    self.end_session();
                }
                UnityToCoreMessage::Disconnect => {
                    self.log_status("Unity client disconnected");
                }
            }
        }
    }

    fn on_tap_detected(&mut self, finger: Finger, pressure: f32) {
        let event = TapEvent {
            finger,
            pressure_peak: pressure,
            duration_ms: 0, // TODO: Calculate actual duration
            timestamp: Utc::now(),
        };

        self.tap_log.push(event.clone());
        if self.tap_log.len() > 100 {
            self.tap_log.remove(0);
        }

        // Forward to Unity
        self.send_to_unity(CoreToUnityMessage::TapDetected(event));
    }

    fn send_to_unity(&self, msg: CoreToUnityMessage) {
        if let Some(handle) = &self.unity_handle {
            let _ = handle.send(msg);
        }
    }

    fn start_session(&mut self) {
        self.session_active = true;
        self.session_stats = SessionStats::default();
        self.tap_log.clear();

        let session_id = Utc::now().format("%Y%m%d_%H%M%S").to_string();
        self.log_status(&format!("Session started: {}", session_id));

        self.send_to_unity(CoreToUnityMessage::SessionStart { session_id });
    }

    fn end_session(&mut self) {
        self.session_active = false;
        self.log_status("Session ended");

        self.send_to_unity(CoreToUnityMessage::SessionEnd {
            stats: self.session_stats.clone(),
        });
    }
}

impl eframe::App for TactilisApp {
    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
        // Process incoming messages
        self.process_arduino_messages();
        self.process_unity_messages();

        // Request continuous repaint for real-time updates
        ctx.request_repaint();

        // Top panel with connection controls
        egui::TopBottomPanel::top("top_panel").show(ctx, |ui| {
            ui.horizontal(|ui| {
                ui.heading("Tactilis Dashboard");
                ui.separator();

                // Arduino connection
                ui.label("Arduino:");
                if self.arduino_handle.is_some() {
                    ui.colored_label(egui::Color32::GREEN, "●");
                    if ui.button("Disconnect").clicked() {
                        self.disconnect_arduino();
                    }
                } else {
                    ui.colored_label(egui::Color32::RED, "●");
                    egui::ComboBox::from_id_salt("port_select")
                        .selected_text(if self.arduino_config.port_name.is_empty() {
                            "Select port..."
                        } else {
                            &self.arduino_config.port_name
                        })
                        .show_ui(ui, |ui| {
                            for port in &self.available_ports {
                                ui.selectable_value(
                                    &mut self.arduino_config.port_name,
                                    port.clone(),
                                    port,
                                );
                            }
                        });
                    if ui.button("🔄").on_hover_text("Refresh ports").clicked() {
                        self.refresh_ports();
                    }
                    if ui.button("Connect").clicked() {
                        self.connect_arduino();
                    }
                }

                ui.separator();

                // Unity connection status
                ui.label("Unity:");
                if self.unity_connected {
                    ui.colored_label(egui::Color32::GREEN, "● Connected");
                } else {
                    ui.colored_label(egui::Color32::YELLOW, "● Waiting");
                }
            });
        });

        // Left panel with sensor visualization
        egui::SidePanel::left("sensor_panel")
            .min_width(300.0)
            .show(ctx, |ui| {
                ui.heading("Sensor Data");
                ui.separator();

                // Current pressure values
                ui.horizontal(|ui| {
                    ui.label("Index finger:");
                    ui.add(
                        egui::ProgressBar::new(self.index_pressure)
                            .text(format!("{:.1}%", self.index_pressure * 100.0)),
                    );
                });

                ui.horizontal(|ui| {
                    ui.label("Middle finger:");
                    ui.add(
                        egui::ProgressBar::new(self.middle_pressure)
                            .text(format!("{:.1}%", self.middle_pressure * 100.0)),
                    );
                });

                ui.separator();

                // Pressure history graph
                ui.label("Pressure History");

                let index_points: egui_plot::PlotPoints = self
                    .index_history
                    .iter()
                    .enumerate()
                    .map(|(i, &p)| [i as f64, p as f64])
                    .collect();

                let middle_points: egui_plot::PlotPoints = self
                    .middle_history
                    .iter()
                    .enumerate()
                    .map(|(i, &p)| [i as f64, p as f64])
                    .collect();

                egui_plot::Plot::new("pressure_plot")
                    .height(200.0)
                    .include_y(0.0)
                    .include_y(1.0)
                    .show(ui, |plot_ui| {
                        plot_ui.line(
                            egui_plot::Line::new(index_points)
                                .name("Index")
                                .color(egui::Color32::LIGHT_BLUE),
                        );
                        plot_ui.line(
                            egui_plot::Line::new(middle_points)
                                .name("Middle")
                                .color(egui::Color32::LIGHT_GREEN),
                        );
                        // Threshold line
                        plot_ui.hline(
                            egui_plot::HLine::new(TAP_THRESHOLD as f64)
                                .name("Tap threshold")
                                .color(egui::Color32::RED)
                                .style(egui_plot::LineStyle::dashed_dense()),
                        );
                    });
            });

        // Central panel with session controls and stats
        egui::CentralPanel::default().show(ctx, |ui| {
            ui.heading("Session");
            ui.separator();

            ui.horizontal(|ui| {
                if self.session_active {
                    if ui.button("⏹ End Session").clicked() {
                        self.end_session();
                    }
                    ui.colored_label(egui::Color32::GREEN, "Session Active");
                } else {
                    if ui.button("▶ Start Session").clicked() {
                        self.start_session();
                    }
                }
            });

            ui.separator();

            // Session statistics
            ui.heading("Statistics");
            egui::Grid::new("stats_grid").show(ui, |ui| {
                ui.label("Total taps:");
                ui.label(self.session_stats.total_taps.to_string());
                ui.end_row();

                ui.label("Successful taps:");
                ui.label(self.session_stats.successful_taps.to_string());
                ui.end_row();

                ui.label("Avg reaction time:");
                ui.label(format!("{:.0} ms", self.session_stats.average_reaction_time_ms));
                ui.end_row();

                ui.label("Avg accuracy:");
                ui.label(format!("{:.1}%", self.session_stats.average_accuracy * 100.0));
                ui.end_row();
            });

            ui.separator();

            // Recent taps log
            ui.heading("Recent Taps");
            egui::ScrollArea::vertical()
                .max_height(150.0)
                .show(ui, |ui| {
                    for tap in self.tap_log.iter().rev().take(10) {
                        ui.horizontal(|ui| {
                            let finger_str = match tap.finger {
                                Finger::Index => "Index",
                                Finger::Middle => "Middle",
                            };
                            ui.label(tap.timestamp.format("%H:%M:%S").to_string());
                            ui.label(finger_str);
                            ui.label(format!("{:.0}%", tap.pressure_peak * 100.0));
                        });
                    }
                });

            ui.separator();

            // Status log
            ui.heading("Status Log");
            egui::ScrollArea::vertical()
                .max_height(150.0)
                .show(ui, |ui| {
                    for msg in &self.status_messages {
                        ui.label(msg);
                    }
                });
        });
    }
}
