<p align="center">
  <img src="game/Assets/sollertia_white.svg" alt="Sollertia Logo" width="360" style="background-color:#0a0e17; padding: 24px 32px; border-radius: 12px;">
</p>

<h3 align="center">A mixed-reality dexterity rehabilitation system for stroke recovery patients.</h3>

<p align="center">Built at UGAHacks XI.</p>

---

## What It Does

Sollertia is an AR/VR tool that helps stroke patients regain fine motor control in their fingers. Patients wear a Meta Quest 3 headset and interact with illuminated buttons on a virtual table surface using their index and middle fingers. The system tracks touch accuracy and response time across a timed session, giving clinicians measurable data on dexterity recovery.

## Why It Matters

Stroke survivors often lose fine motor control, and traditional rehabilitation exercises can feel repetitive and unmotivating. Sollertia turns finger dexterity training into a focused, trackable activity inside an immersive environment — making sessions more engaging for patients while giving clinicians concrete performance data.

## How It Works

1. **Clinician launches the app** on Meta Quest 3 and is greeted by a clean main menu with a settings panel (AR passthrough toggle, session config).
2. **Patient begins a session** by tapping the "Begin Session" button — no controllers needed, just hand tracking.
3. **Buttons light up one at a time** on a virtual table. The patient reaches out and presses them with their fingertip.
4. **Session ends after 45 seconds**, displaying total buttons pressed. The clinician records the result and can run another session by restarting the app.

## Tech Stack

- **Unity 6** (6000.3.7f1) with Universal Render Pipeline
- **OpenXR** + XR Interaction Toolkit for cross-platform VR
- **Meta Quest 3** hand tracking (no controllers required)
- **C#** — single self-contained script that creates the entire experience at runtime

## Project Structure

```text
sollertia/
├── dashboard/      # Rust-based core dashboard (hardware integration)
├── game/           # Unity VR application (this is the demo)
│   └── Assets/
│       ├── Scripts/Sollertia/
│       │   ├── SollertiaDemo.cs          # Main game — creates everything at runtime
│       │   ├── SETUP_INSTRUCTIONS.md     # How to set up in Unity
│       │   └── QUEST_DEPLOYMENT.md       # How to deploy to Quest 3
│       ├── Settings/                     # XR plugin config
│       └── SollertiaDemo.unity           # Scene file
├── hardware/       # Arduino pressure sensor code
└── README.md
```

## Running the Demo

1. Open `game/` in Unity 6 (2023.3+)
2. Open `Assets/SollertiaDemo.unity`
3. Ensure XR Plug-in Management has OpenXR enabled
4. Press Play — the game creates everything at runtime from a single empty GameObject

For Quest 3 deployment, see [`QUEST_DEPLOYMENT.md`](game/Assets/Scripts/Sollertia/QUEST_DEPLOYMENT.md).

## Future Directions

- **Biosensing integration** — pressure sensors on fingertips to capture actual force data during presses, giving clinicians richer dexterity metrics
- **Physiological monitoring** — heart rate, galvanic skin response, and other biosignals to track patient stress and engagement during sessions
- **Longitudinal tracking** — session-over-session dashboards so clinicians can visualize recovery trends over weeks and months
- **Adaptive difficulty** — automatically adjust button speed and spacing based on patient performance

## Team

Built by the Sollertia team at UGAHacks XI.

## License

GPLv3
