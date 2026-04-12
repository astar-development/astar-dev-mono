# OneDrive Sync Client — MainWindowViewModel Refactor

## Problem

`MainWindowViewModel` has **10 constructor parameters** and handles 6 distinct responsibilities. It acts as a God object: creating child ViewModels, wiring sync events, orchestrating startup, managing navigation, and manually syncing the status bar. It is currently untestable in isolation.

---

## Responsibilities to Extract

| # | Responsibility | Current evidence | Target |
|---|---|---|---|
| 1 | **Sync event fan-out** | `OnSyncProgressChanged`, `OnJobCompleted`, `OnSyncCompleted`, `OnConflictDetected` (lines 236–290) | `ISyncEventAggregator` |
| 2 | **App initialization** | `InitialiseAsync` (lines 127–171) | `IApplicationInitializer` |
| 3 | **Child VM construction** | Inline `new(...)` for all 6 child VMs using deps that belong to the child | Inject child VMs from DI |
| 4 | **Manual sync trigger** | `SyncNowAsync` reconstructs `OneDriveAccount` from repo (lines 180–195) | Push account entity lookup into `ISyncScheduler` overload |
| 5 | **StatusBar sync** | `SyncStatusBarToActiveAccount()` called from 6 callsites | Subscribe inside `StatusBarViewModel` to `AccountsViewModel` |

---

## Phase 1 — Extract `ISyncEventAggregator`

**Why first:** removes 4 handler methods (~60 lines), 2 direct deps (`ISyncService`, `ISyncScheduler`), and makes sync events independently consumable by each child VM.

### New files

**`Infrastructure/Sync/ISyncEventAggregator.cs`**
```csharp
public interface ISyncEventAggregator
{
    event EventHandler<SyncProgressEventArgs>  SyncProgressChanged;
    event EventHandler<JobCompletedEventArgs>   JobCompleted;
    event EventHandler<SyncConflict>            ConflictDetected;
    event EventHandler<string>                  SyncCompleted;
}
```

**`Infrastructure/Sync/SyncEventAggregator.cs`**
- Constructor deps: `ISyncService`, `ISyncScheduler`
- Subscribes to both services in constructor, re-raises on UIThread via `Dispatcher.UIThread.Post`
- Registered as **Singleton** in DI

### Modified files

| File | Change |
|---|---|
| `Accounts/AccountsViewModel.cs` | Subscribe to `ISyncEventAggregator` — update card `SyncState` and `ConflictCount` |
| `Dashboard/DashboardViewModel.cs` | Subscribe — call `UpdateAccountSyncState`, `AddActivityItem`, `MarkSyncCompleted` |
| `Activity/ActivityViewModel.cs` | Subscribe — call `AddActivityItem`, `AddConflictItem` |
| `Home/MainWindowViewModel.cs` | Remove 4 handler methods + 2 deps; keep only `scheduler` for `SyncNowAsync` |

### Tests

New test class: `AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncEventAggregator.cs`

```
when_sync_service_raises_progress_then_aggregator_raises_progress
when_sync_service_raises_job_completed_then_aggregator_raises_job_completed
when_sync_service_raises_conflict_then_aggregator_raises_conflict
when_scheduler_raises_sync_completed_then_aggregator_raises_sync_completed
```

---

## Phase 2 — Extract `IApplicationInitializer`

**Why:** `InitialiseAsync` coordinates account restoration across 5 child VMs. It belongs in a dedicated service, not the shell VM.

### New files

**`Infrastructure/Shell/IApplicationInitializer.cs`**
```csharp
public interface IApplicationInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}
```

**`Infrastructure/Shell/ApplicationInitializer.cs`**
- Constructor deps: `IStartupService`, `AccountsViewModel`, `FilesViewModel`, `DashboardViewModel`, `ActivityViewModel`, `SettingsViewModel`
- Contains the body of the current `InitialiseAsync()`, minus event wiring (now done by `SyncEventAggregator`)
- Registered as **Scoped** (one per app session)

### Modified files

| File | Change |
|---|---|
| `Home/MainWindowViewModel.cs` | `InitialiseAsync` becomes `await _initializer.InitializeAsync()` |
| `Home/MainWindowViewModel.cs` | Remove deps: `IStartupService`, `ISyncService`, `ISyncRepository` |

`Accounts.PropertyChanged` wiring that triggers `SyncStatusBarToActiveAccount` moves to Phase 4.

### Tests

New test class: `AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Shell/GivenAnApplicationInitializer.cs`

```
when_initialized_then_accounts_are_restored_from_startup_service
when_initialized_then_files_receives_all_restored_accounts
when_initialized_then_dashboard_receives_all_restored_accounts
when_initialized_then_settings_loads_restored_accounts
when_active_account_exists_then_files_activates_that_account
when_active_account_exists_then_activity_sets_active_account
when_startup_service_throws_then_error_is_logged_and_not_rethrown
```

---

## Phase 3 — Inject Child ViewModels from DI

**Why:** `MainWindowViewModel` constructs child VMs inline, pulling in `IAuthService`, `IGraphService`, `IAccountRepository`, `ISettingsService`, `IThemeService`, `ILocalizationService` solely to forward them to children. These deps belong only to the child VMs.

### Changes

Register child VMs in DI (`App.axaml.cs` or the DI setup file):
```csharp
services.AddSingleton<AccountsViewModel>();
services.AddSingleton<FilesViewModel>();
services.AddSingleton<DashboardViewModel>();
services.AddSingleton<ActivityViewModel>();
services.AddSingleton<SettingsViewModel>();
services.AddSingleton<StatusBarViewModel>();
```

**`Home/MainWindowViewModel.cs`** — replace inline `new(...)` properties with injected parameters:
```csharp
// Before
public AccountsViewModel Accounts { get; } = new(authService, graphService, accountRepository);

// After (primary constructor injection)
public AccountsViewModel Accounts { get; }
// ...set from injected parameter
```

Removed deps from MainWindowViewModel: `IAuthService`, `IGraphService`, `ISettingsService`, `IThemeService`, `ILocalizationService`.

---

## Phase 4 — Reactive StatusBar (remove `SyncStatusBarToActiveAccount`)

**Why:** `SyncStatusBarToActiveAccount()` is called from 6 callsites — fragile, easy to miss. StatusBar should react to `AccountsViewModel.ActiveAccount` changes instead.

### Options (pick one)

**Option A — Subscribe in StatusBarViewModel** (recommended):
- `StatusBarViewModel` takes `AccountsViewModel` as a constructor dep
- Subscribes to `AccountsViewModel.PropertyChanged` for `nameof(ActiveAccount)`
- Self-updates its own properties

**Option B — XAML binding** (simpler, no subscription management):
- Bind `StatusBar.*` directly to `Accounts.ActiveAccount.*` in `MainWindow.axaml`
- Works if `ActiveAccount` raises `PropertyChanged` on its own properties

### Modified files

| File | Change |
|---|---|
| `Home/StatusBarViewModel.cs` | Add `AccountsViewModel` dep (Option A) or keep as-is (Option B) |
| `Home/MainWindowViewModel.cs` | Delete `SyncStatusBarToActiveAccount()` and all 6 callsites |

### Tests (Option A)

New test class: `AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Home/GivenAStatusBarViewModel.cs`

```
when_active_account_is_null_then_has_account_is_false
when_active_account_is_set_then_email_matches_account_email
when_active_account_sync_state_changes_then_status_bar_reflects_change
when_active_account_conflict_count_changes_then_status_bar_reflects_change
```

---

## Phase 5 — Simplify `SyncNowAsync`

**Why:** Reconstructs `OneDriveAccount` from `IAccountRepository` to pass to `scheduler.TriggerAccountAsync`. This logic already exists in `DashboardAccountViewModel.SyncNowAsync` — it's duplicated.

### Change

Add overload to `ISyncScheduler`:
```csharp
Task TriggerAccountAsync(string accountId, CancellationToken ct = default);
```

`SyncNowAsync` becomes:
```csharp
[RelayCommand]
private async Task SyncNowAsync()
{
    var active = Accounts.ActiveAccount;
    if(active is null)
        return;

    await _scheduler.TriggerAccountAsync(active.Id);
}
```

Removes `IAccountRepository` from `MainWindowViewModel`.

---

## Resulting MainWindowViewModel

**Before:** 10 deps, ~310 lines, 6 responsibilities

**After:** ~5 deps, ~120 lines, 1 responsibility (shell coordination)

```csharp
public sealed partial class MainWindowViewModel(
    IApplicationInitializer initializer,
    ISyncScheduler scheduler,
    AccountsViewModel accounts,
    FilesViewModel files,
    DashboardViewModel dashboard,
    ActivityViewModel activity,
    SettingsViewModel settings,
    StatusBarViewModel statusBar) : ObservableObject
```

Removed deps: `IAuthService`, `IGraphService`, `IStartupService`, `ISyncService`, `IThemeService`, `ISyncRepository`, `ISettingsService`, `IAccountRepository`, `ILocalizationService`

---

## Tests for Refactored MainWindowViewModel

New test class: `AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Home/GivenAMainWindowViewModel.cs`

```
when_created_then_active_section_is_dashboard
when_navigate_called_then_active_section_changes
when_navigate_to_dashboard_then_is_dashboard_active_is_true
when_navigate_to_accounts_then_is_accounts_active_is_true
when_sync_now_command_executed_with_no_active_account_then_scheduler_not_called
when_sync_now_command_executed_with_active_account_then_scheduler_called_with_account_id
when_add_account_command_executed_then_navigates_to_accounts_section
when_initialise_async_called_then_delegates_to_application_initializer
```

---

## Execution Order

```
Phase 1: ISyncEventAggregator     ← biggest impact, self-contained
Phase 2: IApplicationInitializer  ← depends on Phase 1 (removes event wiring from InitialiseAsync)
Phase 3: Inject child VMs         ← parallel with Phase 2, depends on DI registrations
Phase 4: Reactive StatusBar       ← depends on Phase 3 (StatusBarViewModel gets AccountsViewModel)
Phase 5: Simplify SyncNowAsync    ← small, can be done any time
```

---

## TDD Commit Sequence (per phase)

```
test(desktop/onedrive): failing tests for <phase name>
feat(desktop/onedrive): implement <phase name>
refactor(desktop/onedrive): clean up after <phase name>  [if needed]
```

Branch: `feature/onedrive-mainwindow-decompose`

---

## Files Created / Modified (full list)

### New files
- `Infrastructure/Sync/ISyncEventAggregator.cs`
- `Infrastructure/Sync/SyncEventAggregator.cs`
- `Infrastructure/Shell/IApplicationInitializer.cs`
- `Infrastructure/Shell/ApplicationInitializer.cs`
- `Tests.Unit/Infrastructure/Sync/GivenASyncEventAggregator.cs`
- `Tests.Unit/Infrastructure/Shell/GivenAnApplicationInitializer.cs`
- `Tests.Unit/Home/GivenAMainWindowViewModel.cs`
- `Tests.Unit/Home/GivenAStatusBarViewModel.cs` (if Option A)

### Modified files
- `Home/MainWindowViewModel.cs` (primary target — shrinks from 310 → ~120 lines)
- `Accounts/AccountsViewModel.cs` (subscribe to ISyncEventAggregator)
- `Dashboard/DashboardViewModel.cs` (subscribe to ISyncEventAggregator)
- `Activity/ActivityViewModel.cs` (subscribe to ISyncEventAggregator)
- `Home/StatusBarViewModel.cs` (reactive to AccountsViewModel.ActiveAccount)
- `Infrastructure/Sync/ISyncScheduler.cs` (add accountId overload)
- DI registration file (register child VMs as Singleton)
