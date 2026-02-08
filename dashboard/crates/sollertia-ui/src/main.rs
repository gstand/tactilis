use std::{
    collections::VecDeque,
    sync::{Arc, Mutex},
    thread,
    time::{Duration, Instant},
};

use eframe::egui;
use futures_util::{SinkExt, StreamExt};
use serde::{Deserialize, Serialize};
use serial2::SerialPort;
use sollertia_core::{Finger, Pressure};
use tokio::sync::broadcast;

const PRESSURE_THRESHOLD: u16 = 100;
const WEBSOCKET_PORT: u16 = 9000;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PressEvent {
    pub finger: Finger,
    pub pressure: u16,
    pub timestamp_ms: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UnityResponse {
    pub finger_position: [f32; 3],
    pub target_position: [f32; 3],
    pub timestamp_ms: u64,
}

#[derive(Debug, Clone)]
pub struct AccuracyRecord {
    pub finger: Finger,
    pub distance: f32,
    pub timestamp: Instant,
    pub reaction_time_ms: u64,
}

#[derive(Default)]
struct SharedState {
    index_pressure: u16,
    middle_pressure: u16,
    accuracy_records: VecDeque<AccuracyRecord>,
    connected_clients: usize,
    serial_connected: bool,
    last_press_time: Option<Instant>,
    pending_press: Option<PressEvent>,
}

fn main() -> eframe::Result<()> {
    let state = Arc::new(Mutex::new(SharedState::default()));
    let (press_tx, _) = broadcast::channel::<PressEvent>(16);

    let serial_state = Arc::clone(&state);
    let serial_press_tx = press_tx.clone();
    thread::spawn(move || {
        run_serial_polling(serial_state, serial_press_tx);
    });

    let ws_state = Arc::clone(&state);
    thread::spawn(move || {
        let rt = tokio::runtime::Runtime::new().unwrap();
        rt.block_on(run_websocket_server(ws_state, press_tx));
    });

    let options = eframe::NativeOptions {
        viewport: egui::ViewportBuilder::default()
            .with_inner_size([800.0, 600.0])
            .with_title("Tactilis Dashboard"),
        ..Default::default()
    };

    eframe::run_native(
        "Tactilis Dashboard",
        options,
        Box::new(|_cc| Ok(Box::new(TactilisApp::new(state)))),
    )
}

struct TactilisApp {
    state: Arc<Mutex<SharedState>>,
    start_time: Instant,
}

impl TactilisApp {
    fn new(state: Arc<Mutex<SharedState>>) -> Self {
        Self {
            state,
            start_time: Instant::now(),
        }
    }
}

impl eframe::App for TactilisApp {
    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
        let state = self.state.lock().unwrap();

        egui::TopBottomPanel::top("header").show(ctx, |ui| {
            ui.horizontal(|ui| {
                ui.heading("Tactilis Dashboard");
                ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                    let elapsed = self.start_time.elapsed();
                    ui.label(format!(
                        "Session: {:02}:{:02}",
                        elapsed.as_secs() / 60,
                        elapsed.as_secs() % 60
                    ));
                });
            });
        });

        egui::SidePanel::left("status_panel")
            .resizable(false)
            .default_width(200.0)
            .show(ctx, |ui| {
                ui.heading("Connection Status");
                ui.separator();

                ui.horizontal(|ui| {
                    let (color, text) = if state.serial_connected {
                        (egui::Color32::GREEN, "● Connected")
                    } else {
                        (egui::Color32::RED, "● Disconnected")
                    };
                    ui.colored_label(color, text);
                    ui.label("Serial");
                });

                ui.horizontal(|ui| {
                    let (color, text) = if state.connected_clients > 0 {
                        (egui::Color32::GREEN, format!("● {} client(s)", state.connected_clients))
                    } else {
                        (egui::Color32::YELLOW, "● Waiting...".to_string())
                    };
                    ui.colored_label(
                        if state.connected_clients > 0 {
                            egui::Color32::GREEN
                        } else {
                            egui::Color32::YELLOW
                        },
                        &text,
                    );
                    ui.label("WebSocket");
                });

                ui.add_space(20.0);
                ui.heading("Pressure Readings");
                ui.separator();

                ui.horizontal(|ui| {
                    ui.label("Index Finger:");
                    let bar_width = (state.index_pressure as f32 / 1023.0) * 100.0;
                    let bar = egui::ProgressBar::new(bar_width / 100.0)
                        .text(format!("{}", state.index_pressure));
                    ui.add(bar);
                });

                ui.horizontal(|ui| {
                    ui.label("Middle Finger:");
                    let bar_width = (state.middle_pressure as f32 / 1023.0) * 100.0;
                    let bar = egui::ProgressBar::new(bar_width / 100.0)
                        .text(format!("{}", state.middle_pressure));
                    ui.add(bar);
                });

                ui.add_space(10.0);
                ui.label(format!("Threshold: {}", PRESSURE_THRESHOLD));
            });

        egui::CentralPanel::default().show(ctx, |ui| {
            ui.heading("Accuracy History");
            ui.separator();

            if state.accuracy_records.is_empty() {
                ui.centered_and_justified(|ui| {
                    ui.label("No accuracy data yet. Press targets in AR to record data.");
                });
            } else {
                egui::ScrollArea::vertical().show(ui, |ui| {
                    egui::Grid::new("accuracy_grid")
                        .num_columns(4)
                        .striped(true)
                        .min_col_width(100.0)
                        .show(ui, |ui| {
                            ui.strong("Finger");
                            ui.strong("Distance (cm)");
                            ui.strong("Reaction (ms)");
                            ui.strong("Time");
                            ui.end_row();

                            for record in state.accuracy_records.iter().rev().take(50) {
                                let finger_str = match record.finger {
                                    Finger::Index => "Index",
                                    Finger::Middle => "Middle",
                                    _ => "Unknown",
                                };
                                ui.label(finger_str);
                                ui.label(format!("{:.2}", record.distance * 100.0));
                                ui.label(format!("{}", record.reaction_time_ms));
                                let elapsed = record.timestamp.elapsed();
                                ui.label(format!("{:.1}s ago", elapsed.as_secs_f32()));
                                ui.end_row();
                            }
                        });
                });

                ui.separator();
                ui.horizontal(|ui| {
                    let avg_distance: f32 = state
                        .accuracy_records
                        .iter()
                        .map(|r| r.distance)
                        .sum::<f32>()
                        / state.accuracy_records.len() as f32;
                    let avg_reaction: u64 = state
                        .accuracy_records
                        .iter()
                        .map(|r| r.reaction_time_ms)
                        .sum::<u64>()
                        / state.accuracy_records.len() as u64;

                    ui.label(format!(
                        "Average Distance: {:.2} cm | Average Reaction: {} ms | Total Presses: {}",
                        avg_distance * 100.0,
                        avg_reaction,
                        state.accuracy_records.len()
                    ));
                });
            }
        });

        drop(state);
        ctx.request_repaint_after(Duration::from_millis(50));
    }
}

fn run_serial_polling(state: Arc<Mutex<SharedState>>, press_tx: broadcast::Sender<PressEvent>) {
    loop {
        let port_result = SerialPort::open("/dev/cu.usbmodem1101", 9600);

        let port = match port_result {
            Ok(p) => {
                if let Ok(mut s) = state.lock() {
                    s.serial_connected = true;
                }
                p
            }
            Err(e) => {
                eprintln!("Failed to open serial port: {e}. Retrying in 2s...");
                if let Ok(mut s) = state.lock() {
                    s.serial_connected = false;
                }
                thread::sleep(Duration::from_secs(2));
                continue;
            }
        };

        thread::sleep(Duration::from_secs(2));

        if port.write_all(&[83]).is_err() || port.flush().is_err() {
            if let Ok(mut s) = state.lock() {
                s.serial_connected = false;
            }
            continue;
        }

        let mut buffer = [0u8; 4];
        let mut is_started = false;
        let mut prev_index_above = false;
        let mut prev_middle_above = false;
        let start_time = Instant::now();

        loop {
            if port.read_exact(&mut buffer).is_err() {
                if let Ok(mut s) = state.lock() {
                    s.serial_connected = false;
                }
                break;
            }

            let pressure_result = Pressure::from_serial(buffer);

            if let Ok(pressure) = pressure_result {
                if !is_started {
                    if pressure.is_start() {
                        is_started = true;
                    }
                    continue;
                }

                if let Ok(mut s) = state.lock() {
                    match pressure.finger {
                        Finger::Index => {
                            s.index_pressure = pressure.pressure;
                            let above = pressure.pressure > PRESSURE_THRESHOLD;
                            if above && !prev_index_above {
                                let event = PressEvent {
                                    finger: Finger::Index,
                                    pressure: pressure.pressure,
                                    timestamp_ms: start_time.elapsed().as_millis() as u64,
                                };
                                s.last_press_time = Some(Instant::now());
                                s.pending_press = Some(event.clone());
                                let _ = press_tx.send(event);
                            }
                            prev_index_above = above;
                        }
                        Finger::Middle => {
                            s.middle_pressure = pressure.pressure;
                            let above = pressure.pressure > PRESSURE_THRESHOLD;
                            if above && !prev_middle_above {
                                let event = PressEvent {
                                    finger: Finger::Middle,
                                    pressure: pressure.pressure,
                                    timestamp_ms: start_time.elapsed().as_millis() as u64,
                                };
                                s.last_press_time = Some(Instant::now());
                                s.pending_press = Some(event.clone());
                                let _ = press_tx.send(event);
                            }
                            prev_middle_above = above;
                        }
                        _ => {}
                    }
                }
            }
        }

        let _ = port.write_all(&[115]);
        let _ = port.flush();
    }
}

async fn run_websocket_server(
    state: Arc<Mutex<SharedState>>,
    press_tx: broadcast::Sender<PressEvent>,
) {
    let addr = format!("0.0.0.0:{}", WEBSOCKET_PORT);
    let listener = tokio::net::TcpListener::bind(&addr).await.unwrap();
    println!("WebSocket server listening on ws://{}", addr);

    loop {
        let (stream, peer) = match listener.accept().await {
            Ok(conn) => conn,
            Err(e) => {
                eprintln!("Failed to accept connection: {e}");
                continue;
            }
        };

        println!("New connection from: {}", peer);

        let ws_stream = match tokio_tungstenite::accept_async(stream).await {
            Ok(ws) => ws,
            Err(e) => {
                eprintln!("WebSocket handshake failed: {e}");
                continue;
            }
        };

        if let Ok(mut s) = state.lock() {
            s.connected_clients += 1;
        }

        let client_state = Arc::clone(&state);
        let mut press_rx = press_tx.subscribe();

        tokio::spawn(async move {
            let (mut ws_sender, mut ws_receiver) = ws_stream.split();

            loop {
                tokio::select! {
                    press_event = press_rx.recv() => {
                        if let Ok(event) = press_event {
                            let json = serde_json::to_string(&event).unwrap();
                            if ws_sender
                                .send(tokio_tungstenite::tungstenite::Message::Text(json.into()))
                                .await
                                .is_err()
                            {
                                break;
                            }
                        }
                    }
                    msg = ws_receiver.next() => {
                        match msg {
                            Some(Ok(tokio_tungstenite::tungstenite::Message::Text(text))) => {
                                if let Ok(response) = serde_json::from_str::<UnityResponse>(&text) {
                                    let distance = calculate_distance(
                                        response.finger_position,
                                        response.target_position,
                                    );

                                    if let Ok(mut s) = client_state.lock() {
                                        let reaction_time = s
                                            .last_press_time
                                            .map(|t| t.elapsed().as_millis() as u64)
                                            .unwrap_or(0);

                                        let finger = s
                                            .pending_press
                                            .as_ref()
                                            .map(|p| p.finger)
                                            .unwrap_or(Finger::Index);

                                        s.accuracy_records.push_back(AccuracyRecord {
                                            finger,
                                            distance,
                                            timestamp: Instant::now(),
                                            reaction_time_ms: reaction_time,
                                        });

                                        if s.accuracy_records.len() > 1000 {
                                            s.accuracy_records.pop_front();
                                        }

                                        s.pending_press = None;
                                    }
                                }
                            }
                            Some(Ok(tokio_tungstenite::tungstenite::Message::Close(_))) | None => {
                                break;
                            }
                            _ => {}
                        }
                    }
                }
            }

            if let Ok(mut s) = client_state.lock() {
                s.connected_clients = s.connected_clients.saturating_sub(1);
            }
            println!("Client disconnected: {}", peer);
        });
    }
}

fn calculate_distance(pos1: [f32; 3], pos2: [f32; 3]) -> f32 {
    let dx = pos1[0] - pos2[0];
    let dy = pos1[1] - pos2[1];
    let dz = pos1[2] - pos2[2];
    (dx * dx + dy * dy + dz * dz).sqrt()
}
