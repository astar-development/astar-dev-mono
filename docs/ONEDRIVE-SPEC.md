# AStar.Dev OneDrive Sync — Application Specification

**Version:** 2.0 | **Date:** 2026-03-27 | **Status:** Approved for development

---

## 1. Executive Summary

A cross-platform AvaloniaUI desktop application to synchronise personal Microsoft OneDrive accounts to the local file system. Supports multiple accounts, app-wide theming with runtime switching, and localisation infrastructure from day one.

This is a **fresh build** — the previous implementation (`AStar.Dev.OneDriveSync.old/`) does not meet repo standards. The UI patterns from the old app are preserved for user familiarity, but the codebase is entirely new. All projects — including the existing `AStar.Dev.Sync.Engine`, `AStar.Dev.Conflict.Resolution`, and `AStar.Dev.OneDrive.Client` packages — will be rewritten to follow **feature-slice architecture** (grouped by business benefit, not by technical type).

### Key Architectural Principle

> Every folder answers "what business problem does this solve?" — not "what kind of file is this?"
>
> **Example:** `Features/ChangeDetection/`, `Features/FileTransfer/`, `Features/AccountManagement/` — not `Services/`, `Models/`, `ViewModels/`.

---

## 2. Goals & Success Metrics

| Goal                         | Metric                                               | Target             |
| ---------------------------- | ---------------------------------------------------- | ------------------ |
| Reliable bi-directional sync | Sync runs completing without data loss               | 100%               |
| Multi-account support        | Concurrent accounts without degradation              | ≥ 5                |
| Non-technical UX             | User adds account and starts first sync without docs | < 3 minutes        |
| Background operation         | Scheduled syncs fire without manual intervention     | ✓                  |
| Conflict transparency        | No silent overwrites                                 | 100% user-resolved |
| Immediate theme switching    | Theme change applies without restart                 | ✓                  |
| Localisation readiness       | All user-facing strings externalised from day one    | 100%               |

---

## 3. Non-Goals (Out of Scope)

- Entra ID / work/school accounts
- Shared OneDrive folders (files shared with the account by others)
- File-level sync selection (folders only)
- Web-based interface
- Automatic conflict resolution (all conflicts are user-resolved)
- Mobile platforms
- Windows/macOS packaging in MVP
- Real-time file system watching (`inotify` / `FileSystemWatcher`)
- Telemetry, crash reporting, or analytics (app is local-only apart from OneDrive sync and update checks)
- Self-updating AppImage (MVP uses manual download)

---

## 4. User Personas

**Alex (Power User)** — Developer with 3–5 personal OneDrive accounts. Wants automated sync, per-account tuning, full log access, and quick diagnostics. Comfortable with advanced settings.

**Sam (Casual User)** — Non-technical. One account. Wants set-and-forget. Must not encounter technical jargon or settings that could cause damage if misconfigured.

---

## 5. App-Level User Type

A global app setting determines the UI complexity surface: **Casual** or **Power User**.

| Aspect                    | Casual                          | Power User                  |
| ------------------------- | ------------------------------- | --------------------------- |
| Concurrency settings      | Hidden (default of 5 applies)   | Editable (1–10)             |
| Debug logging toggle      | Hidden                          | Visible per account         |
| Log Viewer detail         | Friendly errors/warnings only   | Full verbose Serilog output |
| Sync interval             | Hidden (default 60 min applies) | Editable from fixed list    |
| Advanced account settings | Hidden                          | Visible                     |

### Setting the User Type

- Set during the **welcome/onboarding flow** before the first add-account wizard
- Editable any time in **Settings**
- Switching to **Power User** requires confirmation ("This unlocks advanced settings that can affect sync performance if misconfigured")
- Switching to **Casual** applies immediately with no confirmation

---

## 6. Functional Requirements

### 6.1 Onboarding & Help

| ID    | Requirement                                                                                                                | Phase |
| ----- | -------------------------------------------------------------------------------------------------------------------------- | ----- |
| OH-01 | First launch shows a welcome/onboarding screen explaining what the app does, with a prominent "Add your first account" CTA | MVP   |
| OH-02 | Onboarding includes a link to an external markdown help file (maintained separately from the UI)                           | MVP   |
| OH-03 | Onboarding includes a "Skip" option for power users who want to proceed directly                                           | MVP   |
| OH-04 | User type selection (Casual / Power User) is presented during the onboarding flow, before the first add-account wizard     | MVP   |
| OH-05 | A Help icon on the nav rail allows the user to replay the onboarding content at any time                                   | MVP   |
| OH-06 | Help content is identical to the onboarding for MVP                                                                        | MVP   |

### 6.2 Account Management

| ID    | Requirement                                                                                                                                     | Phase |
| ----- | ----------------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| AM-01 | Add a personal Microsoft account via MSAL OAuth (system browser)                                                                                | MVP   |
| AM-02 | 3-step add-account wizard: (1) authenticate, (2) optionally select folders, (3) confirm                                                         | MVP   |
| AM-03 | Wizard step 2 shows root OneDrive folders; UI allows expanding to arbitrary depth for subfolder selection                                       | MVP   |
| AM-04 | Folder selection editable from the Accounts view after wizard                                                                                   | MVP   |
| AM-05 | Per-account settings (Power User only): sync interval, concurrency (1–10, default 5), local sync path, debug logging toggle                     | MVP   |
| AM-06 | Each account must have a unique, non-overlapping local sync folder; the UI must prevent overlapping folder selection                            | MVP   |
| AM-07 | Platform-specific default local sync folder set on add (e.g., `~/OneDrive/<account>/`); editable                                                | MVP   |
| AM-08 | Remove account prompts user: keep or delete local synced folder                                                                                 | MVP   |
| AM-09 | Account removal is **blocked** while a sync is active for that account. User must cancel the sync first                                         | MVP   |
| AM-10 | Local sync folder validation: if the selected folder is not empty, warn the user that conflicts may occur on first sync; allow them to continue | MVP   |
| AM-11 | If the local sync folder is on an unmounted/unavailable drive when a sync triggers, the sync fails with a clear message and log entry           | MVP   |

### 6.3 Sync Engine

| ID    | Requirement                                                                                                                                                                                                                    | Phase    |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- |
| SE-01 | Bi-directional sync                                                                                                                                                                                                            | MVP      |
| SE-02 | Default 5 concurrent up/downloads per account; configurable 1–10 by Power Users. Hard maximum of 10 regardless of user type                                                                                                    | MVP      |
| SE-03 | Manual "Sync Now" per account from the Accounts view                                                                                                                                                                           | MVP      |
| SE-04 | Automatic sync on configurable timer; default 60 minutes. Intervals: 5, 15, 30, 60 minutes (Power User only)                                                                                                                   | MVP      |
| SE-05 | When multiple accounts are configured, if any account is already syncing and a second account's sync is triggered, the user is prompted to confirm ("syncing multiple accounts simultaneously may impact performance")         | MVP      |
| SE-06 | Only one sync may run per account at a time. If a sync for an account is already in progress, a new sync for that account is rejected                                                                                          | MVP      |
| SE-07 | Folder-level selection only; all files in selected folders (and descendants) are synced                                                                                                                                        | MVP      |
| SE-08 | First sync downloads remote content; conflict resolution applies if local files already exist                                                                                                                                  | MVP      |
| SE-09 | Delta sync via Microsoft Graph delta queries for incremental sync (changed files only)                                                                                                                                         | MVP      |
| SE-10 | When a delta token expires (>30 days inactivity), the app informs the user that a full re-sync is required and offers the choice to proceed now or postpone to a manual trigger                                                | MVP      |
| SE-11 | During full re-sync, files with matching timestamps/checksums are skipped — no unnecessary bandwidth consumption                                                                                                               | MVP      |
| SE-12 | Remote folder renames detected via delta query are reflected locally — the app updates the local folder to match, not treated as a delete+create conflict                                                                      | MVP      |
| SE-13 | Sync progress displayed as account-level progress bar: "Syncing account X — 45% — ETA 3 min"                                                                                                                                   | MVP      |
| SE-14 | ETA updates in real-time during sync (including when throttled by Graph API) without impacting UI responsiveness                                                                                                               | MVP      |
| SE-15 | Symlinks, hardlinks, `.git` directories, socket files, and other non-regular files are **skipped** during sync. Each skip is logged, and the user is informed post-sync that "some files were skipped — please review the log" | MVP      |
| SE-16 | Resumable downloads (range requests for interrupted large files)                                                                                                                                                               | Post-MVP |

### 6.4 Conflict Resolution

| ID    | Requirement                                                                                                                                               | Phase |
| ----- | --------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| CR-01 | Conflict triggered by: same file modified both sides, or file deleted one side / present the other                                                        | MVP   |
| CR-02 | User MUST manually resolve every conflict — no silent overwrites or deletions                                                                             | MVP   |
| CR-03 | Resolution strategies per conflict: **Local Wins**, **Remote Wins**, **Keep Both**, **Skip**                                                              | MVP   |
| CR-04 | "Keep Both" renames the conflicting copy as `original-name-(yyyy-MM-ddTHHmmssZ).ext` where the datetime is UTC                                            | MVP   |
| CR-05 | "Skip" defers the conflict; the queue is persistent across sessions (stored in SQLite)                                                                    | MVP   |
| CR-06 | The conflict list presents each conflict as a checkable item. The user can select any subset and apply a single resolution strategy to all selected items | MVP   |
| CR-07 | A "Select All" button selects all conflicts in the current list                                                                                           | MVP   |
| CR-08 | Once a resolution strategy is applied to a conflict, it cascades to all matching pending conflicts across sessions (same file path)                       | MVP   |

### 6.5 Authentication & Token Management

| ID    | Requirement                                                                                                                                                                                                                     | Phase |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| AU-01 | MSAL for personal Microsoft accounts only (`consumers` tenant)                                                                                                                                                                  | MVP   |
| AU-02 | Tokens persisted locally; no re-auth on every launch                                                                                                                                                                            | MVP   |
| AU-03 | On Linux, if the OS keychain is unavailable, the app presents an explicit opt-in consent dialog before falling back to an insecure local token store (machine-scoped encryption key, not plaintext). Consent stored per account | MVP   |
| AU-04 | Token refresh is silent; user prompted only if automation fails                                                                                                                                                                 | MVP   |
| AU-05 | When token refresh fails (user changed password, revoked access), all syncs for that account are paused and a persistent banner/badge is shown until the user re-authenticates                                                  | MVP   |

### 6.6 Error Handling & Resilience

| ID    | Requirement                                                                                                                                                                                | Phase    |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- |
| EH-01 | Network loss during sync: retry with exponential backoff. If backoff exhausts, mark sync as interrupted and wait for next scheduled/manual trigger                                         | MVP      |
| EH-02 | Graph API 429 (throttled): silently respect `Retry-After` header and continue sync. ETA updates to reflect the delay                                                                       | MVP      |
| EH-03 | Disk space exhaustion: check available space before starting a sync and warn if likely insufficient. If a write fails mid-sync due to disk full, the sync fails with a clear error message | MVP      |
| EH-04 | Interrupted sync (crash, network drop) detected on next launch or scheduled run                                                                                                            | MVP      |
| EH-05 | Resume from interruption point where possible                                                                                                                                              | MVP      |
| EH-06 | If resume impossible, user informed with clear explanation                                                                                                                                 | MVP      |
| EH-07 | SQLite database backed up (copied) before each sync run. No periodic backups — only on mutation                                                                                            | MVP      |
| EH-08 | If DB is corrupt on startup, user informed with option to start fresh (re-add accounts)                                                                                                    | MVP      |
| EH-09 | Configurable "minimum free space" threshold that prevents sync from starting                                                                                                               | Post-MVP |

### 6.7 System Tray & Background Operation

| ID    | Requirement                                                                                                                                         | Phase    |
| ----- | --------------------------------------------------------------------------------------------------------------------------------------------------- | -------- |
| ST-01 | Closing main window keeps app running as system tray process                                                                                        | MVP      |
| ST-02 | Tray context menu: Open, per-account Sync Now entries, Quit                                                                                         | MVP      |
| ST-03 | OS-level notifications when app is not in foreground: sync complete, conflict detected, error. Single on/off toggle in Settings                     | MVP      |
| ST-04 | App launches at OS startup via `.desktop` file in `~/.config/autostart/`                                                                            | MVP      |
| ST-05 | If system tray is unavailable, the app stays in the taskbar normally — closing the window closes the app. Background sync does not run in this mode | MVP      |
| ST-06 | Configurable notification granularity per account                                                                                                   | Post-MVP |

### 6.8 Application Lifecycle

| ID    | Requirement                                                                                                                                            | Phase    |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- |
| AL-01 | Single-instance enforcement. If the app is already running, a second launch shows an "already running" message and exits                               | MVP      |
| AL-02 | UI appears immediately on startup with a loading state while background initialisation (token validation, DB migration, sync state recovery) completes | MVP      |
| AL-03 | Bring existing instance to foreground on second launch attempt                                                                                         | Post-MVP |

### 6.9 Application Updates

| ID    | Requirement                                                                                                                                                              | Phase    |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- |
| UP-01 | App checks GitHub Releases API for available updates **at startup**                                                                                                      | MVP      |
| UP-02 | Update notification shows release notes summary and a "Download" button that opens the GitHub Releases page in the system browser                                        | MVP      |
| UP-03 | Deferred update re-prompts every hour                                                                                                                                    | MVP      |
| UP-04 | After 7 days of deferral, update is forced — app displays a "Please update to continue" screen with a download link; app functionality is blocked until the user updates | MVP      |
| UP-05 | Any release published within 12 months of the current date is accepted as a valid latest version                                                                         | MVP      |
| UP-06 | Forced update (UP-04) waits for any active sync to complete before blocking the UI                                                                                       | MVP      |
| UP-07 | Scheduled update checks (not just at startup)                                                                                                                            | Post-MVP |
| UP-08 | Self-updating AppImage (download, replace, restart)                                                                                                                      | Post-MVP |

### 6.10 Theming

| ID    | Requirement                                                                                                             | Phase    |
| ----- | ----------------------------------------------------------------------------------------------------------------------- | -------- |
| TH-01 | Three theme modes: **Light**, **Dark**, **Auto** (follows OS)                                                           | MVP      |
| TH-02 | Theme is a **global app setting** stored in the SQLite database                                                         | MVP      |
| TH-03 | Theme switching applies **immediately at runtime** — no restart required. Resource dictionaries are swapped dynamically | MVP      |
| TH-04 | Every UI element themeable via resource dictionaries — no hardcoded colours                                             | MVP      |
| TH-05 | "Auto" mode reacts to OS theme changes in real-time (manual or scheduled)                                               | MVP      |
| TH-06 | Architecture supports adding new themes with minimal rework (extensible resource dictionary pattern)                    | MVP      |
| TH-07 | Accessible colour schemes (high contrast, colour-blind-friendly)                                                        | Post-MVP |
| TH-08 | Additional custom themes beyond Light/Dark/Auto                                                                         | Post-MVP |
| TH-09 | Custom accent colour selection                                                                                          | Post-MVP |

### 6.11 Localisation

| ID    | Requirement                                                                                                                                                                                        | Phase    |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- |
| LO-01 | All user-facing strings externalised using `.resx` resource files — i18n infrastructure from day one                                                                                               | MVP      |
| LO-02 | MVP locale: UK English (`en-GB`)                                                                                                                                                                   | MVP      |
| LO-03 | Locale selectable in the UI (Settings) and applies globally                                                                                                                                        | MVP      |
| LO-04 | On first launch, the app follows the OS locale if supported; falls back to `en-GB` if not                                                                                                          | MVP      |
| LO-05 | Localisation scope: all UI labels, tooltips, error messages, notification toasts, log messages (where practicable)                                                                                 | MVP      |
| LO-06 | Conflict rename suffixes use UTC-formatted strings for consistency across locales                                                                                                                  | MVP      |
| LO-07 | "Last synced" display: relative time for recent (< 1 hour, e.g., "5 minutes ago"), absolute time for older ("Today at 14:32" / "25 Mar at 09:15"). Date/time formatting respects the active locale | MVP      |
| LO-08 | Additional locale translations                                                                                                                                                                     | Post-MVP |

### 6.12 Logging & Diagnostics

| ID    | Requirement                                                                                                                   | Phase    |
| ----- | ----------------------------------------------------------------------------------------------------------------------------- | -------- |
| LG-01 | Debug logging per account: off = errors/warnings only; on = verbose (Power User only)                                         | MVP      |
| LG-02 | Logs MUST NEVER contain identity PII (name, email, Microsoft account ID). File paths and file names are **not** masked in MVP | MVP      |
| LG-03 | Log Viewer accessible from the nav rail (no longer hidden)                                                                    | MVP      |
| LG-04 | Log Viewer shows account filter — select which account's logs to view                                                         | MVP      |
| LG-05 | Log Viewer detail level adapts to app user type: Casual sees friendly errors/warnings; Power User sees full output            | MVP      |
| LG-06 | Log Viewer in Casual mode shows a tooltip: "This view is simplified. Change to Power User in Settings for full log access"    | MVP      |
| LG-07 | Log files written to platform-appropriate paths (e.g., `~/.local/share/AStar.Dev.OneDriveSync/logs/`)                         | MVP      |
| LG-08 | Expand PII masking to include file paths/names                                                                                | Post-MVP |

---

## 7. Navigation Structure

The app uses an icon rail (sidebar) with one top-level item per feature:

| Order | Icon | Label      | Description                                                                                             |
| ----- | ---- | ---------- | ------------------------------------------------------------------------------------------------------- |
| 1     | 🏠   | Dashboard  | Cross-account sync status overview, per-account progress bars, "last synced" timestamps                 |
| 2     | 👤   | Accounts   | Account list, add/remove accounts, per-account settings, folder selection (Files is a sub-section here) |
| 3     | ⚡   | Activity   | Real-time feed of the last 50 activity items, newest first. Quick overview of recent sync actions       |
| 4     | ⚠️   | Conflicts  | Cross-account pending conflict queue with badge count, resolution UI with bulk selection                |
| 5     | 📋   | Log Viewer | Full log history, account filter, detail adapts to user type                                            |
| 6     | ⚙️   | Settings   | App-level: theme, locale, user type, notification toggle                                                |
| 7     | ❓   | Help       | Replay onboarding content, link to external markdown docs                                               |
| 8     | ℹ️   | About      | App version (from assembly), update status, links                                                       |

### About Screen

- Displays the app version from `Assembly.GetEntryAssembly().GetInformationalVersionAttribute()`
- In local development: displays `0.0.1-local (Development Build)`
- In release: displays the version tag (e.g., `1.2.3`) which matches the GitHub Release tag exactly
- Shows current update status (up to date / update available / update required)

---

## 8. Data Architecture

### Single SQLite Database

All data is stored in a single SQLite database managed by EF Core with migrations.

**Key design decisions:**

| Decision               | Detail                                                                                                                                 |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| Database location      | Platform-appropriate app data path (e.g., `~/.local/share/AStar.Dev.OneDriveSync/data.db`)                                             |
| Schema management      | EF Core migrations; `Database.MigrateAsync()` called at startup                                                                        |
| Entity configuration   | `IEntityTypeConfiguration<T>` per entity in `Configurations/` folders; `ApplyConfigurationsFromAssembly` in `OnModelCreating`          |
| Design-time factory    | `IDesignTimeDbContextFactory<T>` provided for `dotnet ef` tooling                                                                      |
| DateTimeOffset storage | All `DateTimeOffset` columns stored as Unix milliseconds (`long`) via EF Core value converters — enables server-side sorting/filtering |
| `EnsureCreatedAsync`   | **Not used** — bypasses migration history                                                                                              |

### Account Identifier Strategy

- Each account has a **synthetic internal ID** (e.g., GUID or auto-increment) used as the foreign key across all tables
- The **Accounts table** is the only table that stores real identity data (display name, email, Microsoft account ID)
- All other tables reference accounts via the synthetic ID only — no PII leakage

### Required Test Coverage for Data Integrity

| Test Type        | Requirement                                                                                                                                   |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Integration      | Account deletion removes **all** rows referencing that account across every table. No orphaned data survives                                  |
| Integration      | Inserting data into any non-Account table with a real email, name, or Microsoft ID instead of the synthetic ID **fails** (schema enforcement) |
| Unit/Integration | No PII (name, email, Microsoft account ID) exists in any table other than the Accounts table                                                  |
| Integration      | The synthetic account ID used in foreign keys is independent of any Microsoft account detail                                                  |

---

## 9. Project Architecture

### Feature-Slice Structure

All projects use feature-slice architecture. Files are grouped by the business capability they serve, not by their technical type.

#### Desktop App — `apps/desktop/AStar.Dev.OneDriveSync/`

```
Features/
  Dashboard/
    DashboardView.axaml
    DashboardView.axaml.cs
    DashboardViewModel.cs
    DashboardAccountViewModel.cs
  AccountManagement/
    AccountsView.axaml
    AccountsView.axaml.cs
    AccountsViewModel.cs
    AddAccountWizardView.axaml
    AddAccountWizardViewModel.cs
    FolderSelectionView.axaml
    ...
  Activity/
    ActivityView.axaml
    ActivityViewModel.cs
    ...
  ConflictResolution/
    ConflictsView.axaml
    ConflictsViewModel.cs
    ...
  LogViewer/
    LogViewerView.axaml
    LogViewerViewModel.cs
    ...
  Settings/
    SettingsView.axaml
    SettingsViewModel.cs
    ...
  Help/
    HelpView.axaml
    ...
  About/
    AboutView.axaml
    ...
  Onboarding/
    OnboardingView.axaml
    OnboardingViewModel.cs
    ...
Infrastructure/
  Theming/
    ThemeService.cs
    IThemeService.cs
    ...
  Localisation/
    LocalisationService.cs
    ILocalisationService.cs
    ...
  Shell/
    MainWindow.axaml
    MainWindowViewModel.cs
    NavigationService.cs
    ...
  Tray/
    TrayService.cs
    ...
  Persistence/
    AppDbContext.cs
    Configurations/
    Migrations/
    ...
  Updates/
    UpdateCheckService.cs
    ...
  SingleInstance/
    SingleInstanceGuard.cs
    ...
```

#### Sync Engine Package — `packages/core/astar-dev-sync-engine/AStar.Dev.Sync.Engine/`

```
Features/
  ChangeDetection/
    IDeltaTracker.cs
    DeltaTracker.cs
    ...
  FileTransfer/
    IFileTransferService.cs
    FileTransferService.cs
    ...
  StateTracking/
    ISyncStateStore.cs
    SyncState.cs
    ...
  Scheduling/
    ISyncScheduler.cs
    SyncScheduler.cs
    ...
  Orchestration/
    ISyncEngine.cs
    SyncEngine.cs
    SyncGate.cs
    SyncReport.cs
    ...
```

#### Conflict Resolution Package — `packages/core/astar-dev-conflict-resolution/AStar.Dev.Conflict.Resolution/`

```
Features/
  Detection/
    IConflictDetector.cs
    ConflictDetector.cs
    ConflictType.cs
    ...
  Resolution/
    IConflictResolver.cs
    ConflictResolver.cs
    ConflictPolicy.cs
    ...
  Persistence/
    IConflictStore.cs
    ConflictRecord.cs
    ...
  Cascading/
    ICascadeService.cs
    CascadeService.cs
    ...
```

#### OneDrive Client Package — `packages/infra/astar-dev-onedrive-client/AStar.Dev.OneDrive.Client/`

```
Features/
  Authentication/
    IMsalClient.cs
    MsalClient.cs
    ITokenManager.cs
    TokenManager.cs
    IConsentStore.cs
    ...
  FolderBrowsing/
    IOneDriveFolderService.cs
    OneDriveFolder.cs
    ...
  FileOperations/
    IFileDownloader.cs
    IFileUploader.cs
    ...
  DeltaQueries/
    IDeltaQueryService.cs
    DeltaToken.cs
    ...
```

### Repo Layout Summary

| Component                        | Location                                                                                |
| -------------------------------- | --------------------------------------------------------------------------------------- |
| Desktop app                      | `apps/desktop/AStar.Dev.OneDriveSync/`                                                  |
| Sync engine (reusable)           | `packages/core/astar-dev-sync-engine/AStar.Dev.Sync.Engine/`                            |
| Conflict resolution (reusable)   | `packages/core/astar-dev-conflict-resolution/AStar.Dev.Conflict.Resolution/`            |
| OneDrive/Graph client (reusable) | `packages/infra/astar-dev-onedrive-client/AStar.Dev.OneDrive.Client/`                   |
| Sync engine tests                | `packages/core/astar-dev-sync-engine/AStar.Dev.Sync.Engine.Tests.Unit/`                 |
| Conflict resolution tests        | `packages/core/astar-dev-conflict-resolution/AStar.Dev.Conflict.Resolution.Tests.Unit/` |
| OneDrive client tests            | `packages/infra/astar-dev-onedrive-client/AStar.Dev.OneDrive.Client.Tests.Unit/`        |

### Technology Stack

| Concern                 | Technology                                                                                           |
| ----------------------- | ---------------------------------------------------------------------------------------------------- |
| Framework               | `net10.0`                                                                                            |
| UI framework            | Avalonia 11.3.12                                                                                     |
| MVVM                    | ReactiveUI (used consistently — no mixing with other patterns)                                       |
| Database                | SQLite via EF Core with migrations                                                                   |
| Authentication          | MSAL (`Microsoft.Identity.Client`) — `consumers` tenant only                                         |
| Graph API               | Microsoft Graph SDK (or Kiota)                                                                       |
| Logging                 | Serilog                                                                                              |
| DI                      | Microsoft.Extensions.DependencyInjection                                                             |
| File system abstraction | `System.IO.Abstractions` from the [Testably](https://github.com/Testably) packages (for testability) |
| Build                   | Central Package Management (CPM) — all versions in `Directory.Packages.props`                        |
| Packaging               | AppImage (Linux MVP)                                                                                 |

---

## 10. Non-Functional Requirements

| ID    | Requirement                                                                                                                                                                  |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NF-00 | Logging is NOT optional. All stories / features MUST include suitable logging.                                                                                               |
| NF-01 | Sync I/O runs on background threads; UI never freezes                                                                                                                        |
| NF-02 | Main UI input latency < 100 ms during active sync                                                                                                                            |
| NF-03 | Memory stable during long-running syncs (no unbounded growth)                                                                                                                |
| NF-04 | Zero tolerance for silent data loss                                                                                                                                          |
| NF-05 | Persistent conflict queue survives crashes without corruption (atomic writes)                                                                                                |
| NF-06 | Tokens encrypted at rest; insecure fallback uses machine-scoped key, never plaintext                                                                                         |
| NF-07 | No identity PII in logs (file paths/names permitted in MVP)                                                                                                                  |
| NF-08 | All Graph API communication over HTTPS                                                                                                                                       |
| NF-09 | TDD conventions; all reusable packages have `.Tests.Unit` projects                                                                                                           |
| NF-10 | Sync engine, conflict logic, Graph client testable in isolation (DI, interfaces, `System.IO.Abstractions`)                                                                   |
| NF-11 | Core keyboard navigation for main nav and common actions (mouse acceptable for complex interactions like folder tree)                                                        |
| NF-12 | All controls expose automation properties for screen readers (Post-MVP — see section 12)                                                                                     |
| NF-13 | Feature-slice architecture in all projects — no "group by type" folders                                                                                                      |
| NF-14 | Use of DevAM.LemonShark (or similar) to mock OneDrive / EntraID responses where practicable                                                                                  |
| NF-15 | The app UI must NOT allow the user to select a feature that has not been implemented. It should be disabled or hidden until implemented.                                     |
| NF-16 | Functional Paradigms MUST be used wherever practicable. Use AStar.Dev.Functional.Extensions: Result<T>, Option<T>, Match<Async> etc. If there is no suitable method, add it. |

---

## 11. Target Platform

### MVP: Fedora KDE Plasma 43+ on x64

- **Primary test environment:** Fedora KDE Plasma V43+ on x64
- **Not blocked on other platforms** — the app should install and run on other Linux distros, but if it doesn't work, that is acceptable for MVP
- System tray support is reliable on KDE Plasma, which is the primary target
- `.desktop` file autostart via `~/.config/autostart/`

### Build Targets

The existing `apps/desktop/Directory.Build.props` configures all 6 RIDs (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`) with self-contained, trimmed, single-file output. MVP focuses on `linux-x64` only; others are not tested.

---

## 12. Risks

| Risk                                           | Impact                        | Likelihood       | Mitigation                                                           |
| ---------------------------------------------- | ----------------------------- | ---------------- | -------------------------------------------------------------------- |
| MSAL/Graph SDK not trim-compatible             | Release build failures        | Medium           | Test trimmed builds early; add trimmer root descriptors as needed    |
| Graph API rate limiting on large initial syncs | Throttled first sync; poor UX | Medium           | Exponential backoff, respect `Retry-After`, ETA absorbs delays       |
| MSAL token cache unreliable on Linux           | Frequent re-auth prompts      | Medium           | AU-03 fallback with opt-in consent; test both paths early            |
| Delta token expiry after 30 days inactivity    | Unexpected full re-sync       | Medium           | SE-10 informs user; SE-11 skips unchanged files                      |
| Remote folder renames cause phantom conflicts  | Confusing sync behaviour      | Medium           | SE-12 detects renames via delta query                                |
| SQLite corruption on power loss during sync    | App non-functional            | Low              | EH-07 pre-sync backup; EH-08 recovery path                           |
| Forced update blocks user mid-work             | Frustration                   | Low              | UP-06 waits for sync; UP-04 gives 7-day grace period                 |
| Linux system tray unavailable on non-KDE DEs   | Background sync doesn't work  | Low (KDE target) | ST-05 graceful degradation to taskbar mode                           |
| Large file downloads interrupted by network    | Wasted bandwidth on retry     | Medium           | MVP restarts from scratch; SE-16 (Post-MVP) adds resumable downloads |

---

## 13. Resolved Decisions

All decisions are incorporated into the requirements above. This table is retained for traceability.

| ID    | Decision                                     | Resolution                                                                                      |
| ----- | -------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| OD-01 | "Keep Both" datetime format                  | UTC `yyyy-MM-ddTHHmmssZ` — see CR-04                                                            |
| OD-02 | Conflict resolution selection model          | Checkbox per conflict; subset or Select All; strategy applies to all checked — see CR-06, CR-07 |
| OD-03 | Quota exceeded during upload                 | Error surfaced to user; manual resolution; no automatic retry                                   |
| OD-04 | Include shared OneDrive folders?             | No — excluded from scope                                                                        |
| OD-05 | Update distribution mechanism                | GitHub Releases with manual download — see UP-01, UP-02                                         |
| OD-06 | Maximum file size                            | No app-level limit; follow OneDrive API limits (250 GB chunked)                                 |
| OD-07 | Insecure token fallback — opt-in or warning? | Opt-in consent dialog; decision stored per account — see AU-03                                  |
| OD-08 | Linux packaging format for MVP               | AppImage — self-contained single-file aligns with existing build config                         |
| OD-09 | "Sync Now" scope                             | Per account; concurrent sync on same account rejected — see SE-03, SE-06                        |
| OD-10 | Database architecture                        | Single SQLite DB with synthetic account ID; PII contained to Accounts table only                |
| OD-11 | File system watching                         | Not in MVP — changes detected at sync time only (KISS; 300–500k files would exhaust inotify)    |
| OD-12 | Network failure during sync                  | Exponential backoff then mark interrupted — see EH-01                                           |
| OD-13 | App user type scope                          | App-level (not per-account); set during onboarding; editable in Settings                        |
| OD-14 | Multiple app instances                       | Blocked — show message and exit (MVP); bring to foreground (Post-MVP)                           |
| OD-15 | Downgrade path                               | Unsupported in MVP — re-add accounts if downgrade needed                                        |
| OD-16 | "Last synced" display format                 | Hybrid — relative for < 1 hour, absolute for older                                              |
| OD-17 | Default concurrency                          | 5 (changed from 8 in previous spec)                                                             |
| OD-18 | Special files (symlinks, .git, etc.)         | Skipped, logged, user informed post-sync                                                        |
| DB-01 | DateTime storage type in SQLite              | `DateTimeOffset` as Unix milliseconds (`long`) via EF Core value converters                     |
| DB-02 | EF Core entity configuration pattern         | `IEntityTypeConfiguration<T>` per entity; `ApplyConfigurationsFromAssembly`                     |
| DB-03 | Schema initialisation at startup             | `Database.MigrateAsync()` at startup; `IDesignTimeDbContextFactory<T>` for tooling              |

---

## 14. Assumptions to Validate (Technical Spikes)

1. Microsoft Graph delta query is sufficient for incremental sync and detects remote folder renames - CONFIRED
2. Avalonia 11.3.12 supports system tray on KDE Plasma 43+ via a community package - CONFIRED
3. MSAL public client flow works on Fedora Linux without a local HTTP redirect server (or one is trivial to add) - CONFIRED
4. MSAL and Graph SDK are trim-compatible with the existing self-contained build config - CONFIRED
5. SQLite via EF Core is performant for conflict queue, sync state, and delta tokens at scale (300 - CONFIRED–500k files)
6. "Personal Microsoft account" = `consumers` tenant — no multi-tenant requirements - CONFIRED
7. AppImage can register for OS autostart via `.desktop` file without root access

---

## 15. Post-MVP Roadmap

| Area              | Item                                                                              |
| ----------------- | --------------------------------------------------------------------------------- |
| **Platforms**     | Windows packaging and release                                                     |
| **Platforms**     | macOS packaging and release                                                       |
| **Sync**          | Resumable downloads via range requests (SE-16)                                    |
| **Sync**          | Bandwidth throttling per account                                                  |
| **Sync**          | Per-file sync progress UI                                                         |
| **Accounts**      | Entra ID / work account support                                                   |
| **Settings**      | "Reset to Defaults" option                                                        |
| **Settings**      | Configurable minimum free space threshold (EH-09)                                 |
| **Notifications** | Configurable notification granularity per account (ST-06)                         |
| **Theming**       | Accessible colour schemes — high contrast, colour-blind-friendly (TH-07)          |
| **Theming**       | Additional custom themes beyond Light/Dark/Auto (TH-08)                           |
| **Theming**       | Custom accent colour selection (TH-09)                                            |
| **Localisation**  | Additional locale translations (LO-08)                                            |
| **Accessibility** | Full keyboard-only operation for all interactions (including folder tree, wizard) |
| **Accessibility** | Screen reader support via AT-SPI2 / Orca (NF-12)                                  |
| **Updates**       | Scheduled update checks — not just at startup (UP-07)                             |
| **Updates**       | Self-updating AppImage — download, replace, restart (UP-08)                       |
| **Lifecycle**     | Bring existing instance to foreground on second launch attempt (AL-03)            |
| **Privacy**       | Expand PII masking to include file paths/names in logs and DB (LG-08)             |
| **Privacy**       | Formal GDPR data export / full data deletion beyond account removal               |
| **Log Viewer**    | Discuss with users: graduated access beyond Casual/Power binary                   |
| **Downgrade**     | Professional downgrade path (rollback DB migrations safely)                       |

---

## 16. Phased Delivery

### Phase 1 — MVP (Fedora KDE Plasma 43+ / x64)

All requirements marked **MVP** in sections 6.1–6.12, plus all NFRs from section 10.

**Deliverables:**

1. Rewritten desktop app at `apps/desktop/AStar.Dev.OneDriveSync/` with feature-slice architecture
2. Rewritten `AStar.Dev.Sync.Engine` package with feature-slice architecture
3. Rewritten `AStar.Dev.Conflict.Resolution` package with feature-slice architecture
4. Rewritten `AStar.Dev.OneDrive.Client` package with feature-slice architecture
5. Full test suites (`.Tests.Unit`) for all three packages
6. Integration tests for account deletion data integrity and PII containment
7. AppImage distribution via GitHub Releases

### Phase 2 — Post-MVP

Items from section 15, prioritised based on user feedback after MVP launch.

---

## 17. Next Steps

1. **Run technical spikes** on the remaining assumption in section 14 before sprint planning - specifically #7
2. **Audit new NuGet dependencies** for trim compatibility (`Microsoft.Identity.Client`, `Microsoft.Graph` / Kiota, tray library)
3. **Delete `AStar.Dev.OneDriveSync.old/`** once the new app scaffolding is in place
4. **Convert FRs to backlog items** (GitHub Issues) for sprint planning
5. **Validate reactive theme switching** — spike Avalonia `ThemeVariant` runtime swap with resource dictionary hot-reload to confirm approach before building all views- CONFIRMED
