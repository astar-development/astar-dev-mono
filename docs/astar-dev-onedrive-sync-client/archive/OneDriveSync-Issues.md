# OneDrive Sync Desktop — Code Review

**Branch:** `feature/onedrive-mainwindow-decompose`
**Commits reviewed:** ef73cad → aa602c6 (Phases 1–5 of MainWindowViewModel refactor)

---

## Production Code Issues

---

### Issue 1 - Done

**File:** [MainWindowViewModel.cs:54](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L54)
**Severity:** `warning`
**Issue:** `ActiveView` getter assigns to a local variable only to return it immediately; intermediate variable adds no value and forces a `return` that is not preceded by a blank line.

**Fix:**

```csharp
public object? ActiveView => ActiveSection switch
{
    NavSection.Dashboard => DashboardViewInstance,
    NavSection.Files     => FilesViewInstance,
    NavSection.Activity  => ActivityViewInstance,
    NavSection.Accounts  => AccountsViewInstance,
    NavSection.Settings  => SettingsViewInstance,
    _                    => null
};
```

---

### Issue 2 - Done

**File:** [MainWindowViewModel.cs:120](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L120)
**Severity:** `error`
**Issue:** `InitialiseAsync` swallows all exceptions with a bare `catch(Exception)` and logs a `Fatal` message using a Serilog static accessor, hiding failures from callers and bypassing the functional error-handling pattern; should use `Try.RunAsync` (already used elsewhere in the same class) and propagate or surface failures.

**Fix:**

```csharp
public async Task InitialiseAsync()
{
    accounts.AccountSelected += OnAccountSelectedAsync;
    accounts.AccountAdded += OnAccountAddedAsync;
    accounts.AccountRemoved += OnAccountRemoved;

    await Try.RunAsync(async () =>
        {
            await initializer.InitializeAsync();
            return Unit.Default;
        })
        .TapErrorAsync(e => Serilog.Log.Fatal(e, "[MainWindowViewModel.InitialiseAsync] FATAL ERROR: {Error}", e));
}
```

---

### Issue 3 - Done

**File:** [MainWindowViewModel.cs:137](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L137)
**Severity:** `warning`
**Issue:** `SyncNowAsync` awaits `scheduler.TriggerAccountAsync` without `ConfigureAwait(false)`; all awaits in infrastructure/library code must use `ConfigureAwait(false)`.

**Fix:**

```csharp
await scheduler.TriggerAccountAsync(active.Id).ConfigureAwait(false);
```

---

### Issue 4

**File:** [MainWindowViewModel.cs:143](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L143)
**Severity:** `warning`
**Issue:** `SyncNowAsync` uses a raw `string` account ID (`active.Id`) as a primitive; per the repo's Primitive Obsession convention, account IDs should be strongly-typed. This is systemic — also present in `ISyncScheduler`, `SyncScheduler`, and `TriggerAccountAsync(string accountId, ...)`.

---

### Issue 5

**File:** [MainWindowViewModel.cs:153–161](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L153-L161)
**Severity:** `warning`
**Issue:** `OnAccountSelectedAsync` and `OnAccountAddedAsync` use `Try.RunAsync` correctly for `async void` event handlers, but the inner awaits on `files.ActivateAccountAsync` and `activity.SetActiveAccountAsync` lack `ConfigureAwait(false)`.

**Fix:** Add `.ConfigureAwait(false)` to each awaited call inside the `Try.RunAsync` lambdas.

---

### Issue 6

**File:** [MainWindowViewModel.cs:20](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L20)
**Severity:** `suggestion`
**Issue:** Primary constructor has 8 parameters, exceeding the recommended maximum of 5; consider a parameter object (e.g., `MainWindowViewModelDependencies`) to group child view models.

---

### Issue 7

**File:** [MainWindowViewModel.cs:108–118](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs#L108-L118)
**Severity:** `suggestion`
**Issue:** Public pass-through properties (`Accounts`, `Files`, `Activity`, `Dashboard`, `Settings`) expose injected child view-model dependencies directly; if required by XAML, add XML doc comments explaining why.

---

### Issue 8 - Incorrect

**File:** [StatusBarViewModel.cs:13–23](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/StatusBarViewModel.cs#L13-L23)
**Severity:** `warning`
**Issue:** `StatusBarViewModel` uses a traditional constructor with explicit `private readonly` fields instead of a primary constructor; the ReactiveUI exception does not apply here (no `ReactiveCommand.CreateFromTask` usage), so a primary constructor must be used.

---

### Issue 9

**File:** [StatusBarViewModel.cs:58–66](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/StatusBarViewModel.cs#L58-L66)
**Severity:** `suggestion`
**Issue:** All public properties on `StatusBarViewModel` lack XML doc comments; all public members on public types must be documented per repo convention.

---

### Issue 10 - Incorrect

**File:** [StatusBarViewModel.cs:100–122](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Home/StatusBarViewModel.cs#L100-L122)
**Severity:** `warning`
**Issue:** `ApplyActiveAccount` has a `return` inside the `if(active is null)` branch immediately preceded by `AccountDisplayName = string.Empty;` with no blank line before the `return`, violating the return-spacing rule.

**Fix:**

```csharp
if(active is null)
{
    HasAccount = false;
    AccountEmail = string.Empty;
    AccountDisplayName = string.Empty;

    return;
}
```

---

### Issue 11 - Done

**File:** [ApplicationInitializer.cs:13](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Shell/ApplicationInitializer.cs#L13)
**Severity:** `error`
**Issue:** `InitializeAsync` swallows all exceptions with `catch(Exception)` and logs `Fatal` via the Serilog static accessor; callers receive a completed `Task` after a fatal boot failure and cannot distinguish success from failure. Use `Try.RunAsync` or re-throw after logging.

---

### Issue 12 - Done

**File:** [ApplicationInitializer.cs:35–38](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Shell/ApplicationInitializer.cs#L35-L38)
**Severity:** `warning`
**Issue:** `foreach` body awaits on lines 36–37 are missing a blank line before the closing `if`; formatting only (see Issue 10 for the rule).

---

### Issue 13 - Done

**File:** [ISyncScheduler.cs:10–12](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/ISyncScheduler.cs#L10-L12)
**Severity:** `warning`
**Issue:** `#pragma warning disable CA1716` suppression on `Stop()` has no documented reason comment, violating the repo rule that all suppressions must explain why.

**Fix:**

```csharp
// CA1716: 'Stop' conflicts with a VB reserved keyword. Suppressed because this is a C#-only desktop app
// with no VB consumers and renaming would break the existing public interface contract.
#pragma warning disable CA1716
void Stop();
#pragma warning restore CA1716
```

---

### Issue 14 - Done

**File:** [ISyncScheduler.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/ISyncScheduler.cs)
**Severity:** `suggestion`
**Issue:** `Start`, `Stop`, and `SetInterval` have no XML doc comments; all public interface members must be documented.

---

### Issue 15 - Incorrect

**File:** [SyncEventAggregator.cs:26](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncEventAggregator.cs#L26)
**Severity:** `warning`
**Issue:** `SyncEventAggregator` uses an explicit constructor instead of a primary constructor without any ReactiveUI justification; should use a primary constructor with event subscriptions in the constructor body.

---

### Issue 16 ⚠️ Critical - Done

**File:** [SyncScheduler.cs:106](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L106)
**Severity:** `error`
**Issue:** `Interlocked.Exchange(ref _running, true)` returns the **previous** value and assigns it back to `_running`, immediately resetting the flag to `false`. The re-entrancy guard on line 99 (`if(_running) return;`) is completely ineffective — concurrent sync passes are never suppressed.

**Fix:**

```csharp
private int _runningFlag; // replace the bool _running field

private async Task RunSyncPassAsync(CancellationToken ct)
{
    if(Interlocked.Exchange(ref _runningFlag, 1) == 1)
        return;

    try
    {
        // ... sync pass body
    }
    finally
    {
        Interlocked.Exchange(ref _runningFlag, 0);
    }
}
```

---

### Issue 17

**File:** [SyncScheduler.cs:87](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L87)
**Severity:** `warning`
**Issue:** `TriggerAccountAsync` calls `syncService.SyncAccountAsync` without `ConfigureAwait(false)`.

**Fix:**

```csharp
await syncService.SyncAccountAsync(account, ct).ConfigureAwait(false);
```

---

### Issue 18

**File:** [SyncScheduler.cs:53](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L53)
**Severity:** `warning`
**Issue:** `TriggerNowAsync` awaits `RunSyncPassAsync` without `ConfigureAwait(false)`.

**Fix:**

```csharp
await RunSyncPassAsync(ct).ConfigureAwait(false);
```

---

### Issue 19

**File:** [SyncScheduler.cs:110](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L110)
**Severity:** `warning`
**Issue:** `accountRepository.GetAllAsync` is called with `CancellationToken.None` inside `RunSyncPassAsync`, discarding the `ct` parameter that was correctly threaded through to `TriggerNowAsync`.

**Fix:**

```csharp
var entities = await accountRepository.GetAllAsync(ct).ConfigureAwait(false);
```

---

### Issue 20

**File:** [SyncScheduler.cs:111–121](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L111-L121)
**Severity:** `warning`
**Issue:** LINQ chain inside `RunSyncPassAsync` embeds object construction and cancellation logic in a single chained expression with an anonymous lambda spanning many lines; extract the mapping to a named method or local function.

**Fix:**

```csharp
var accounts = entities
    .TakeWhile(_ => !ct.IsCancellationRequested)
    .Select(MapToOneDriveAccount);

private static OneDriveAccount MapToOneDriveAccount(AccountEntity entity) => new()
{
    Id          = entity.Id,
    DisplayName = entity.DisplayName,
    // ...
};
```

---

### Issue 21

**File:** [SyncScheduler.cs:96](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L96)
**Severity:** `warning`
**Issue:** ReSharper `// AsyncVoidMethod` suppression on the timer callback is acceptable (reason is stated), but should reference the team-agreed policy for `async void` in timer callbacks to be explicit about scope.

---

### Issue 22

**File:** [SyncScheduler.cs:144–150](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Sync/SyncScheduler.cs#L144-L150)
**Severity:** `warning`
**Issue:** `DisposeAsync` awaits `_timer.DisposeAsync()` without `ConfigureAwait(false)`.

**Fix:**

```csharp
public async ValueTask DisposeAsync()
{
    Stop();

    if(_timer is not null)
        await _timer.DisposeAsync().ConfigureAwait(false);
}
```

---

### Issue 23 ⚠️ Critical

**File:** [ShellServiceExtensions.cs:44](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Startup/ShellServiceExtensions.cs#L44)
**Severity:** `error`
**Issue:** `services.BuildServiceProvider()` is called mid-registration to resolve `IOptions<EntraIdConfiguration>`, creating a second DI container and risking duplicate singleton instances.

**Fix:** Accept options as a parameter instead:

```csharp
internal static IServiceCollection AddShell(this IServiceCollection services, InMemoryLogSink inMemoryLogSink, IOptions<EntraIdConfiguration> entraIdConfiguration)
{
    _ = services.AddOneDriveClient(entraIdConfiguration);
    return services;
}
```

---

### Issue 24 - Done

**File:** [ViewModelExtensions.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Startup/ViewModelExtensions.cs)
**Severity:** `suggestion`
**Issue:** `ViewModelExtensions` is `public static` but should be `internal static`; DI registration is not a public API in a desktop app project.

---

## Test Code Issues

---

### Issue 25 - Done

**File:** [GivenASyncScheduler.cs:7](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncScheduler.cs#L7)
**Severity:** `warning`
**Issue:** Test class namespace is `...Tests.Unit.Services.Sync` but the file lives under `Infrastructure/Sync/`; namespace must mirror the folder path (`...Tests.Unit.Infrastructure.Sync`).

---

### Issue 26

**File:** [GivenASyncScheduler.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncScheduler.cs) — multiple methods
**Severity:** `warning`
**Issue:** Most tests assert only `scheduler.ShouldNotBeNull()` — tests that verify nothing beyond construction are not meaningful and create false coverage confidence. Actual behaviours (timer ticking, cancellation propagation, re-entrancy guard) are untested.

Affected: `when_constructed_then_scheduler_is_not_null`, `when_started_then_*`, `when_stopped_after_start_then_*`, `when_interval_set_after_start_then_*`, `when_trigger_now_called_then_*`, `when_started_with_various_intervals_then_*`, `when_scheduler_created_then_it_is_async_disposable`.

---

### Issue 27

**File:** [GivenASyncScheduler.cs:17](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncScheduler.cs#L17)
**Severity:** `suggestion`
**Issue:** Each test manually constructs `new SyncScheduler(...)` rather than using a `CreateSut()` factory method, duplicating setup across 17 tests; use the same pattern as `GivenAMainWindowViewModel`.

---

### Issue 28

**File:** [GivenASyncScheduler.cs:183](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncScheduler.cs#L183)
**Severity:** `warning`
**Issue:** `when_trigger_account_and_sync_service_throws_then_completed_event_still_raised` declares `ex` but never asserts on it (dead variable); the test does not verify the `finally` block ran correctly.

---

### Issue 29

**File:** [GivenAMainWindowViewModel.cs:104–105](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Home/GivenAMainWindowViewModel.cs#L104-L105)
**Severity:** `warning`
**Issue:** `when_sync_now_command_executed_with_active_account_then_scheduler_called_with_account_id` constructs `MainWindowViewModel` directly with 8 positional arguments instead of using `CreateSut()`, making the test fragile to constructor changes.

---

### Issue 30

**File:** [GivenASyncScheduler.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncScheduler.cs) — missing coverage
**Severity:** `warning`
**Issue:** `RunSyncPassAsync` re-entrancy guard (Issue 16) has no test; once Issue 16 is fixed, a test verifying that a second concurrent call is suppressed must be added.

---

### Issue 31

**File:** [GivenAMainWindowViewModel.cs:129](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Home/GivenAMainWindowViewModel.cs#L129)
**Severity:** `suggestion`
**Issue:** `when_initialise_async_called_then_delegates_to_application_initializer` is the only test for `InitialiseAsync` and does not exercise the event-subscription side-effects (`accounts.AccountSelected`, `accounts.AccountAdded`, `accounts.AccountRemoved`).

---

### Issue 32

**File:** [GivenASyncEventAggregator.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Infrastructure/Sync/GivenASyncEventAggregator.cs)
**Severity:** `suggestion`
**Issue:** No test verifies that events are dispatched via `IUiDispatcher`; a spy dispatcher test would verify the dispatch contract independently of `InlineUiDispatcher`.

---

SyncEntities.cs:L162-164, SyncFolderEntity.cs:L182,185, SyncJobEntity.cs:L205-207: Same pattern — all strongly-typed FK properties uninitialized. Same null-inner-string risk on new entity construction.

AccountEntity.cs:L140: = LocalSyncPath.Restore(string.Empty) — sentinel empty value leaks EF-only bypass into domain default. Detection scattered: SyncScheduler checks entity.LocalSyncPath.Value.Length > 0 not is null. OneDriveAccount.LocalSyncPath is LocalSyncPath?, entity is non-nullable with empty sentinel — asymmetric nullability model between domain and entity.

SyncScheduler.cs:L55: TriggerAccountAsync(string accountId, ...) still accepts raw string, immediately wraps with new AccountId(accountId). Signature should be AccountId accountId; callers that have strings should wrap at their boundary.

SyncRepository.cs:L370-372: new AccountId(j.AccountId), new OneDriveFolderId(j.FolderId) etc — SyncJob domain model still uses raw strings. Mapping leaks wrapping into infra layer. SyncJob should carry typed IDs if it's domain-level.

AccountsViewModel.cs:L100-101: LocalSyncPathFactory.Create(defaultPath).Match<LocalSyncPath?>(p => p, \_ => null) — factory failure silently yields null; sync then fails with generic "No local sync path configured". Log the failure at warn level so it's diagnosable.

## Summary

| Severity     | Count |
| ------------ | ----- |
| `error`      | 4     |
| `warning`    | 18    |
| `suggestion` | 10    |

**Verdict: Request changes.**

### Blockers (must fix before merge)

1. **Issue 16** — `SyncScheduler.RunSyncPassAsync` re-entrancy guard is logically inverted; `Interlocked.Exchange` immediately resets `_running` to `false`. Concurrent sync passes are never suppressed — risk of data corruption or duplicate work at runtime.
2. **Issue 23** — `ShellServiceExtensions` calls `BuildServiceProvider()` mid-registration, creating a second DI container and risking duplicate singleton instances.
3. **Issues 2 & 11** — Fatal boot failures in `MainWindowViewModel.InitialiseAsync` and `ApplicationInitializer.InitializeAsync` are silently swallowed; the app appears to start correctly after a critical failure.

All `ConfigureAwait(false)` omissions (Issues 3, 5, 17, 18, 22) must also be resolved — `TreatWarningsAsErrors=true` may surface the analyzer rule depending on active analyzers.
