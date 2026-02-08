# Timer and Score Component for Meta Quest 3 Mixed Reality

A Unity-based UI system for Meta Quest 3 MR applications featuring a **30-second timer**, **score tracking**, and a **sleek end session popup** with a calming blue color scheme.

---

## 🎯 What This Package Provides

| Component | Description |
|-----------|-------------|
| **TimerUI** | Circular countdown timer (30 seconds) with progress indicator |
| **ScoreUI** | Real-time score display that animates on change |
| **EndSessionUI** | Beautiful popup showing final score when session ends |
| **GameSessionManager** | Central controller that manages the entire session |
| **ButtonClickHandler** | Helper script to easily add scoring to any button |

---

## 🚀 Quick Start Setup (Step-by-Step for Beginners)

### Prerequisites

1. **Unity 6.3 LTS (6000.3.x)** or newer
2. **Meta XR SDK** installed via Package Manager
3. **TextMeshPro** (usually included, import essentials if prompted)

### Step 1: Open the Project

1. Open Unity Hub
2. Click "Open" and navigate to this project folder
3. Wait for Unity to import all assets

### Step 2: Use the Setup Wizard

1. In Unity, go to **Window > Game UI > Setup Wizard**
2. Click **"Create Complete UI Setup"**
3. This automatically creates all UI components with proper hierarchy

### Step 3: Configure for Meta Quest 3

1. Go to **File > Build Settings**
2. Select **Android** platform and click **Switch Platform**
3. Go to **Edit > Project Settings > XR Plug-in Management**
4. Enable **OpenXR** under Android tab
5. Under OpenXR, add **Meta Quest Support** feature

### Step 4: Scene Setup

The wizard creates this hierarchy:
```
GameUI
└── WorldSpaceCanvas
    ├── TimerUI (top-center)
    ├── ScoreUI (top-right)
    └── EndSessionUI (center, hidden until session ends)
```

---

## 🎮 How to Use in Your Main Game

### Starting a Session

```csharp
// Get reference to the session manager
GameSessionManager sessionManager = FindObjectOfType<GameSessionManager>();

// Start the 30-second session
sessionManager.StartSession();
```

### Adding Score When User Clicks a Button

**Option A: Use the ButtonClickHandler component**
1. Add `ButtonClickHandler` component to any interactable object
2. Connect the `OnClick()` method to your XR Interactable's Select event

**Option B: Call AddScore directly**
```csharp
// When user clicks a button in your game
sessionManager.AddScore(1); // Add 1 point
sessionManager.AddScore(5); // Or add custom points
```

### Listening to Events

```csharp
void Start()
{
    sessionManager.OnSessionStart.AddListener(OnGameStart);
    sessionManager.OnSessionEnd.AddListener(OnGameEnd);
    sessionManager.OnScoreChanged.AddListener(OnScoreUpdate);
}

void OnGameStart()
{
    // Enable your game buttons
}

void OnGameEnd()
{
    // Disable your game buttons
}

void OnScoreUpdate(int newScore)
{
    // React to score changes
}
```

---

## 🎨 Color Scheme (Calming Blue)

| Color | Hex | RGB | Usage |
|-------|-----|-----|-------|
| Primary Blue | `#3380CC` | (51, 128, 204) | Progress bars, main elements |
| Light Blue | `#66B3F2` | (102, 179, 242) | Text, highlights |
| Dark Blue | `#1A4073` | (26, 64, 115) | Backgrounds, shadows |
| Panel BG | `#1F2E47` | (31, 46, 71) @ 95% | Panel backgrounds |

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   └── GameUI/
│       ├── GameSessionManager.cs   # Main controller
│       ├── TimerUI.cs              # Timer display
│       ├── ScoreUI.cs              # Score display
│       ├── EndSessionUI.cs         # End popup
│       ├── UIColorScheme.cs        # Color definitions
│       ├── WorldSpaceUIFollower.cs # Camera follow for MR
│       ├── ButtonClickHandler.cs   # Score helper
│       └── Editor/
│           └── GameUISetupWizard.cs # Setup automation
├── Prefabs/
│   └── GameUI/                     # Prefab storage
└── Scenes/
    └── SampleScene.unity           # Default scene
```

---

## 🥽 Meta Quest 3 MR Best Practices Applied

1. **World Space Canvas** - UI exists in 3D space, not stuck to face
2. **Lazy Follow** - UI smoothly follows user without being intrusive
3. **Comfortable Distance** - UI positioned 2m from user by default
4. **Passthrough Compatible** - Transparent backgrounds work with MR
5. **XR Interaction Ready** - Works with hand tracking and controllers

---

## 🔧 Customization

### Change Session Duration

```csharp
sessionManager.SetSessionDuration(60f); // 60 seconds instead of 30
sessionManager.StartSession();
```

### Modify Colors

Edit the color values in each UI script's Inspector, or create a `UIColorScheme` ScriptableObject:
1. Right-click in Project > Create > GameUI > Color Scheme
2. Assign to your UI components

### Adjust UI Position

Modify `WorldSpaceUIFollower` settings:
- **Follow Distance**: How far from camera (default: 2m)
- **Height Offset**: Vertical offset from eye level
- **Lazy Follow**: Enable for comfortable MR experience

---

## 🔗 Integration with Main Game

The person building the main game should:

1. **Keep the GameUI hierarchy** in their scene
2. **Call `StartSession()`** when the game begins
3. **Call `AddScore()`** whenever a button is clicked
4. **Listen to `OnSessionEnd`** to know when time is up
5. **Connect `OnRestartClicked`** on EndSessionUI to restart logic

---

## 📝 API Reference

### GameSessionManager

| Method | Description |
|--------|-------------|
| `StartSession()` | Begins 30-second countdown |
| `AddScore(int points)` | Adds points to current score |
| `EndSession()` | Manually ends session early |
| `ResetSession()` | Resets without starting |
| `SetSessionDuration(float)` | Changes timer duration |

| Property | Type | Description |
|----------|------|-------------|
| `IsSessionActive` | bool | True if session is running |
| `CurrentScore` | int | Current score value |
| `TimeRemaining` | float | Seconds left |

| Event | Description |
|-------|-------------|
| `OnSessionStart` | Fired when session begins |
| `OnSessionEnd` | Fired when timer reaches 0 |
| `OnScoreChanged` | Fired with new score value |

---

## ❓ Troubleshooting

**UI not visible?**
- Check Canvas is set to World Space
- Verify camera is assigned to Event Camera
- Ensure UI is in front of camera (Z position)

**Timer not counting?**
- Call `StartSession()` to begin
- Check `autoStartSession` in Inspector

**Score not updating?**
- Verify `GameSessionManager` reference is set
- Ensure `IsSessionActive` is true

**End screen not appearing?**
- Check `EndSessionUI` reference in GameSessionManager
- Verify the GameObject is in the scene

---

## 📄 License

This component is provided for use in your Meta Quest 3 MR application.

---

*Built with Unity's XR Interaction Toolkit and optimized for Meta Quest 3 Mixed Reality.*
