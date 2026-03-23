### AStar.Dev.Clock — Avalonia Analog Clock (Light/Dark Theme)

This is a cross‑platform C# project (targeting .NET 10 / Avalonia 11) that displays an analog clock with a light and dark theme. It should run on Linux Mint, as well as other desktop OSes supported by .NET and Avalonia.

#### Prerequisites
- .NET 10 SDK (or newest installed SDK that supports Avalonia 11). On Linux Mint, you can install from Microsoft’s package feeds or via `dotnet-install.sh`.

Verify installation:
```
dotnet --info
```

#### Build and run
```
cd src/AStar.Dev.Clock
dotnet run -c Release
```

#### Features
- Smooth analog clock face (hour, minute, second hands)
- Light and Dark theme toggle in the title bar
- Resizes cleanly; vector rendering via Avalonia drawing routines
- Optional: follows system theme when you press the “Auto” button

#### Controls
- Light: forces light theme
- Dark: forces dark theme
- Auto: follows the system theme

#### Project structure
- `src/AStar.Dev.Clock/` — Avalonia desktop application
  - `App.axaml` — app resources and theme
  - `MainWindow.axaml` — main window UI with theme controls and the analog clock
  - `Controls/AnalogClockControl.cs` — custom control that renders the clock

#### Packaging
You can publish a self‑contained build if you want a single folder to distribute:
```
dotnet publish src/AStar.Dev.Clock -c Release -r linux-x64 --self-contained false
```

Replace `linux-x64` with your target runtime if needed.
