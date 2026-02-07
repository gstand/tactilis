# AGENTS.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Tactilis is a UGAHacks project. Licensed under GPLv3. The project is a diagnostic tool intended to serve as a rehabilitation tool for people with dexterity disabilities (e.g. stroke), in where patients are equipped with pressure sensors on their index and middle finger, and press buttons scattered throught a surface in the AR environment. Dexterity data (time to tap, accuracy) is tracked to track recovery. The core dashboard is the central piece of the project, in where it is a native application that takes communication over USB from an Arduino which measures the pressure sensors. The core dashboard also communicates with the Unity AR environment to show visual cues for these actions in the AR environment.

## Architecture

The project is organized into three components:

- **core/** - Core dashboard UI (expected: Rust)
- **game/** - Game client (expected: Unity/C#)
- **hardware/** - Hardware/embedded code (expected: C++ or Rust)

## Build Commands

Build systems are not yet configured. When set up, expect:

### Rust (core/)
```bash
cargo build              # Debug build
cargo build --release    # Release build
cargo test               # Run tests
cargo clippy             # Lint
cargo fmt                # Format code
```

### Unity (game/)
Unity projects are built through the Unity Editor or via command line:
```bash
# Build from command line (path will vary based on Unity installation)
/Applications/Unity/Hub/Editor/[VERSION]/Unity.app/Contents/MacOS/Unity -batchmode -projectPath ./game -buildTarget StandaloneOSX -quit
```

### C++ (hardware/)
If using CMake:
```bash
cmake -B build -S hardware
cmake --build build
```

## Cross-Component Communication

When implementing communication between components:
- Define shared data structures in `core/`
- Use serialization formats compatible across Rust/C#/C++ (consider MessagePack, Protocol Buffers, or JSON)
