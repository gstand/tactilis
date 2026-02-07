//! Unity WebSocket communication handler.
//!
//! Runs a WebSocket server that Unity connects to. Handles bidirectional
//! message passing between the core dashboard and the AR game.

use crate::protocol::{CoreToUnityMessage, UnityToCoreMessage};
use futures_util::{SinkExt, StreamExt};
use std::net::SocketAddr;
use std::sync::Arc;
use tokio::net::{TcpListener, TcpStream};
use tokio::sync::{broadcast, mpsc, RwLock};
use tokio_tungstenite::tungstenite::Message;
use thiserror::Error;

#[derive(Error, Debug)]
pub enum UnityError {
    #[error("WebSocket error: {0}")]
    WebSocket(#[from] tokio_tungstenite::tungstenite::Error),
    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),
    #[error("JSON error: {0}")]
    Json(#[from] serde_json::Error),
    #[error("No Unity client connected")]
    NotConnected,
}

/// Server configuration.
#[derive(Clone)]
pub struct UnityServerConfig {
    pub host: String,
    pub port: u16,
}

impl Default for UnityServerConfig {
    fn default() -> Self {
        Self {
            host: "127.0.0.1".to_string(),
            port: 8765,
        }
    }
}

/// Shared state for the Unity server.
pub struct UnityServerState {
    pub connected: bool,
    pub client_version: Option<String>,
}

impl Default for UnityServerState {
    fn default() -> Self {
        Self {
            connected: false,
            client_version: None,
        }
    }
}

/// Handle to the Unity WebSocket server.
pub struct UnityServerHandle {
    /// Send messages to Unity
    pub sender: broadcast::Sender<CoreToUnityMessage>,
    /// Receive messages from Unity
    pub receiver: mpsc::Receiver<UnityToCoreMessage>,
    /// Shared connection state
    pub state: Arc<RwLock<UnityServerState>>,
    /// Shutdown signal
    shutdown_tx: mpsc::Sender<()>,
}

impl UnityServerHandle {
    /// Send a message to the connected Unity client.
    pub fn send(&self, msg: CoreToUnityMessage) -> Result<(), UnityError> {
        self.sender.send(msg).map_err(|_| UnityError::NotConnected)?;
        Ok(())
    }

    /// Shutdown the server.
    pub async fn shutdown(&self) {
        let _ = self.shutdown_tx.send(()).await;
    }
}

/// Start the Unity WebSocket server.
///
/// This spawns a background task that listens for connections.
pub async fn start_server(
    config: UnityServerConfig,
    runtime: &tokio::runtime::Handle,
) -> Result<UnityServerHandle, UnityError> {
    let addr = format!("{}:{}", config.host, config.port);
    let listener = TcpListener::bind(&addr).await?;
    tracing::info!("Unity WebSocket server listening on ws://{}", addr);

    let (outgoing_tx, _) = broadcast::channel::<CoreToUnityMessage>(100);
    let (incoming_tx, incoming_rx) = mpsc::channel::<UnityToCoreMessage>(100);
    let (shutdown_tx, mut shutdown_rx) = mpsc::channel::<()>(1);

    let state = Arc::new(RwLock::new(UnityServerState::default()));

    let outgoing_tx_clone = outgoing_tx.clone();
    let state_clone = state.clone();

    runtime.spawn(async move {
        loop {
            tokio::select! {
                result = listener.accept() => {
                    match result {
                        Ok((stream, addr)) => {
                            tracing::info!("Unity client connecting from: {}", addr);
                            let outgoing_rx = outgoing_tx_clone.subscribe();
                            let incoming_tx = incoming_tx.clone();
                            let state = state_clone.clone();

                            tokio::spawn(handle_connection(
                                stream,
                                addr,
                                outgoing_rx,
                                incoming_tx,
                                state,
                            ));
                        }
                        Err(e) => {
                            tracing::error!("Failed to accept connection: {}", e);
                        }
                    }
                }
                _ = shutdown_rx.recv() => {
                    tracing::info!("Unity server shutting down");
                    break;
                }
            }
        }
    });

    Ok(UnityServerHandle {
        sender: outgoing_tx,
        receiver: incoming_rx,
        state,
        shutdown_tx,
    })
}

async fn handle_connection(
    stream: TcpStream,
    addr: SocketAddr,
    mut outgoing_rx: broadcast::Receiver<CoreToUnityMessage>,
    incoming_tx: mpsc::Sender<UnityToCoreMessage>,
    state: Arc<RwLock<UnityServerState>>,
) {
    let ws_stream = match tokio_tungstenite::accept_async(stream).await {
        Ok(ws) => ws,
        Err(e) => {
            tracing::error!("WebSocket handshake failed for {}: {}", addr, e);
            return;
        }
    };

    tracing::info!("Unity client connected: {}", addr);
    {
        let mut state = state.write().await;
        state.connected = true;
    }

    let (mut ws_sender, mut ws_receiver) = ws_stream.split();

    // Task for sending messages to Unity
    let send_task = async {
        while let Ok(msg) = outgoing_rx.recv().await {
            match serde_json::to_string(&msg) {
                Ok(json) => {
                    if let Err(e) = ws_sender.send(Message::Text(json)).await {
                        tracing::error!("Failed to send to Unity: {}", e);
                        break;
                    }
                }
                Err(e) => {
                    tracing::error!("Failed to serialize message: {}", e);
                }
            }
        }
    };

    // Task for receiving messages from Unity
    let recv_task = async {
        while let Some(result) = ws_receiver.next().await {
            match result {
                Ok(Message::Text(text)) => {
                    match serde_json::from_str::<UnityToCoreMessage>(&text) {
                        Ok(msg) => {
                            // Update state if it's a Ready message
                            if let UnityToCoreMessage::Ready { ref client_version } = msg {
                                let mut state = state.write().await;
                                state.client_version = Some(client_version.clone());
                            }

                            if incoming_tx.send(msg).await.is_err() {
                                break;
                            }
                        }
                        Err(e) => {
                            tracing::warn!("Failed to parse Unity message: {}", e);
                        }
                    }
                }
                Ok(Message::Close(_)) => {
                    tracing::info!("Unity client closed connection");
                    break;
                }
                Ok(Message::Ping(_)) => {
                    // Pings handled automatically by tungstenite
                }
                Ok(_) => {} // Ignore other message types
                Err(e) => {
                    tracing::error!("WebSocket error: {}", e);
                    break;
                }
            }
        }
    };

    // Run both tasks until one completes
    tokio::select! {
        _ = send_task => {}
        _ = recv_task => {}
    }

    // Update state on disconnect
    {
        let mut state = state.write().await;
        state.connected = false;
        state.client_version = None;
    }

    tracing::info!("Unity client disconnected: {}", addr);
}
