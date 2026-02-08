# Sollertia - Setup Guide

## Overview

**Sollertia** is a rehabilitation-focused AR game for Meta Quest 3. Players press virtual buttons on a table surface using their fingers, with pressure sensors tracking actual contact.

The unified system uses:
- **Hybrid table calibration** - Auto-detects tables via Room Setup, falls back to manual placement
- **TactilisButton** - Collision-based button interaction with visual feedback
- **TactilisHandTracker** - XR Hands subsystem for finger tracking
- **SollertiaUI** - Unified UI for all game states

## Core Components

| Component | Purpose |
|-----------|---------|
| `TactilisGame.cs` | Main game controller - manages all phases |
| `SollertiaCalibrationSystem.cs` | Hybrid auto/manual table detection |
| `TactilisHandTracker.cs` | XR Hands finger tracking + gestures |
| `TactilisButton.cs` | Button with Blue/Red colors, collision detection |
| `TactilisFingerTip.cs` | Finger collider for button interaction |
| `SollertiaUI.cs` | Unified UI (messages, HUD, game over) |

## Quick Start

### 1. Open Unity and Import Packages

Unity will import `com.unity.xr.meta-openxr`. This may take a few minutes.

### 2. Enable OpenXR Features

1. **Edit > Project Settings > XR Plug-in Management**
2. Select **Android** tab, enable **OpenXR**
3. Click **OpenXR** in left panel
4. Enable under **Meta Quest Support**:
   - Hand Tracking Subsystem
   - Scene Understanding
   - Plane Detection

### 3. Scene Setup

Create a GameObject with these components:

```
SollertiaGame (GameObject)
├── TactilisGame
├── SollertiaCalibrationSystem
├── TactilisHandTracker
└── SollertiaUI
```

Plus a **Button Grid** with `TactilisButton` components on each button.

### 4. Configure TactilisGame

In Inspector, assign:
- **Game UI**: Your `SollertiaUI` component
- **Hand Tracker**: Your `TactilisHandTracker` component
- **Calibration System**: Your `SollertiaCalibrationSystem` component
- **Button Grid Root**: Parent transform of your buttons
- **Buttons**: Array of `TactilisButton` components

### 5. Configure SollertiaCalibrationSystem

- **Plane Manager**: Your scene's `ARPlaneManager`
- **Button Grid**: Same as TactilisGame's buttonGridRoot
- **Index Finger Tip**: Will be auto-created by TactilisHandTracker

## Game Flow

```
Initializing → WaitingForTable → PlacingGrid → Countdown → Playing → GameOver
                     ↑                                              ↓
                     └──────────── RestartWithCalibration ──────────┘
```

### With Room Setup (Best Experience)

1. Game starts → Hand tracking initializes
2. Auto-detection scans for table surfaces (5s timeout)
3. Table detected → Grid auto-placed
4. 3-2-1-GO countdown
5. Gameplay: Blue/Red buttons, left/right hand matching
6. Game over → Show score, restart options

### Without Room Setup (Fallback)

1. Auto-detection times out after 5 seconds
2. Switches to manual mode
3. User pinches to place grid at finger position
4. Pinch again to confirm
5. Countdown and gameplay proceed normally

## Controls

| Action | Hand Gesture | Keyboard (Editor) |
|--------|--------------|-------------------|
| Select/Confirm | Pinch | P |
| Tap button | Touch with fingertip | 1-9 keys |
| Restart (game over) | Pinch | R |

## Gameplay Rules

- **Blue buttons** → Tap with **left hand** (index finger)
- **Red buttons** → Tap with **right hand** (index finger)
- **+10 points** for correct color match
- **-5 points** for wrong color
- **-3 points** if button times out
- Game lasts **30 seconds**

## Testing

### In Editor

1. Use XR Device Simulator
2. Press **P** to simulate pinch gesture
3. Press **1-9** to tap buttons (odd = left hand, even = right hand)
4. Press **R** to restart after game over

### On Quest 3

1. Complete **Room Setup** first: Settings > Physical Space > Space Setup
2. Mark your table during room scan
3. Build and deploy to Quest 3
4. Launch app - table should auto-detect

## Troubleshooting

### No table detected

- Complete Room Setup on Quest 3
- Mark the table during room scan
- Table must be 0.5m - 1.0m height
- Table surface must be at least 0.1m² area

### Hand tracking not working

- Ensure hands are visible to Quest cameras
- Check Hand Tracking is enabled in Quest settings
- Verify XR Hands subsystem is found (check console logs)

### Buttons not responding

- Ensure `TactilisFingerTip` colliders are created
- Check buttons have `TactilisButton` component
- Verify colliders are set as triggers

## Architecture

```
TactilisGame (main controller)
├── SollertiaCalibrationSystem
│   ├── Auto mode (ARPlaneManager + Room Setup)
│   └── Manual mode (pinch to place)
├── TactilisHandTracker
│   ├── Left index → TactilisFingerTip (Blue)
│   └── Right index → TactilisFingerTip (Red)
├── TactilisButton[] (button grid)
│   └── OnTriggerEnter → TactilisFingerTip collision
└── SollertiaUI
    ├── Message panel (calibration, countdown)
    ├── Game HUD (score, timer)
    └── Game over panel
```

## Legacy Support

The old `GameController` + `CalibrationManager` + `WhackAMoleGameManager` system is still available but deprecated. Use `TactilisGame` as the single entry point for new development.
