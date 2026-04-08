# Code Review — `apps/desktop/AStar.Dev.OneDrive.Sync.Client`

> Review date: 2026-04-05. All file paths are relative to the repo root.

---

## Blocker

### B-01 — Security | Hardcoded OAuth Client ID

**Files:** `apps/desktop/AStar.Dev.OneDrive.Sync.Client/Services/AuthService.cs:18`, `appsettings.json:3,22`

The Azure AD Client ID `3057f494-687d-4abb-a653-4b8066230b6e` is hardcoded as a `const` in `AuthService.cs` AND duplicated in `appsettings.json` (both `EntraId` and `Authentication` sections). `AuthService` reads only the hardcoded constant — configuration drift is guaranteed, and rotating the Client ID requires a code change + rebuild.

**Fix:** Remove the constant; inject `IOptions<AuthOptions>` bound to a single config section.

```csharp
public sealed class AuthService(TokenCacheService cacheService, IOptions<AuthOptions> options) : IAuthService
{
    private readonly IPublicClientApplication _app = PublicClientApplicationBuilder
        .Create(options.Value.ClientId)
        .WithAuthority(options.Value.Authority)
        .WithRedirectUri(options.Value.RedirectUri)
        .Build();
```

---

### B-02 — Race Condition | `_running` / `_cacheRegistered` not thread-safe

**Files:** `Services/Sync/SyncScheduler.cs:17,56,80,87`, `Services/AuthService.cs:39,126-130`

`SyncScheduler._running` is a plain `bool` mutated from the `Timer` callback thread and `TriggerNowAsync` on any caller thread. `AuthService._cacheRegistered` has the same problem. Both are TOCTOU races — two threads can simultaneously read `false` and both proceed.

**Fix for `_running`:** use `Interlocked.CompareExchange`:

```csharp
private int _running; // 0 = idle, 1 = busy

private async Task RunSyncPassAsync(CancellationToken ct)
{
    if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        return;

    try { /* sync work */ }
    finally { Interlocked.Exchange(ref _running, 0); }
}
```

**Fix for `_cacheRegistered`:** use `SemaphoreSlim(1,1)` or `Lazy<Task>` so concurrent callers cannot both register the cache.

---

### B-03 — Architecture | Service Locator (`App.*` static properties)

**Files:** `App.axaml.cs:21-27`, `ViewModels/MainWindowViewModel.cs:112-114,176`, `ViewModels/DashboardViewModel.cs:37`, `ViewModels/AccountsViewModel.cs:83`

`App.Localisation`, `App.Theme`, `App.Auth`, `App.Repository`, `App.SyncService`, `App.Scheduler`, `App.AppSettings` are static properties resolved by ViewModels at runtime. This is the Service Locator anti-pattern: unit testing any ViewModel that touches `App.*` is impossible, and the declared constructor contract is a lie.

**Fix:** Remove all `App.*` static service properties. Pass every dependency through constructors; forward already-injected dependencies from parent VMs to child VMs rather than going back to the static bag.

---

### B-04 — Architecture | No DI container — manual `new` throughout

**File:** `App.axaml.cs:72-103`

Every service is manually `new`-ed in `BootstrapAsync`. No `IServiceCollection` / `IServiceProvider` exists. This violates the CLAUDE.md mandate "Dependency Injection is NEVER an after-thought" and makes every class non-substitutable in tests.

**Fix:** Introduce `IServiceCollection` in `Program.cs`; register all services; resolve `MainWindow` from the container. Use `GenericHost` / `IHostBuilder` — the standard pattern for Avalonia + DI.

---

### B-05 — TDD/Tests | Zero test coverage

**Scope:** entire project

No `*.Tests.Unit` or `*.Tests.Integration` project exists anywhere in the repo for this application. Every piece of logic — conflict resolution, delta-processing, scheduler, local change detection, repository methods, converters — is completely untested. The sync engine is production-critical and has no safety net.

**Minimum required coverage on first test project:**

| Class | Scenarios to cover |
|---|---|
| `ConflictResolver.Resolve` | all five `ConflictPolicy` values |
| `LocalChangeDetector.DetectChanges` | cutoff filtering, hidden-file exclusion, double-recursion bug (see m-11) |
| `SyncScheduler` | timer behaviour, `TriggerNowAsync` re-entrancy guard |
| `AuthResult` factory methods | success, error, null guard |
| `SettingsService` | serialisation/deserialisation roundtrip, corrupt-file fallback |

---

### B-06 — Security | PII (email, display name) logged unredacted

**Files:** `Services/Sync/SyncService.cs:20,36,299`, `Services/Startup/StartupService.cs:28-31`, `ViewModels/AccountsViewModel.cs:99`

User email addresses and display names are written to `sync.txt` at `Information` level and to `Debug.WriteLine`. CLAUDE.md rule: "No PII/secrets — use `HashedUserId` pattern; redact with `Serilog.Expressions` if needed."

**Fix:** Hash or mask the email before logging; replace all `Debug.WriteLine` PII calls with structured Serilog calls using hashed/masked values.

---

## Major

### M-01 — `.csproj` re-declares `Directory.Build.props` properties

**File:** `AStar.Dev.OneDrive.Sync.Client.csproj:4,5`

`<TargetFramework>net10.0</TargetFramework>` and `<Nullable>enable</Nullable>` are already provided by `Directory.Build.props`. The rule is: never duplicate settings already defined there.

**Fix:** Remove both lines from the `.csproj`.

---

### M-02 — Race Condition | `async void` on non-Avalonia-event handlers

**Files:** `App.axaml.cs:55`, `ViewModels/AccountsViewModel.cs:76`, `ViewModels/AccountFilesViewModel.cs:107`, `ViewModels/MainWindowViewModel.cs:201,209`

`OnWizardCompleted`, `OnIncludeToggled`, `OnAccountSelected`, `OnAccountAdded` are `EventHandler<T>` delegates attached in code — not Avalonia event handlers. `async void` swallows all exceptions silently.

**Fix:** Use `[RelayCommand]` async methods or `Dispatcher.UIThread.InvokeAsync(...)` and return an awaitable task so failures are observable.

---

### M-03 — Code Quality | `ClassifyJobsAsync` is `static async` with only `await Task.CompletedTask`

**File:** `Services/Sync/SyncService.cs:226-288`

The method is entirely synchronous; the only `await` is `await Task.CompletedTask` — a dummy to satisfy the compiler. The `async` state machine allocates for nothing.

**Fix:** Remove `async`, return the tuple directly, call synchronously.

```csharp
private static (List<SyncJob> Clean, List<SyncConflict> Conflicts) ClassifyJobs(...)
```

---

### M-04 — Code Quality | Typo in method name `ProcessPownloadDeltas`

**File:** `Services/Sync/SyncService.cs:153`

"Pownload" — will confuse every future reader and grep.

**Fix:** Rename to `ProcessDownloadDeltasAsync`.

---

### M-05 — Code Quality | `GraphService` token-keyed cache — memory leak

**File:** `Services/Graph/GraphService.cs:15,246-260`

`_cache` is `Dictionary<string, DriveContext>` keyed on the raw access token. OAuth tokens rotate every ~1 hour; every new token creates a new entry that is never evicted.

**Fix:** Key by stable account ID, not the access token.

```csharp
private readonly Dictionary<string, DriveContext> _cache = []; // key = accountId
```

---

### M-06 — Architecture | `SettingsService.LoadAsync` silently discards deserialisation errors

**File:** `Services/SettingsService.cs:38-48`

A corrupt settings file silently resets all user preferences with no diagnostic.

**Fix:**

```csharp
catch (Exception ex)
{
    Log.Warning(ex, "Failed to load settings from {Path}; using defaults", svc._path);
    svc.Current = new AppSettings();
}
```

---

### M-07 — Architecture | Technical-type folders violate feature-first organisation rule

**Scope:** `ViewModels/`, `Converters/`, `Controls/`, `Views/`

CLAUDE.md explicitly calls out `ViewModels/`, `Converters/`, `Validators/` as violations. Organise by business feature.

**Fix:**

```
Features/
  Dashboard/
    DashboardView.axaml
    DashboardViewModel.cs
    DashboardAccountViewModel.cs
  Activity/
    ActivityView.axaml
    ActivityViewModel.cs
  Accounts/
    AccountsView.axaml
    AccountsViewModel.cs
    ...
```

---

### M-08 — Code Quality | `HttpClient` created with `new` — socket exhaustion risk

**Files:** `Services/Sync/HttpDownloader.cs:18`, `Services/Sync/UploadService.cs:29`

`new HttpClient()` is instantiated directly in short-lived objects. Fixing B-04 (DI container) also enables the correct fix here.

**Fix:** Inject `IHttpClientFactory` and call `CreateClient(...)`.

---

### M-09 — Missing Feature | No `AsNoTracking()` on read-only EF queries

**Files:** `Data/Repositories/AccountRepository.cs:9-12`, `Data/Repositories/SyncRepository.cs:31-36,74-80,88-91`

All read-only queries (`GetAllAsync`, `GetByIdAsync`, `GetPendingJobsAsync`, `GetPendingConflictsAsync`, `GetPendingConflictCountAsync`) unnecessarily track entities in the change tracker.

**Fix:**

```csharp
public Task<List<AccountEntity>> GetAllAsync()
    => db.Accounts
        .AsNoTracking()
        .Include(a => a.SyncFolders)
        .OrderBy(a => a.Email)
        .ToListAsync();
```

---

### M-10 — Code Quality | Inline comments restate the code

**Files:** `Data/Repositories/AccountRepository.cs:31,33,55,59`, `Services/Startup/StartupService.cs:26,29,38,60`, `Services/TokenCacheService.cs:40-44,64-66,80-82`, `Services/AuthService.cs:5-15`, `Services/Sync/SyncService.cs:22`, `Services/Sync/LocalChangeDetector.cs:21`

Comments such as `// Update scalar properties`, `// Clear all active flags then set the requested one`, `// Only restore accounts that still have a valid cached MSAL token` restate exactly what the code says. CLAUDE.md rule: never comment within methods or private members.

**Fix:** Delete all comments that describe what rather than why.

---

### M-11 — Architecture | Entity configurations are not `sealed`

**Files:** `Data/Configuration/AccountEntityConfiguration.cs:8`, `Data/Configuration/SyncFolderEntityConfiguration.cs:8`, `Data/Configuration/SyncJobEntityConfiguration.cs:8`, `Data/Configuration/SyncConflictEntityConfiguration.cs:8`

EF configuration classes have no designed extension points.

**Fix:** Add `sealed` to all four.

---

### M-12 — Missing Feature | Logging does not use `AStar.Dev.Logging.Extensions` `LogMessage`

**Scope:** every file calling `Serilog.Log.*`

All logging uses raw string literals. CLAUDE.md rule: "Use `LogMessage` class for compile-time logging templates."

**Fix:** Add `<ProjectReference>` to `AStar.Dev.Logging.Extensions`; replace ad-hoc log calls with `LogMessage` templates; add new `LogMessage` entries for any missing templates.

---

### M-13 — Code Quality | Duplicate palette arrays

**File:** `ViewModels/AccountCardViewModel.cs:99-121`

`AccentPalette` and `Palette` are two `static readonly string[]` fields with identical values. One is redundant.

**Fix:** Remove `AccentPalette`; replace `AccentPalette[...]` references with `Palette[...]`.

---

### M-14 — Code Quality | `return` not preceded by blank line

CLAUDE.md rule: every `return` must be preceded by a blank line. Violations:

| File | Lines |
|---|---|
| `Services/Sync/SyncService.cs` | 32, 40 |
| `Services/Sync/HttpDownloader.cs` | 78 |
| `Services/Graph/GraphService.cs` | 83-86, 97-99 |
| `Services/Startup/StartupService.cs` | 41 |
| `Data/Repositories/AccountRepository.cs` | 27 |
| `ViewModels/AddAccountWizardViewModel.cs` | 56-63 |

---

### M-15 — Code Quality | `Debug.WriteLine` in production code paths

**Files:** `Services/Startup/StartupService.cs:28,31`, `ViewModels/AccountsViewModel.cs:99`, `Services/Sync/SyncScheduler.cs:116-118`

`Debug.WriteLine` is silently dropped in Release builds; failures are invisible in production.

**Fix:** Replace with structured `Serilog.Log.Debug(...)` / `Serilog.Log.Warning(...)` calls.

---

## Minor

### m-01 — Naming | Typo in constant: `AuthorityForMicrosoftAccountsOunly`

**File:** `Services/AuthService.cs:21`

**Fix:** Rename to `AuthorityForMicrosoftAccountsOnly`.

---

### m-02 — Naming | Typo in field: `MaxLogSixe`

**File:** `ViewModels/ActivityViewModel.cs:16`

**Fix:** Rename to `MaxLogSize`.

---

### m-03 — Naming | Typo in method: `GetUpdloadedDocumentId`

**File:** `Services/Sync/UploadService.cs:173`

**Fix:** Rename to `GetUploadedDocumentIdAsync`.

---

### m-04 — Code Quality | Magic debug string left in progress event

**File:** `Services/Sync/SyncService.cs:24`

`currentFile: "TEST TEST TEST !!!"` is passed in a progress event raised during production sync.

**Fix:** Pass the actual current file name or `string.Empty`.

---

### m-05 — Code Quality | Debug suffix left in `UpdateLastSyncText`

**File:** `ViewModels/DashboardAccountViewModel.cs:130`

`"Just now 2"` — the trailing `2` is a debug artifact.

**Fix:** Change to `"Just now"`.

---

### m-06 — Code Quality | `AccountSettings` is dead code

**File:** `Models/AccountSettings.cs`

Declared but never used. A comment even notes "kept as a plain model for now" indicating it was never wired up.

**Fix:** Remove the file, or integrate it if it has a genuine future purpose.

---

### m-07 — Code Quality | `SyncJob` record has unjustified mutable `set` properties

**File:** `Models/SyncJob.cs:17-23`

`State`, `ErrorMessage`, `CompletedAt` are `set` while all others are `init`. Repo rule prefers immutable records; `with` expressions already handle in-place updates (used on line 52 of `ParallelDownloadPipeline.cs`).

**Fix:** Make all properties `init`-only; use `with` expressions throughout.

---

### m-08 — Code Quality | Multiple types per file

**Files:**

| File | Types declared |
|---|---|
| `ViewModels/AddAccountWizardViewModel.cs` | `WizardStep`, `WizardFolderItem`, `AddAccountWizardViewModel` |
| `Services/SettingsService.cs` | `ISettingsService`, `SettingsService` |
| `Services/Startup/StartupService.cs` | `IStartupService`, `StartupService` |
| `Models/SyncConflict.cs` | `ConflictState`, `SyncConflict` |

CLAUDE.md rule: one class per file, named after the class.

**Fix:** Split each into its own file.

---

### m-09 — Architecture | Folder-namespace mismatch for `LocalizationService`

**Files:** `Localization/LocalizationService.cs:6`, `Localization/ILocalizationService.cs`

Namespace is `Services.Localization`; files are under `Localization/` at the project root, not `Services/Localization/`.

**Fix:** Either move the files to `Services/Localization/` or rename the namespace to match the folder.

---

### m-10 — Code Quality | Suppressed `CA1822` with no documented reason

**File:** `Services/Sync/LocalChangeDetector.cs:21-23`

`#pragma warning disable CA1822` with no comment. CLAUDE.md rule: suppressions require a documented reason.

**Fix:** Make the method `static` (it has no instance state), or document why the suppression is necessary.

---

### m-11 — Code Quality | `LocalChangeDetector` double-processes all subdirectory files

**File:** `Services/Sync/LocalChangeDetector.cs:51`

`Directory.EnumerateFiles(localDir, "", SearchOption.AllDirectories)` enumerates all files recursively, AND `ProcessSubDirectories` also manually recurses — every file in subdirectories is processed twice.

**Fix:** Choose one approach: either `SearchOption.AllDirectories` and delete `ProcessSubDirectories`, or `SearchOption.TopDirectoryOnly` with manual recursion. Also change `""` to `"*"` for idiomatic clarity.

---

### m-12 — Code Quality | Dead variable `isComplete`

**File:** `Services/Sync/ParallelDownloadPipeline.cs:59`

`bool isComplete = completedSoFar == total;` is computed but never read.

**Fix:** Remove the variable.

---

### m-13 — Architecture | `SyncProgressEventArgs` depends on a ViewModel type

**File:** `Services/Sync/SyncProgressEventArgs.cs:3`

`SyncProgressEventArgs` (in `Services.Sync`) imports `SyncState` from `ViewModels`. Services must not depend on ViewModel types.

**Fix:** Move `SyncState` (and `ActivityTab`, `ActivityItemType`, `SyncIntervalOption`, `ThemeOption`, `WizardStep`, `ConflictPolicyOption`) from `ViewModels` to `Models` or a `Domain` namespace.

---

### m-14 — Code Quality | `IsFileToSkip` is an unreadable single-expression one-liner

**File:** `Services/Sync/LocalChangeDetector.cs:109`

Four logical conditions on one line with no breaks. CLAUDE.md: methods must be readable; a long chain is a smell even in an expression-bodied member.

**Fix:** Extract a named predicate per condition or break the expression across lines with meaningful variable names.

---

## Info

### i-01 — Naming | Boilerplate typo in `Program.cs` comment

**File:** `Program.cs:54`

`"also used by visualdddd designer"` — "visualdddd" should be "visual". This is also an inline comment that restates Avalonia boilerplate.

**Fix:** Remove or correct.

---

### i-02 — Code Quality | `SyncScheduler` exposed as concrete type on `App`

**File:** `App.axaml.cs:26`

`public static SyncScheduler Scheduler` uses the concrete type. Exposes implementation detail; blocks substitution in tests (relates to B-03).

---

### i-03 — Code Quality | `SaveAsync` task discarded in `SettingsViewModel`

**File:** `ViewModels/SettingsViewModel.cs:19,27,44`

`_ = settingsService.SaveAsync();` in all three `partial void On...Changed` methods. Save failures are silently ignored.

**Fix:** At minimum, log the failure; ideally surface it to the UI.

---

### i-04 — Code Quality | `AuthResult` should be a `record`

**File:** `Services/AuthResult.cs`

Immutable DTO with factory methods — the repo rule says these should be `record` types.

---

### i-05 — Design | `AppTheme` enum co-located with `IThemeService`

**File:** `Services/IThemeService.cs:3`

One type per file. `AppTheme` belongs in `Models`.

---

### i-06 — Design | `IGraphService` extends `IDisposable` but `Dispose` never called

**File:** `Services/Graph/IGraphService.cs:5`, `Services/Graph/GraphService.cs:275`

`GraphService.Dispose()` only partially cleans up; with the Service Locator pattern (B-03) it is never called at all. `HttpDownloader._http` and `UploadService._http` ownership chains are undocumented.

---

### i-07 — Design | Namespace-folder mismatch for `IAuthService` / `AuthResult`

**Files:** `Services/IAuthService.cs:1`, `Services/AuthResult.cs:1`

Namespace is `Services.Auth` but files live directly under `Services/`, not `Services/Auth/`.

**Fix:** Move files to `Services/Auth/`.

---

## Summary

| Severity | Count |
|---|---|
| Blocker | 6 |
| Major | 15 |
| Minor | 14 |
| Info | 7 |

**Verdict: Request Changes.**

Three highest-priority concerns before any merge:

1. **B-04 + B-03** — no DI container and Service Locator anti-pattern make every class untestable and also block B-05.
2. **B-05** — zero tests on a production sync engine.
3. **B-01** — hardcoded Client ID must be removed before any public release.

All Blockers and Majors must be resolved. All Minors and Infos should be raised as GitHub issues per the Definition of Done.
