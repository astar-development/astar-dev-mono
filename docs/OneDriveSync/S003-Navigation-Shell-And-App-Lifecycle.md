# S003 — Navigation Shell & Application Lifecycle

**Phase:** MVP
**Area:** Foundation
**Spec refs:** Section 7 (Navigation Structure), AL-01, AL-02, NF-01, NF-15

---

## User Story

As a user,
I want the app to open immediately with a loading state while initialisation completes, and navigate between sections via an icon rail,
So that the app feels responsive from the first moment and I always know where I am.

---

## Acceptance Criteria

### Shell & Navigation

- [x] `Infrastructure/Shell/MainWindow.axaml` contains the icon rail (sidebar) with all 8 nav items in spec order (Dashboard, Accounts, Activity, Conflicts, Log Viewer, Settings, Help, About)
- [x] `Infrastructure/Shell/NavigationService.cs` + `INavigationService` handles view switching without code-behind logic in `MainWindowViewModel`
- [x] Each nav item activates the correct feature view (stubbed views acceptable at this stage — they will be replaced by feature stories)
- [x] `MainWindowViewModel` owns nav selection state; `MainWindow.axaml.cs` contains only Avalonia lifecycle hooks
- [x] **NF-15**: Nav items for unimplemented features are **disabled** (not hidden) until the owning feature story is complete — enabled state is controlled via a `IFeatureAvailabilityService`

### Startup Loading State (AL-02)

- [x] UI is visible immediately on launch with a loading indicator
- [x] Background initialisation tasks run concurrently on a non-UI thread: DB migration, token validation stub, sync state recovery stub
- [x] Loading indicator dismissed and nav rail enabled only when all initialisation tasks complete (or fail with a handled error)
- [x] DB migration failure surfaces a "Database corrupt — Start Fresh?" dialog (wires up EH-08 from S002)

### Single-Instance Enforcement (AL-01)

- [x] `Infrastructure/SingleInstance/SingleInstanceGuard.cs` implemented using a named `Mutex` or named pipe
- [x] Second launch attempt: shows OS-level message "AStar OneDrive Sync is already running" and exits with code 0
- [x] Single-instance guard activated before the Avalonia `AppBuilder` initialises — no partial startup on duplicate launch
- [x] **Unit test**: guard detects an existing instance and returns the correct signal

### Startup Sequence (Ordered)

1. Single-instance check
2. DI container build
3. Show `MainWindow` with loading state
4. Run background init (DB migrate, token validation, sync state recovery) — all with `CancellationToken` from app lifetime
5. Dismiss loading state

### Logging

- [x] App startup logged at `Information` with app version
- [x] Each init task start/complete/fail logged at appropriate level (NF-00)

### Tests

- [x] **Unit test**: `MainWindowViewModel` nav selection changes the correct active view
- [x] **Unit test**: `SingleInstanceGuard` — first instance acquires; second returns duplicate signal
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `async void` permitted **only** in Avalonia event handlers in `MainWindow.axaml.cs`; comment required: `// Avalonia event handler — async void required`
- All nav-to-feature bindings use `ReactiveUI` routing or explicit `INavigationService` — no `if/switch` in code-behind
- `IFeatureAvailabilityService` is a simple dictionary-backed singleton; features register themselves as available during DI setup
- Disabled nav items must still show tooltips explaining they are "Coming soon" or similar (NF-15)

---

## Dependencies

- S001 (project scaffolding)
- S002 (database foundation — needed for startup init flow)
