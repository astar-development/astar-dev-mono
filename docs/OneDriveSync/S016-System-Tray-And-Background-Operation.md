# S016 — System Tray & Background Operation

**Phase:** MVP  
**Area:** Infrastructure/Tray  
**Spec refs:** ST-01 to ST-05, NF-01

---

## User Story

As a user,  
I want the app to continue syncing in the background when I close the window, with a system tray icon that gives me quick access to sync controls and notifications,  
So that I can set-and-forget without keeping the window open.

---

## Acceptance Criteria

### Tray Behaviour (ST-01, ST-02)
- [ ] Closing the main window minimises to system tray — does **not** exit the process
- [ ] Tray context menu contains:
  - "Open" — restores/focuses the main window
  - One "Sync Now — [AccountName]" entry per configured account
  - "Quit" — exits the app (waits for active syncs to reach a safe checkpoint first)
- [ ] Double-clicking the tray icon opens/focuses the main window
- [ ] `TrayService` interface in `Infrastructure/Tray/`; implementation is platform-specific (KDE/Linux MVP)

### OS Notifications (ST-03)
- [ ] Notifications sent for: sync complete, conflict detected, sync error — **only when main window is not in foreground**
- [ ] Single on/off notification toggle in Settings (wired up in S015)
- [ ] Notification shows: account name, brief status message; tapping notification opens the main window

### Autostart (ST-04)
- [ ] On first launch, app writes a `.desktop` file to `~/.config/autostart/AStar.Dev.OneDriveSync.desktop`
- [ ] Autostart `.desktop` file launches the app minimised to tray
- [ ] Autostart `.desktop` file is removed if the app is uninstalled (install/uninstall script concern — document this)

### Tray Unavailable Graceful Degradation (ST-05)
- [ ] At startup, `TrayService` detects whether the system tray is available
- [ ] If unavailable: closing the window closes the app; scheduled background sync does not run; user is informed once via a toast: "System tray not available — background sync is disabled"
- [ ] Tray availability status exposed via `ITrayService.IsAvailable`

### Tests
- [ ] **Unit test**: `TrayService` — when `IsAvailable = false`, `StartMinimised()` is a no-op and returns `TrayUnavailableResult`
- [ ] **Unit test**: tray context menu items match configured accounts (mock `IAccountRepository`)
- [ ] **Unit test**: "Quit" from tray triggers graceful shutdown (cancellation token cancelled; active syncs signalled)
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `ITrayService` interface in the desktop app (not in packages — platform-specific)
- KDE Plasma tray support via Avalonia community tray package (confirmed in spec Section 14 assumption 2)
- `TrayService` registered as **singleton**; lifecycle managed by `App.axaml.cs`
- Platform-specific code isolated behind `ITrayService` — `TrayViewModel` never references `Windows.*` or `Linux.*` namespaces
- NF-00: tray availability, autostart registration, and notification sends all logged at `Information`/`Debug`

---

## Implementation Constraints

- `Application.Current.ShutdownMode` **must** be set to `ShutdownMode.OnExplicitShutdown` in `App.axaml` (or programmatically before the first window close). Without this, closing the main window exits the process regardless of any tray intercept logic.
- The `Closing` event handler in `MainWindow.axaml.cs` that intercepts window close to minimise to tray must be `async void` — this is a permitted Avalonia exception because framework event handlers do not support `Task`-returning signatures. Add the comment: `// Avalonia event handler — async void required`.
- `TrayIcon` must be created on the UI thread. Initialise it inside `OnFrameworkInitializationCompleted()` in `App.axaml.cs`; never construct it from a background thread or a singleton constructor.
- The `CancellationTokenSource` used to signal graceful shutdown when the user selects "Quit" must be disposed after cancellation; use a `using` declaration to guarantee disposal.

---

## Dependencies

- S001 (project scaffolding)
- S003 (navigation shell — window close intercepted here)
- S008 (account management — per-account "Sync Now" tray entries)
- S010 (sync engine — "Sync Now" from tray triggers sync)
- S015 (settings — notification toggle)
