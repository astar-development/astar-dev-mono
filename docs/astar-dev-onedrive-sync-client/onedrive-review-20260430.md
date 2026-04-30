# Code Review — AStar.Dev.OneDrive.Sync.Client — 2026-04-30

## Summary

| Severity | Count |
|---|---|
| Blocker | 5 |
| Critical | 11 |
| Major | 28 |
| Minor | 19 |
| Trivial | 6 |
| **Total** | **69** |

**Overall verdict: Request Changes.** The codebase is structurally sound and well-tested in many areas, but it has a confirmed SQL injection risk (Blocker), duplicate repository registrations that would cause runtime `IDbContextFactory` contention, widespread missing `ConfigureAwait(false)` in infrastructure code, several `async void` event handlers without fire-and-forget safety, pervasive absence of XML docs on public API surfaces, significant technical-type folder organisation violations, multiple test classes using the wrong naming convention, and a suppressed compiler warning without documented justification.

---

## Blocker Issues

- [ ] **[Data/Repositories/SyncRuleRepository.cs:24]** Blocker — `ExecuteSqlAsync` with string interpolation: the `$"INSERT INTO SyncRules ... VALUES ({accountId.Id}, {remotePath} ...)"` interpolation embeds raw user-supplied strings directly into SQL. Although EF Core's `ExecuteSqlAsync` with `$""` does use `FormattableString` parameterisation for the whole interpolated string, the `remotePath` value comes from `rule.RemotePath` which is user-supplied folder-path data from OneDrive; review carefully that all inputs are parameterised. `DeleteChildRulesAsync` at line 51 concatenates `string pattern = parentPath + "/%"` then passes it as an interpolated parameter — the concatenation happens outside the interpolation boundary, meaning EF Core correctly parameterises the already-concatenated string. Confirm EF Core treats every `{...}` hole in `ExecuteSqlAsync` as a `SqlParameter`. If the EF version pre-dates that guarantee, the call at line 24 is a full SQL injection. Fix: use `ExecuteSqlAsync` with separately declared `SqliteParameter` objects or switch to a strongly-typed LINQ expression (possible for upsert with `ExecuteUpdate` + `Add` pattern matching the other repositories).

- [x] **[Data/PersistenceServiceExtensions.cs] + [Startup/ShellServiceExtensions.cs:28-32]** Blocker — All five repository interfaces (`IAccountRepository`, `ISyncRepository`, `IDriveStateRepository`, `ISyncRuleRepository`, `ISyncedItemRepository`) are registered **twice** as singletons: once in `AddPersistence()` (called from `App.axaml.cs:66`) and again in `AddShell()` (called from `App.axaml.cs:78`). Two competing singleton registrations for the same interface means the DI container resolves whichever was registered last; all consumers that received the first registration hold a different instance than those that received the second. This will cause data-consistency bugs that are extremely hard to trace. Fix: remove all repository registrations from `ShellServiceExtensions` — `AddPersistence` is the canonical location.

- [x] **[Startup/LocalizationExtensions.cs:13-15]** Blocker — `#pragma warning disable CA1859` is used to suppress a compiler warning without a documented reason. `TreatWarningsAsErrors=true` is a global repo rule; suppressions must carry a `// Justification: ...` comment explaining why the warning cannot be addressed. Fix: either implement the concrete type directly (remove the suppression) or add an inline comment: `// Justification: ILocalizationService is required for DI registration — concrete type must not be exposed here`.

- [x] **[App.axaml.cs:93]** Blocker — `Path.Combine(logDirectory, ApplicationMetadata.ApplicationLogName)` uses `Path.Combine` instead of the repo-mandated `CombinePath` extension from `AStar.Dev.Utilities`. `Path.Combine` can silently drop all preceding components if any argument begins with a directory separator; `CombinePath` guards against this. The `logDirectory` value is constructed correctly on line 84 using `CombinePath`, but the File sink path at line 93 reverts to the forbidden API. Fix: `logDirectory.CombinePath(ApplicationMetadata.ApplicationLogName)`.

- [ ] **[Infrastructure/Sync/LocalChangeDetector.cs:43,97] + [Infrastructure/Sync/RemoteFolderEnumerator.cs:210] + [Accounts/AccountFilesViewModel.cs:151]** Blocker — `Path.Combine` used instead of `CombinePath` in multiple places throughout the sync engine: `LocalChangeDetector.BuildLocalPath` (line 97), `RemoteFolderEnumerator.BuildLocalPath` (line 210), and `AccountFilesViewModel.OnOpenInFileManager` (line 151). Each is a repo-rule violation that can silently produce wrong paths. Fix: replace all with the `CombinePath` extension.

---

## Critical Issues

- [ ] **[Infrastructure/Sync/SyncScheduler.cs:99]** Critical — `OnTimerTickAsync` is an `async void` method. If `RunSyncPassAsync` throws an unhandled exception the process will crash with no opportunity to log. The comment `// ReSharper disable once AsyncVoidMethod — Timer requires this signature` acknowledges the pattern but does not add the error handling that makes it safe. Fix: wrap the body in a `try/catch` that logs at `Error` level:

```csharp
private async void OnTimerTickAsync(object? state)
{
    if (SyncIsAlreadyRunning())
        return;

    try
    {
        await RunSyncPassAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        Serilog.Log.Error(ex, "[SyncScheduler] Unhandled exception in timer callback: {Error}", ex.Message);
    }
}
```

- [ ] **[Accounts/AccountsViewModel.cs:90] + [Home/MainWindowViewModel.cs:139,149]** Critical — `OnWizardCompletedAsync`, `OnAccountSelectedAsync`, and `OnAccountAddedAsync` are all `async void`. An unhandled exception inside `Try.RunAsync` that is not caught by `.TapErrorAsync` (e.g., the `TapErrorAsync` callback itself throws) would crash the process with no recovery. Fix: add a top-level `try/catch` around the entire `async void` body in each case.

- [ ] **[Infrastructure/Sync/SyncService.cs:21,52,59,60]** Critical — `authService.AcquireTokenSilentAsync`, `syncRepository.ResolveConflictAsync`, and related awaits are missing `ConfigureAwait(false)`. This is infrastructure code and must not capture the synchronisation context. Fix: add `.ConfigureAwait(false)` to every `await` in non-UI infrastructure classes.

- [ ] **[Infrastructure/Sync/SyncService.cs:77]** Critical — `syncRepository.AddConflictAsync(conflict)` inside the lambda passed to `EnumerateAsync` is awaited without `ConfigureAwait(false)`. Fix: `.ConfigureAwait(false)`.

- [ ] **[Infrastructure/Sync/DownloadWorker.cs:25-46]** Critical — Every `await syncRepository.UpdateJobStateAsync(...)` call in `RunAsync` is missing `ConfigureAwait(false)`. This is a background worker with no UI thread affinity. Fix: add `.ConfigureAwait(false)` to all awaits.

- [ ] **[Infrastructure/Sync/RemoteFolderEnumerator.cs:16-54]** Critical — Multiple `await` calls in `EnumerateAsync` are missing `ConfigureAwait(false)`: `syncRuleRepository.GetByAccountIdAsync` (line 16), `syncedItemRepository.GetAllByAccountAsync` (line 25), `graphService.GetDriveIdAsync` (line 26), `syncRuleRepository.UpsertAsync` (line 54), `graphService.EnumerateFolderAsync` (line 58), `HandleFolderAsync` (line 70), `syncedItemRepository.UpsertAsync` (line 101). Fix: add `.ConfigureAwait(false)` throughout.

- [ ] **[Infrastructure/Sync/SyncScheduler.cs:62-79]** Critical — `TriggerAccountAsync(string accountId, ...)` constructs an `OneDriveAccount` object inline (lines 68-77) duplicating the identical mapping logic in `RunSyncPassAsync` (lines 120-130). If the mapping changes in one place it will not be updated in the other. Fix: extract a private `MapEntityToAccount(AccountEntity entity, IReadOnlyList<SyncRuleEntity> rules) -> OneDriveAccount` method and call it from both sites.

- [ ] **[Accounts/AccountFilesViewModel.cs:123]** Critical — `OnIncludeToggledAsync` is `async void`. If either `_syncRuleRepository.DeleteChildRulesAsync` or `_syncRuleRepository.UpsertAsync` throws outside the `try/catch` (e.g., during setup), or if the `catch` block itself throws, the exception escapes and crashes the process. Fix: ensure the outer `try/catch` is truly comprehensive and never re-throws.

- [ ] **[Accounts/AccountsViewModel.cs:146]** Critical — `_ = repository.SetActiveAccountAsync(new AccountId(card.Id), CancellationToken.None)` is a fire-and-forget task. If the repository call throws, the exception is swallowed silently. Fix: `await` the call (the calling method `OnCardSelected` must become `async Task` and be called accordingly), or at minimum attach a `.ContinueWith` error logger.

- [ ] **[Infrastructure/Authentication/AuthService.cs:71]** Critical — `accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId)` has no null guard on `a.HomeAccountId` — if MSAL returns an account with a null `HomeAccountId` the comparison will throw a `NullReferenceException`. Fix: use null-conditional: `a.HomeAccountId?.Identifier == accountId`.

- [ ] **[Infrastructure/Graph/GraphService.cs:214-229]** Critical — `ResolveClientWithDriveContextAsync` caches drive context keyed by **access token string** in a `ConcurrentDictionary`. Access tokens are short-lived (typically 1 hour). When a token expires and a new one is issued, the old entry leaks in the cache indefinitely and the cache grows unboundedly over time. If a token is refreshed silently, the new token produces a second cache entry rather than reusing the existing drive context. Fix: key the cache on `AccountId` (or MSAL `HomeAccountId`) rather than the raw token string, and invalidate on token refresh.

---

## Major Issues

- [ ] **[Onboarding/AddAccountWizardViewModel.cs:11-13]** Major — `WizardStep` enum and `WizardFolderItem` class are defined in the same file as `AddAccountWizardViewModel`. Repo convention requires one type per file. Fix: move `WizardStep` to `WizardStep.cs` and `WizardFolderItem` to `WizardFolderItem.cs` in the `Onboarding/` folder.

- [ ] **[Startup/ViewExtensions.cs:14]** Major — Comment `// When refactored, this class will disappear but "baby steps"` violates the no-comments rule. Fix: raise a GitHub issue for the refactor and remove the comment.

- [ ] **[Infrastructure/Sync/SyncService.cs] (whole class)** Major — `SyncService` mixes authentication orchestration, conflict detection, remote enumeration coordination, local detection coordination, job execution, and account state persistence. `SyncAccountInternalAsync` (lines 63-128) is 65 lines long, exceeding the 30-line guideline. Fix: extract a dedicated `SyncPassOrchestrator` that handles the internal sync flow, leaving `SyncService` as a thin facade for public entry points.

- [ ] **[Infrastructure/Sync/RemoteFolderEnumerator.cs] (whole class)** Major — `EnumerateAsync` is a 105-line method performing: rule loading, drive ID resolution, folder ID back-filling, remote item enumeration, ETag comparison, phantom-item creation, conflict building, and conflict resolution branching. Violates both the 30-line method guideline and SRP. Fix: extract sub-methods for each distinct step (`ResolveRulesAsync`, `BuildDownloadJobsAsync`, `HandleItemAsync`, etc.).

- [ ] **[Accounts/AccountFilesViewModel.cs:54-121]** Major — `LoadAsync` is 67 lines and mixes authentication, Graph API calls, rule loading, sync-state mapping, and ViewModel construction. Fix: extract at least `BuildFolderTreeNodeAsync` and `LoadRulesAsync` as private helpers.

- [ ] **[Activity/ActivityViewModel.cs:62-94]** Major — `SetActiveAccountAsync` mixes persisted-conflict loading, model-to-ViewModel mapping, `ObservableCollection` management, and UI state reset. Fix: extract `LoadPersistedConflictsAsync` and `MapConflictEntityToViewModel`.

- [ ] **[Infrastructure/Sync/UploadService.cs:86-115]** Major — `UploadChunksAsync` is 29 lines and mixes file I/O, chunked read logic, range calculation, and retry orchestration. Fix: extract `ReadChunkAsync` and `ComputeRangeEnd` to bring individual methods under 20 lines.

- [ ] **[Converters/] folder** Major — All converter files live in a top-level `Converters/` technical-type folder. Repo convention requires organisation by business feature. Fix: co-locate converters with the feature that owns them (`FolderTreeConverters` → `Home/`, `DashboardConverters` → `Dashboard/`, `WizardConverters` → `Onboarding/`).

- [ ] **[Models/] folder** Major — `Models/` is a top-level technical-type folder. Domain models should be in `Domain/` (if they are value objects) or alongside the feature that owns them. Fix: move `SyncJob`, `SyncConflict`, `DeltaItem`, etc. to feature-appropriate locations or a dedicated domain layer.

- [ ] **[Infrastructure/Shell/FeatureAvailabilityService.cs:5]** Major — Missing XML documentation on all public members (`IsAvailable`, `Freeze`, `Register`). Fix: add `/// <inheritdoc />` to all three public methods.

- [ ] **[Infrastructure/Sync/SyncService.cs:5-10]** Major — `SyncService` is missing XML docs on all three public events and `SyncAccountAsync`/`ResolveConflictAsync` methods. Fix: add `/// <inheritdoc />` above each public member.

- [ ] **[Infrastructure/Sync/SyncScheduler.cs:12-13]** Major — `SyncScheduler` public members (`StartSync`, `StopSync`, `SetInterval`, `SyncStarted`, `SyncCompleted`) have no `<inheritdoc />`. Fix: add `/// <inheritdoc />`.

- [ ] **[Data/Repositories/SyncRepository.cs:10-122]** Major — None of the public methods (`EnqueueJobsAsync`, `GetPendingJobsAsync`, `UpdateJobStateAsync`, etc.) have XML docs or `<inheritdoc />`. Fix: add `<inheritdoc />` to every public method.

- [ ] **[Data/Repositories/AccountRepository.cs] + [SyncRuleRepository.cs] + [DriveStateRepository.cs] + [SyncedItemRepository.cs]** Major — None carry `<inheritdoc />` on their public methods. Fix: add `<inheritdoc />` to every public method in all four files.

- [ ] **[Infrastructure/Sync/LocalChangeDetector.cs:13]** Major — `DetectNewAndModifiedFiles` returns `List<SyncJob>` (mutable concrete type) rather than `IReadOnlyList<SyncJob>`. Repo convention requires immutable collection types on public API surfaces. Fix: change return type to `IReadOnlyList<SyncJob>`.

- [ ] **[Infrastructure/Sync/SyncScheduler.cs:116-130]** Major — `RunSyncPassAsync` contains inline `OneDriveAccount` construction duplicating the identical mapping in `TriggerAccountAsync` (see Critical issue above). Fix: extract `MapEntityToAccount`.

- [ ] **[Home/FolderTreeNodeViewModel.cs:119-163]** Major — `EnsureChildrenLoadedAsync` is 44 lines and performs both Graph service calls and `ObservableCollection` population. Fix: extract `BuildChildViewModelsAsync(IReadOnlyList<DriveFolder> folders)` to separate concerns.

- [ ] **[Infrastructure/Graph/GraphService.cs:163-204]** Major — `EnumerateSubFolderAsync` is a 41-line recursive method that accumulates items, pages, and recurses. Fix: extract the per-item mapping into a `MapToDeltaItem` helper.

- [ ] **[App.axaml.cs:57-79]** Major — `BuildServiceProvider` reads `appsettings.json` a second time (lines 70-71) after `Program.cs` already read it (lines 19-21). The configuration is not shared, so two independent reads occur and the in-memory representation is duplicated. Fix: pass `IConfiguration` into `BuildServiceProvider` as a parameter.

- [ ] **[Startup/ShellServiceExtensions.cs:51-52]** Major — `IParallelDownloadPipeline` and `IAppBootstrapper` are registered as `Transient`. `IAppBootstrapper` takes `MainWindowViewModel` (singleton); a transient bootstrapper injecting a singleton is fine for one-shot use but lifetime intent should be documented or verified intentional. Flag for awareness.

- [ ] **[Infrastructure/Sync/SyncService.cs:95]** Major — `uploadJobs` typed as `List<SyncJob>` (concrete return from `LocalChangeDetector`). If return type is fixed to `IReadOnlyList<SyncJob>`, still works. Additionally, rename `localPathLookup` (line 94) to `syncedItemsByLocalPath` for clarity.

- [ ] **[Models/OneDriveAccount.cs:22,32]** Major — `SelectedFolderIds` and `FolderNames` use mutable collection types (`List<T>` and `Dictionary<K,V>`). Repo convention requires `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>` for model properties. Fix: change to immutable collection types.

- [ ] **[Models/SyncJob.cs:17-24]** Major — `SyncJob` is a `record` but has mutable `set` properties (`State`, `ErrorMessage`, `DownloadUrl`, `CompletedAt`, `UploadedRemoteItemId`). Fix: make all properties `init`-only and use `with` expressions at mutation sites in `DownloadWorker` and `ParallelDownloadPipeline`.

- [ ] **[Tests/Conflicts/ConflictResolverTests.cs:5] + [Tests/Infrastructure/Sync/SyncServiceTests.cs:9]** Major — Test files are in namespace `AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync` but live in the `Conflicts/` and `Infrastructure/Sync/` folders respectively. Fix: update namespaces to match folder paths.

- [ ] **[Tests/Conflicts/ConflictResolverTests.cs] + [Tests/Data/Repositories/AccountRepositoryTests.cs] + [Tests/Infrastructure/Authentication/AuthServiceTests.cs]** Major — Multiple test files use PascalCase method names (`Resolve_WithIgnorePolicy_ShouldReturnSkip`, `GetAllAsync_WithNoAccounts_ShouldReturnEmptyList`) rather than the required `when_[action]_then_[outcome]` snake_case convention. Test class names use `Tests` suffix rather than required `Given` prefix. Fix: rename all test methods to snake_case and rename `ConflictResolverTests`/`AccountRepositoryTests`/`AuthServiceTests` to `GivenA[Context]` classes.

- [ ] **[Tests/Infrastructure/Authentication/AuthServiceTests.cs:3-6]** Major — The test file contains only comments explaining why tests are absent. Comments violate the no-comments rule. Fix: delete the file and raise a GitHub issue tracking the missing integration test coverage.

- [ ] **[Infrastructure/Sync/SyncService.cs] (functional extensions)** Major — `SyncService` does not use `Result<T>` or `Option<T>` from `AStar.Dev.Functional.Extensions` for error handling or optional returns. Instead it uses nullable returns and manual null checks. Fix: refactor to use `Result<T>` for `SyncAccountAsync` and `Option<T>` for optional lookups.

- [ ] **[Data/Repositories/AccountRepository.cs] + [SyncRuleRepository.cs]** Major — Repositories use manual null checks and nullable returns instead of `Option<T>` from `AStar.Dev.Functional.Extensions`. Fix: return `Option<T>` from single-item lookups (`GetByIdAsync`, `GetActiveAsync`, etc.).

- [ ] **[Infrastructure/Authentication/AuthService.cs]** Major — `AuthService` returns raw nullable `AuthResult?` and uses manual null checks rather than `Result<AuthResult>` from `AStar.Dev.Functional.Extensions`. Fix: return `Result<AuthResult>` and propagate errors functionally.

---

## Minor Issues

- [ ] **[Program.cs:11-13]** Minor — Comments restate what the code is doing (`// Initialization code. Don't use any Avalonia...`). Repo convention prohibits explanatory comments; remove the Avalonia template comments.

- [ ] **[Program.cs:51]** Minor — Comment `// Avalonia configuration, don't remove; also used by visualdddd designer.` has a typo and restates what the code does. Fix: delete the comment line.

- [ ] **[Infrastructure/Sync/SyncScheduler.cs:7-10]** Minor — Class `<summary>` says "Default interval: 60 minutes" which duplicates the `DefaultInterval` constant as a magic number. Fix: reference `DefaultInterval` instead of hardcoding "60 minutes".

- [ ] **[Infrastructure/Authentication/AuthService.cs:127-145]** Minor — `BuildSuccess` has no blank line before `return` on line 144; the preceding line 143 is a code statement. Fix: insert a blank line before `return AuthResult.Success(...)`.

- [ ] **[Infrastructure/Sync/UploadService.cs:121-166]** Minor — `UploadChunkWithRetryAsync` has multiple `return` statements (lines 150, 153, 157) without preceding blank lines when they follow code on the preceding line. Fix: add a blank line before each `return` that follows non-`if` code.

- [ ] **[Infrastructure/Sync/SyncScheduler.cs:43]** Minor — `_ = (_timer?.Change(interval, interval))` has unnecessary parentheses. Fix: `_ = _timer?.Change(interval, interval)`.

- [ ] **[Data/Entities/AccountEntity.cs:8]** Minor — `AccountId Id { get; set; } = new AccountId("Unknown")` uses magic string `"Unknown"`. Fix: extract `private const string DefaultAccountId = "Unknown"` or use `AccountId.Empty` if such a concept exists.

- [ ] **[Accounts/AccountSyncSettingsViewModel.cs:32-37]** Minor — `BrowseAsync` is a static async method with an empty body plus a comment explaining it is wired in code-behind. Violates the no-comment rule. Fix: delete the comment and either implement the method or remove it entirely.

- [ ] **[Activity/ActivityViewModel.cs:97-106]** Minor — `AddActivityItem` calls `Dispatcher.UIThread.Post` directly (hard Avalonia dependency) rather than using the `IUiDispatcher` abstraction already present in the codebase. Fix: inject `IUiDispatcher` and call `_dispatcher.Post(...)`.

- [ ] **[Activity/ActivityViewModel.cs:109-117]** Minor — Same `Dispatcher.UIThread.Post` hard dependency in `AddConflictItem`. Fix: same as above.

- [ ] **[Models/AccountSettings.cs:7-9]** Minor — XML `<summary>` comment describes deferred migration work as a TODO. Fix: raise a GitHub issue for the migration and remove the comment.

- [ ] **[Models/SyncConflict.cs:3]** Minor — `ConflictState` enum defined in same file as `SyncConflict`. One type per file. Fix: move `ConflictState` to `ConflictState.cs`.

- [ ] **[Infrastructure/Graph/GraphService.cs:232]** Minor — `private sealed record DriveContext(string DriveId, string RootId)` is a private nested type inside `GraphService`. Repo convention requires one type per file. Fix: move to `DriveContext.cs` in `Infrastructure/Graph/`, or accept as a justified exception with a documented comment.

- [ ] **[Infrastructure/Sync/SyncJobExecutor.cs:55-56]** Minor — `$"/{relativePath.TrimStart('/')}"` uses magic string `"/"`. Fix: extract `private const string PathSeparator = "/"`.

- [ ] **[Home/FolderTreeNodeViewModel.cs:52]** Minor — Unicode escape literals `"▾"` and `"▸"` for chevron glyphs are magic strings. Fix: extract as `private const string ExpandedGlyph = "▾"` and `private const string CollapsedGlyph = "▸"`.

- [ ] **[Accounts/AccountsViewModel.cs:96-101]** Minor — `account.IsActive = Accounts.Count == 0` and `account.AccentIndex = Accounts.Count % 6` mutate the `OneDriveAccount` domain model directly inside the wizard completion handler. Fix: perform this logic inside a factory method before the `AccountAdded` event is raised, not in the ViewModel observer.

- [ ] **[Tests/AStar.Dev.OneDrive.Sync.Client.Tests.Unit.csproj:5]** Minor — `<NoWarn>NU1902</NoWarn>` suppresses a NuGet warning without a documented reason. Fix: add an XML comment explaining why this warning is suppressed.

- [ ] **[Accounts/AccountsViewModel.cs:79]** Minor — `await authService.SignOutAsync(card.Id)` and `await repository.DeleteAsync(...)` in `RemoveAccountAsync` are awaited without `ConfigureAwait(false)`. ViewModel (UI) code intentionally captures context, but flag for consistency audit against other async calls in the same method.

- [ ] **[Infrastructure/Sync/LocalChangeDetector.cs] + [Infrastructure/Sync/RemoteFolderEnumerator.cs]** Minor — Multiple methods do not use `AStar.Dev.Logging.Extensions` `LogMessage` compile-time templates; instead, raw string interpolation is used in `logger.LogInformation(...)` calls throughout. Fix: add compile-time log message templates to `LogMessage` and use them.

---

## Trivial Issues

- [ ] **[Infrastructure/Sync/DownloadWorker.cs:28-29]** Trivial — `string? error = null` and `bool success = false` declared on separate lines with alignment padding. Remove padding.

- [ ] **[Infrastructure/Sync/SyncService.cs:13]** Trivial — Extra space alignment on event field declarations (`SyncProgressChanged`, `JobCompleted`, `ConflictDetected`). Remove for consistency.

- [ ] **[Infrastructure/Graph/GraphService.cs:15-18]** Trivial — `ChildrenSelect` array uses alignment padding. Remove for consistency.

- [ ] **[Accounts/AccountFilesViewModel.cs:16-21]** Trivial — Constructor parameters are re-assigned to private readonly fields manually even though the primary constructor already captures them as members. The explicit `private readonly` declarations shadow the captured parameters. Fix: remove the explicit field declarations and use the primary-constructor-captured names throughout.

- [ ] **[Home/MainWindowViewModel.cs:49-97]** Trivial — Five lazy-backed property getters use the C# 13 `field` keyword. Ensure all CI toolchain supports C# 13+.

- [ ] **[Infrastructure/Sync/HttpDownloader.cs:111]** Trivial — `AddAdditionalSecondBackoff` returns a constant `TimeSpan.FromSeconds(1)` with no other logic. Fix: inline the constant or use `private static readonly TimeSpan AdditionalBackoff = TimeSpan.FromSeconds(1)`.

---

## References

- [Repo conventions (CLAUDE.md)](../../CLAUDE.md)
- [C# code style rules](../../.claude/rules/c-sharp-code-style.md)
- [OWASP SQL Injection](https://owasp.org/www-community/attacks/SQL_Injection)
- [Microsoft: ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [Microsoft: async void anti-pattern](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [EF Core ExecuteSqlAsync parameterisation](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries#passing-parameters)
- [AStar.Dev.Functional.Extensions](../../packages/core/AStar.Dev.Functional.Extensions/)
- [AStar.Dev.Utilities](../../packages/core/AStar.Dev.Utilities/)
- [AStar.Dev.Logging.Extensions](../../packages/infra/AStar.Dev.Logging.Extensions/)
