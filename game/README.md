# Rehab Tool

A mixed reality game for stroke rehabilitation. Built for Meta Quest 3.

## What it does

Players press virtual buttons on a grid using their fingers. We use pressure sensors (FRS) attached to the fingers to detect when they actually press down. The game tracks hits, misses, and reaction times over a 30-second session.

## Hardware

- Meta Quest 3
- Elegoo Uno R3 (or any Arduino with CH340/FTDI chip)
- 2x Force Resistive Sensors (index + middle finger)
- USB OTG cable to connect Arduino to Quest

## Quick Start

1. Open project in Unity 6
2. Install required packages (AR Foundation, XR Hands, Meta OpenXR)
3. Upload the Arduino sketch to your Elegoo board
4. Build and deploy to Quest 3
5. Connect Arduino via USB OTG
