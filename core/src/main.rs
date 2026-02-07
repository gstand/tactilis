mod app;
mod arduino;
mod protocol;
mod unity;

use app::TactilisApp;
use tracing_subscriber::EnvFilter;

fn main() -> eframe::Result<()> {
    // Initialize logging
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::from_default_env().add_directive("tactilis=debug".parse().unwrap()))
        .init();

    tracing::info!("Starting Tactilis Core Dashboard");

    let native_options = eframe::NativeOptions {
        viewport: egui::ViewportBuilder::default()
            .with_inner_size([1024.0, 768.0])
            .with_min_inner_size([800.0, 600.0])
            .with_title("Tactilis Dashboard"),
        ..Default::default()
    };

    eframe::run_native(
        "Tactilis Dashboard",
        native_options,
        Box::new(|cc| Ok(Box::new(TactilisApp::new(cc)))),
    )
}
