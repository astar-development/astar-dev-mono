// =============================================================================
// SPIKE 2 — Avalonia 11 system tray on Linux
// =============================================================================
// ASSUMPTION: Avalonia 11.2.3 supports system tray on Linux via built-in API
//
// HOW TO RUN:
//   dotnet run
//   The main window shows. Close it — the app should remain alive in the tray.
//   Right-click the tray icon to see the context menu.
//
// WHAT TO CHECK (tick each box):
//   [x] Tray icon appears after closing the main window         — verified Linux Mint (Cinnamon) 2026-03-25
//   [ ] Tray icon visible on GNOME (may require AppIndicator/libappindicator)
//   [x] Tray icon visible on KDE Plasma                        — target DE; verified Linux Mint 2026-03-25 (Cinnamon/X11); re-verify on Fedora KDE v43
//   [ ] Tray icon visible on other DEs in use (Sway, XFCE, etc.)
//   [x] Right-click shows context menu with Open and Quit items — verified Linux Mint (Cinnamon) 2026-03-25
//   [x] "Open" re-shows the main window                        — verified Linux Mint (Cinnamon) 2026-03-25
//   [x] "Quit" exits the process cleanly                       — verified Linux Mint (Cinnamon) 2026-03-25
//   [x] No crash or unhandled exception on any tested DE        — verified Linux Mint (Cinnamon) 2026-03-25
//
// KNOWN RISK:
//   GNOME by default does not show tray icons. The AppIndicator extension
//   (gnome-shell-extension-appindicator) must be installed.
//   If tray is invisible on GNOME, note this as a risk — graceful degradation
//   (keep the window open instead of hiding to tray) may be required.
// =============================================================================

using Avalonia;

AppBuilder.Configure<AStar.Dev.Spikes.AvaloniaTray.App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
