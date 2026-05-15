# Numpad Overlay

A lightweight, always-on-top Windows numpad overlay application built with C# and WPF (.NET 8). Displays a virtual numpad on your screen that responds to keyboard input in real-time.

## Features

- **Always-on-top overlay**: Transparent numpad window stays visible on top of other applications
- **Real-time key feedback**: Visual highlight when numpad keys are pressed
- **Global keyboard hook**: Captures and displays all numpad input system-wide
- **NumLock status indicator**: Shows current NumLock state
- **Customizable settings**:
  - Adjust overlay opacity (10% - 100%)
  - Choose between Small, Medium, and Large sizes
  - Configure toggle hotkey (Ctrl + key combination)
- **System tray integration**: Minimize/restore from system tray, quick access menu
- **Persistent settings**: Settings saved to `settings.json` automatically
- **Self-contained executable**: Single-file deployment, no dependencies required

## System Requirements

- **OS**: Windows 10/11 (64-bit)
- **.NET Runtime**: .NET 8.0+ (included in self-contained build)
- **Architecture**: x64 (win-x64)

## Installation

### Option 1: Download Release Build
1. Go to [Releases](../../releases)
2. Download the latest `NumpadOverlay.exe`
3. Run the executable

### Option 2: Build from Source
```bash
# Clone repository
git clone https://github.com/longhangoc/numpadcsharp.git
cd numpadcsharp

# Build self-contained executable
dotnet publish NumpadOverlay.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish

# Run
./publish/NumpadOverlay.exe
```

## Usage

1. **Launch** the application
2. **Numpad overlay** appears on your screen
3. **Press keys** on your numpad to see visual feedback
4. **Ctrl + ` (backtick)** to toggle overlay visibility (default hotkey)
5. **Right-click tray icon** for quick menu:
   - Hide/Show Overlay
   - Open Settings
   - Exit

## Settings

Access settings by clicking the ⚙ button on the overlay or selecting "Open Settings" from tray menu.

- **Opacity**: Slide to adjust overlay transparency
- **Size**: Choose Small (75%), Medium (100%), or Large (150%)
- **Toggle Hotkey**: Select Ctrl + (F1-F12 or backtick)

All changes are saved automatically when the settings window closes.

## Architecture

### Core Components

- **MainWindow.xaml/xaml.cs**: Main overlay UI and keyboard hook implementation
- **SettingsWindow.xaml/xaml.cs**: Settings UI and controls
- **Models/AppSettings.cs**: Settings model with JSON serialization
- **App.xaml/xaml.cs**: WPF application entry point

### Key Technologies

- **WPF** for UI rendering
- **Windows Forms** for system tray integration
- **Low-level keyboard hook** (WH_KEYBOARD_LL) for global input capture
- **System.Text.Json** for persistent settings

### Threading Model

- Keyboard hook runs on dedicated background thread with STA apartment state
- UI updates dispatched to main dispatcher to avoid cross-thread issues
- Settings changes propagate via `INotifyPropertyChanged`

## Keyboard Hook Details

The application uses `SetWindowsHookEx` with `WH_KEYBOARD_LL` to intercept all keyboard input globally. This allows:
- Detection of numpad key presses regardless of active window
- Non-intrusive monitoring (does not consume the keystrokes)
- Real-time visual feedback

## Building

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 (optional, for IDE development)

### Debug Build

```bash
dotnet build NumpadOverlay.csproj --configuration Debug
```

### Release Build

```bash
dotnet build NumpadOverlay.csproj --configuration Release
```

### Publish (Self-Contained)

```bash
dotnet publish NumpadOverlay.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o ./publish
```

## Settings File

Settings are stored in `settings.json` located in the application directory:

```json
{
  "opacity": 0.85,
  "overlaySize": "medium",
  "toggleHotkeyVirtualKey": 192
}
```

- `opacity`: 0.1 - 1.0 (10% - 100%)
- `overlaySize`: "small", "medium", or "large"
- `toggleHotkeyVirtualKey`: Virtual key code (0xC0 = backtick, 0x70-0x7B = F1-F12)

## Known Limitations

- Windows-only (requires WinAPI and WPF)
- Requires elevation/admin privileges for global keyboard hook on some systems
- SVG icon loading disabled (uses system application icon instead)

## Troubleshooting

### Overlay doesn't appear
- Check if the application is running in system tray
- Try clicking the tray icon or pressing Ctrl + `

### Keyboard input not captured
- Ensure the application is running with sufficient permissions
- Check that NumLock key is functioning

### Settings not saved
- Verify write permissions in application directory
- Check for `settings.json` file in the app folder

## License

MIT License - See LICENSE file for details

## Contributing

Contributions welcome! Please feel free to submit issues and pull requests.

---

**Built with**: C#, WPF, .NET 8.0
