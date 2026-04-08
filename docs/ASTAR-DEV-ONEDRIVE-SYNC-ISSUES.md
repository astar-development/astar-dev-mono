# AStar.Dev.OneDrive.Sync.Client — Code Review Issues

> Reviewed: 2026-04-08
> Verdict: **Request Changes**

---

## .csproj / Project Configuration

### `apps/desktop/AStar.Dev.OneDrive.Sync.Client/AStar.Dev.OneDrive.Sync.Client.csproj`

| #   | Line | Severity | Issue                                                                                                          | Fix                 | Done |
| --- | ---- | -------- | -------------------------------------------------------------------------------------------------------------- | ------------------- | ---- |
| 1   | 5    | error    | `<Nullable>enable</Nullable>` declared in project file — already provided by `Directory.Build.props`.          | Remove the element. | yes  |
| 2   | 23   | warning  | XML comment `<!--Condition below is needed...-->` restates what the `Condition` attribute already makes clear. | Remove.             | yes  |
| 3   | 37   | warning  | Section comment `<!-- Dependency Injection & Logging -->` — package names speak for themselves.                | Remove.             | yes  |

### `apps/desktop/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/AStar.Dev.OneDrive.Sync.Client.Tests.Unit.csproj`

| #   | Line  | Severity | Issue                                                                                                                                 | Fix                                                                          | Done        |
| --- | ----- | -------- | ------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- | ----------- |
| 4   | 5     | error    | `<Nullable>enable</Nullable>` — same violation; inherited from `Directory.Build.props`.                                               | Remove.                                                                      | yes         |
| 5   | 6     | error    | `<OutputType>Exe</OutputType>` declared explicitly. If required for `TestingPlatformDotnetTestSupport`, document why.                 | Add a justifying comment or confirm the repo-wide default already sets this. | yes         |
| 6   | 17-19 | warning  | Global usings for `Xunit`, `Shouldly`, `NSubstitute` via `<Using>` items — already configured globally per QA specialist conventions. | Remove those three `<Using>` items.                                          | Not correct |

---

## Services/Sync/SyncService.cs

| #   | Line  | Severity | Issue                                                                                                                 | Fix                                                                                           | Done |
| --- | ----- | -------- | --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ---- |
| 7   | 13    | error    | `LocalChangeDetector _changeDetector = new();` — newed up inside the class; DI and testability violation.             | Inject `ILocalChangeDetector` via the primary constructor.                                    | yes  |
| 8   | 25    | warning  | `SyncProgressChanged` called with literal `"TEST TEST TEST !!!"` — debug leftover.                                    | Remove or replace with meaningful status string.                                              |      |
| 9   | 23    | warning  | Commented-out code `//Dashboard.UpdateAccountSyncState...` — dead code, no-comments rule.                             | Remove entirely; raise a GitHub issue if the feature is needed.                               | yes  |
| 10  | 287   | warning  | `ClassifyJobsAsync` is `static async` but has no actual `await` — unnecessary state machine allocation.               | Remove `async`/`await`; return tuple directly.                                                | yes  |
| 11  | 304   | error    | `new ParallelDownloadPipeline(...)` with hardcoded magic number `8` for worker count — DI violation and magic number. | Inject `ParallelDownloadPipeline` (or factory); extract `private const int WorkerCount = 8;`. | yes  |
| 12  | 325   | error    | `new HttpDownloader()` inside `ApplyConflictOutcomeAsync` — new `HttpClient` on every call; socket exhaustion risk.   | Inject `IHttpDownloader`; reuse the single instance.                                          | yes  |
| 13  | 31-32 | warning  | `return` on line 32 directly follows `RaiseProgress(...)` on line 31 — no blank line.                                 | Add blank line before `return`.                                                               | yes  |
| 14  | 58    | warning  | `return` directly follows the `if` condition on line 57 — no blank line.                                              | Add blank line before `return`.                                                               | yes  |

---

## Services/Sync/HttpDownloader.cs

| #   | Line | Severity | Issue                                                                                                                            | Fix                                                                                               | Done             |
| --- | ---- | -------- | -------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- | ---------------- |
| 15  | 18   | error    | `new HttpClient()` used directly — socket exhaustion risk.                                                                       | Register via `services.AddHttpClient<HttpDownloader>()`; inject `HttpClient` through constructor. | Factory injected |
| 16  | 64   | warning  | `new byte[81920]` buffer allocated on every download call — 8 workers × 80 KB of per-call heap allocations.                      | Use `ArrayPool<byte>.Shared.Rent(81920)` / `Return()`.                                            |                  |
| 17  | 78   | error    | `return;` directly follows `PreserveRemoteTimestamp(...)` — no blank line.                                                       | Add blank line.                                                                                   | yes              |
| 18  | 98   | warning  | `return delta + AddAdditionalSecondBackoff();` directly follows the `if` pattern on line 97 — no blank line.                     | Add blank line.                                                                                   | yes              |
| 19  | 104  | warning  | `return wait + AddAdditionalSecondBackoff();` directly follows the `if` on line 103 — no blank line.                             | Add blank line.                                                                                   | yes              |
| 20  | 121  | warning  | Inline comment `// Exponential backoff with jitter: 2s, 4s, 8s, ...` restates what the method name and constants already convey. | Remove.                                                                                           | yes              |

---

## Services/Sync/UploadService.cs

| #   | Line        | Severity | Issue                                                                                                                                                         | Fix                                                                                  | Done            |
| --- | ----------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | --------------- |
| 21  | 28          | error    | `private readonly HttpClient _http = new();` — socket exhaustion antipattern.                                                                                 | Inject via constructor / `IHttpClientFactory`.                                       | yes             |
| 22  | 22          | warning  | `// 10 MB — must be multiple of 320 KB (327,680 bytes)` restates what the constant name and value convey.                                                     | Remove comment; rename constant to `ChunkSizeBytes` if additional clarity is needed. | yes and renamed |
| 23  | 129         | error    | `chunk.ToArray()` inside `UploadChunkWithRetryAsync` — full byte-array copy on every retry; up to 5 × 10 MB = 50 MB extra allocation per chunk in worst case. | Use `new ReadOnlyMemoryContent(chunk)` directly (available since .NET 5).            |                 |
| 24  | 151/155/161 | warning  | Multiple `return null;` / `return await ...` statements missing blank lines before them.                                                                      | Add blank lines.                                                                     | yes             |
| 25  | 156         | warning  | Typo in method name `GetUpdloadedDocumentId` ("Updloaded").                                                                                                   | Rename to `GetUploadedDocumentId`.                                                   | yes             |

---

## Services/Sync/ParallelDownloadPipeline.cs

| #   | Line | Severity | Issue                                                                                                                              | Fix                                                          | Done |
| --- | ---- | -------- | ---------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ | ---- |
| 26  | 23   | error    | `private readonly HttpDownloader _downloader = new();` — third separate `new HttpClient()` across the codebase; socket exhaustion. | Inject `HttpDownloader` via constructor.                     | yes  |
| 27  | 33   | warning  | `object lockObj = new object();` as a method-local variable — unconventional; harder to reason about under concurrency.            | Use `private readonly object _lock = new();` at field level. | yes  |
| 28  | 95   | warning  | Inline comment `// Always raise completion so UI resets` describes what the code does.                                             | Remove.                                                      | yes  |

---

## Services/Sync/SyncScheduler.cs

| #   | Line  | Severity | Issue                                                                                                                                                                                                | Fix                                                                                     | Done       |
| --- | ----- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- | ---------- |
| 29  | 76-82 | error    | `private async void OnTimerTick(object? state)` — `async void` with no outer `try/catch`. Any exception thrown before the inner `try` in `RunSyncPassAsync` is unhandled and will crash the process. | Wrap the entire body of `OnTimerTick` in `try/catch(Exception ex) { Log.Error(...); }`. | already is |
| 30  | 115   | warning  | `System.Diagnostics.Debug.WriteLine(...)` — should be `Serilog.Log.Error(ex, ...)`.                                                                                                                  | Replace with structured Serilog logging.                                                | yes        |
| 31  | 32    | warning  | `_running = false;` immediately after creating the timer in `Start()` — `_running` is already `false`; this is a no-op that may indicate a copy-paste error.                                         | Remove the redundant assignment.                                                        | yes        |

---

## Services/Graph/GraphService.cs

| #   | Line | Severity   | Issue                                                                                                                                                                   | Fix                                                                       | Done      |
| --- | ---- | ---------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- | --------- |
| 32  | 13   | error      | `Dictionary<string, DriveContext>` keyed by raw access token — tokens expire; cache grows unboundedly with token rotation.                                              | Key by `accountId`/email; apply TTL-based eviction.                       |           |
| 33  | 244  | warning    | `return (graphServiceClient, cached);` directly follows the `if` on line 244 — no blank line.                                                                           | Add blank line.                                                           | Incorrect |
| 34  | 215  | warning    | Magic string `"root:"`.                                                                                                                                                 | Extract to `private const string RootPathMarker = "root:";`.              | yes       |
| 35  | 217  | suggestion | `StringComparison.CurrentCulture` used for URL path `IndexOf` — should be `StringComparison.Ordinal`.                                                                   | Change to `StringComparison.Ordinal`.                                     |           |
| 36  | 268  | suggestion | `StaticAccessTokenProvider.GetAuthorizationTokenAsync` ignores `uri` — any URL receives the token. Intentional (Graph-only client) but should be documented or guarded. | Add an XML doc comment explaining the deliberate host-agnostic behaviour. |           |

---

## Infrastructure/Authentication/AuthService.cs

| #   | Line        | Severity   | Issue                                                                                                                                                                                         | Fix                                                                                  | Done            |
| --- | ----------- | ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | --------------- |
| 37  | 18          | error      | `ClientId` hardcoded as `const` in production source (`"3057f494-687d-4abb-a653-4b8066230b6e"`). `App.cs` reads `ONEDRIVEYNC_AZURE_CLIENT_ID` from the environment — the two are out of sync. | Inject `IOptions<OneDriveClientOptions>` into `AuthService`; use `options.ClientId`. |                 |
| 38  | 55/59/63/67 | warning    | Multiple `return` statements inside `catch` blocks with no blank line before them.                                                                                                            | Add blank lines.                                                                     | no, catch block |
| 39  | 81          | warning    | `return AuthResult.Failure(...)` directly follows the `if(account is null)` guard — no blank line.                                                                                            | Add blank line.                                                                      | no              |
| 40  | 127         | warning    | `return;` directly follows the `if(_cacheRegistered)` guard — no blank line.                                                                                                                  | Add blank line.                                                                      | no              |
| 41  | 119-122     | suggestion | `.ToList()` on a query where return type is `IReadOnlyList<string>` — intent is immutable but the concrete `List<T>` is mutable.                                                              | Use `[..accounts.Select(...)]` or `.ToList().AsReadOnly()`.                          | yes             |

---

## Infrastructure/Authentication/TokenCacheService.cs

| #   | Line  | Severity   | Issue                                                                                                                     | Fix                                                     | Done    |
| --- | ----- | ---------- | ------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------- | ------- |
| 42  | 40-41 | warning    | Inline comments explaining platform-specific MSAL constraint belong in the class XML summary, not inside the method body. | Move reasoning to class-level `<summary>`.              | removed |
| 43  | 59    | suggestion | `TimeSpan.FromSeconds(5)` — magic number.                                                                                 | Extract `private const int KeyringTimeoutSeconds = 5;`. | yes     |

---

## Infrastructure/Shell/SettingsService.cs

| #   | Line  | Severity | Issue                                                                                               | Fix                                                                                                                | Done |
| --- | ----- | -------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ---- |
| 44  | 6-13  | error    | `ISettingsService` and `SettingsService` declared in the same file — one class per file convention. | Move `ISettingsService` to `ISettingsService.cs`.                                                                  | yes  |
| 45  | 30    | error    | `return svc;` directly follows the `if (!File.Exists(...))` condition body — no blank line.         | Add blank line before `return svc;`.                                                                               | no   |
| 46  | 38-39 | warning  | Empty `catch` block silently swallows all deserialization exceptions.                               | Add `Log.Warning(ex, "[SettingsService] Failed to deserialize settings from {Path}; using defaults", svc._path);`. | yes  |

---

## Infrastructure/Shell/StartupService.cs

| #   | Line  | Severity | Issue                                                                                                             | Fix                                                                           | Done    |
| --- | ----- | -------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- | ------- |
| 47  | 7-17  | error    | `IStartupService` and `StartupService` in the same file — one class per file convention.                          | Move interface to `IStartupService.cs`.                                       | yes     |
| 48  | 26-27 | error    | `System.Diagnostics.Debug.WriteLine` outputs MSAL account IDs — PII in debug output.                              | Remove entirely or replace with `Log.Debug` using the `HashedUserId` pattern. | removed |
| 49  | 29-30 | error    | `Debug.WriteLine` outputs `entity.Email` — PII.                                                                   | Remove.                                                                       | removed |
| 50  | 23-25 | warning  | Inline comment `// Only restore accounts that still have a valid cached MSAL token` — self-evident from the code. | Remove.                                                                       | removed |

---

## Localization/LocalizationService.cs

| #   | Line | Severity | Issue                                                                                                           | Fix                                                                                           | Done |
| --- | ---- | -------- | --------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- | ---- |
| 51  | 64   | error    | `LoadAsync(target).GetAwaiter().GetResult()` — blocking async; will deadlock on the Avalonia UI thread.         | Remove `Initialise()` or guard that it is never called from a synchronisation-context thread. | yes  |
| 52  | 74   | warning  | `return string.Format(...)` directly follows the `string template = GetLocal(key);` assignment — no blank line. | Add blank line.                                                                               | yes     |
| 53  | 78   | warning  | `return template;` directly follows the `catch(FormatException)` opening brace — no blank line.                 | Add blank line.                                                                               | yes  |
| 54  | 85   | warning  | `return;` directly follows the `if` on line 84 — no blank line.                                                 | Add blank line.                                                                               | yes  |
| 55  | 141  | warning  | `return [];` directly follows `catch(JsonException)` opening brace — no blank line.                             | Add blank line.                                                                               | yes  |

---

## Data/Repositories/AccountRepository.cs

| #   | Line  | Severity   | Issue                                                                                                        | Fix                                             | Done |
| --- | ----- | ---------- | ------------------------------------------------------------------------------------------------------------ | ----------------------------------------------- | ---- |
| 56  | 31-32 | warning    | Inline comment `// Update scalar properties` — describes what the code does.                                 | Remove.                                         | yes  |
| 57  | 34-35 | warning    | Inline comment `// Sync folder collection — remove deleted, add new` — same.                                 | Remove.                                         | yes  |
| 58  | 55-57 | warning    | Inline comment `// Clear all active flags then set the requested one` — same.                                | Remove.                                         | yes   |
| 59  | 65    | warning    | `async Task UpdateDeltaLinkAsync(...)` is expression-bodied with a single `await` — redundant state machine. | Remove `async`; return the `Task` directly.     | ?     |
| 60  | 51    | suggestion | Same state machine overhead in `DeleteAsync`.                                                                | Return `Task` directly without `async`/`await`. | ?    |

---

## App.axaml.cs

| #   | Line    | Severity | Issue                                                                                                                                                                                      | Fix                                                                                                   | Done |
| --- | ------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------- | ---- |
| 61  | 26      | error    | `private ServiceProvider _services = null!;` — `null!` suppression without a documented reason.                                                                                            | Add `// Assigned in OnFrameworkInitializationCompleted before any use.` or restructure.               |      |
| 62  | 29-35   | error    | Six `public static` mutable service locator properties (`Localisation`, `Theme`, `Auth`, etc.) — SOLID / DI violation; makes all consumers untestable.                                     | Pass dependencies through constructor injection; remove static service locator. Raise a GitHub issue. |      |
| 63  | 124-130 | error    | `AuthService`, `TokenCacheService`, `GraphService`, `SyncService`, `SyncScheduler`, `StartupService` are newed up manually in `BootstrapAsync` instead of being resolved from `_services`. | Register all services in `BuildServiceProvider`; resolve via `_services.GetRequiredService<T>()`.     |      |
| 64  | 50      | warning  | `async` lambda subscribed to `Opened` event — unhandled exceptions in `BootstrapAsync` are swallowed (fire-and-forget).                                                                    | Wrap in `try/catch` inside the lambda.                                                                |      |
| 65  | 81      | warning  | Typo in env-var name: `ONEDRIVEYNC_AZURE_CLIENT_ID` (missing `S`; should be `ONEDRIVESYNC_AZURE_CLIENT_ID`).                                                                               | Fix the typo.                                                                                         |      |

---

## Dashboard/DashboardViewModel.cs

| #   | Line | Severity | Issue                                                                                                               | Fix                                                    | Done |
| --- | ---- | -------- | ------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ | ---- |
| 66  | 48   | error    | `new DashboardAccountViewModel(account, scheduler, App.AccountRepository)` — calls static service locator directly. | Inject `IAccountRepository` into `DashboardViewModel`. |      |

---

## Home/MainWindowViewModel.cs

| #   | Line    | Severity | Issue                                                                                                                                                                            | Fix                                                                                        | Done |
| --- | ------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | ---- |
| 67  | 120-128 | error    | `Accounts`, `Files`, `Activity`, `Dashboard`, `Settings`, `StatusBar` instantiated via `new(...)` calling `App.AccountRepository` and `App.Theme` — service locator antipattern. | Inject composed child ViewModels or use a ViewModel factory.                               |      |
| 68  | 24-33   | warning  | Primary constructor has 8 parameters — exceeds recommended maximum of 5.                                                                                                         | Introduce a parameter object (e.g., `SyncClientContext`) grouping the common dependencies. |      |
| 69  | 209/217 | warning  | `async void OnAccountSelected` and `async void OnAccountAdded` — Avalonia event handler exception; both have no `try/catch`.                                                     | Wrap bodies in `try/catch(Exception ex) { Log.Error(ex, ...); }`.                          |      |

---

## Home/MainWindow.axaml.cs

| #   | Line  | Severity | Issue                                                                                                           | Fix                                                                               | Done |
| --- | ----- | -------- | --------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- | ---- |
| 70  | 22-29 | error    | `InitialiseAsync` has 8 parameters; `MainWindowViewModel` is newed up inside `MainWindow` rather than injected. | Inject `MainWindowViewModel` directly; remove the parameter-heavy factory method. |      |

---

## Infrastructure/ViewModelExtensions.cs

| #   | Line  | Severity | Issue                                                                                                                                                                                                                                                                                                                                   | Fix                                                     | Done |
| --- | ----- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------- | ---- |
| 71  | 40/42 | error    | `SettingsViewModel` registered twice — second registration silently overwrites the first; runtime bug.                                                                                                                                                                                                                                  | Remove the duplicate on line 42.                        |      |
| 72  | 20    | warning  | Comment `// When refactored, this class will disappear but "baby steps"` — no-comments rule; no issue reference.                                                                                                                                                                                                                        | Remove comment; raise a GitHub issue.                   |      |
| 73  | 28-39 | error    | `AccountCardViewModel`, `AccountFilesViewModel`, `AccountSyncSettingsViewModel`, `ActivityItemViewModel`, `ConflictItemViewModel`, `DashboardAccountViewModel`, `FolderTreeNodeViewModel` registered as `Singleton` — per-account/per-item ViewModels with account-specific state; Singleton means every caller gets the same instance. | Register as `Transient` per Avalonia DI lifetime rules. |      |

---

## Services/Sync/LocalChangeDetector.cs

| #   | Line  | Severity   | Issue                                                                                                                                                                                 | Fix                                                                                                                                                  | Done |
| --- | ----- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- | ---- |
| 74  | 21-23 | error      | `#pragma warning disable CA1822` — suppression without a documented reason.                                                                                                           | Either make `DetectChanges` static (preferred) or add inline justification: `// Kept as instance method for future ILocalChangeDetector extraction`. |      |
| 75  | 51    | warning    | `Directory.EnumerateFiles(localDir, "", SearchOption.AllDirectories)` — empty string pattern matches nothing on some platforms; intended wildcard is `"*"`.                           | Change to `"*"`.                                                                                                                                     |      |
| 76  | 68    | suggestion | `RemoteItemId = string.Empty` with comment `// unknown until upload completes` — acceptable (non-obvious reason), but consider a named constant `UnknownRemoteItemId = string.Empty`. | Minor suggestion only.                                                                                                                               |      |

---

## LogViewer/InMemoryLogSink.cs

| #   | Line  | Severity | Issue                                                                                                 | Fix                                                               | Done |
| --- | ----- | -------- | ----------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- | ---- |
| 77  | 71-74 | warning  | Extra blank line inside trivially short `if` body in `ExtractAccountId` — inconsistent formatting.    | Remove the extra blank line.                                      |      |
| 78  | 27    | warning  | `private InMemoryLogSink(int capacity)` is `private` but XML doc says "Exposed internal for testing". | Change to `internal` or expose a static factory method for tests. |      |

---

## Test Project — Convention Issues

| #   | File:Line                           | Severity | Issue                                                                                                                                                                                              | Fix                                                                                                     | Done |
| --- | ----------------------------------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- | ---- |
| 79  | `SyncServiceTests.cs:11`            | warning  | Test class is `public class` not `public sealed class`; not prefixed with `Given`.                                                                                                                 | `public sealed class GivenASyncService`.                                                                |      |
| 80  | `SyncServiceTests.cs:13`            | warning  | Method name `Constructor_ShouldInitializeWithDependencies` — must be `when_[action]_then_[outcome]` snake_case.                                                                                    | `when_constructed_then_dependencies_are_set`.                                                           |      |
| 81  | `SyncSchedulerTests.cs:8`           | warning  | Same class naming and sealing violations.                                                                                                                                                          | `public sealed class GivenASyncScheduler`.                                                              |      |
| 82  | `SyncSchedulerTests.cs:9`           | warning  | All test method names are PascalCase instead of snake_case.                                                                                                                                        | Rename all to `when_..._then_...`.                                                                      |      |
| 83  | `ConvertersTests.cs:8,126`          | warning  | Multiple test classes in one file; not sealed; no `Given` prefix.                                                                                                                                  | Split into separate files, seal, rename.                                                                |      |
| 84  | `App_should.cs:9`                   | error    | `DoSomething` is a meaningless test name. Assertion on `App.Localisation` will always fail because it is assigned asynchronously in `BootstrapAsync`, not in `OnFrameworkInitializationCompleted`. | Remove or replace with a meaningful integration test that correctly exercises the async bootstrap path. |      |
| 85  | `AuthServiceTests.cs:1-8`           | warning  | File contains only a comment block — violates the no-comments rule in test files and provides zero coverage.                                                                                       | Either add real tests or delete the file; track in a GitHub issue.                                      |      |
| 86  | `SyncServiceTests.cs:204-225`       | warning  | `SyncAccountAsync_WithMultipleFolders_ShouldSyncAll` has no assertion — placeholder comment with no linked issue.                                                                                  | Add the assertion or mark `[Skip("issue #N")]` with a linked issue.                                     |      |
| 87  | `SyncServiceTests.cs:11-269`        | warning  | All test methods duplicate the same four `Substitute.For<>()` setup lines — no builder or shared fixture pattern.                                                                                  | Extract a shared builder or per-class field initialisation.                                             |      |
| 88  | `SyncSchedulerTests.cs:17,26,33,44` | warning  | Multiple tests assert only `scheduler.ShouldNotBeNull()` — pass by construction; assert nothing meaningful.                                                                                        | Add meaningful behavioural assertions.                                                                  |      |

---

## Design / Architecture (Cross-Cutting)

| #   | Severity   | Issue                                                                                                                                                                                                              |
| --- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --- |
| 89  | error      | `ViewModels/` folder is a technical-type folder. Repo convention mandates feature-based organisation. All ViewModels should live alongside the feature they belong to.                                             |     |
| 90  | warning    | `Models/` folder is a technical-type folder — `OneDriveAccount`, `SyncJob`, `SyncConflict`, `DeltaItem` etc. should be co-located with their owning feature.                                                       |     |
| 91  | warning    | `Converters/` folder is a technical-type folder — converters should live next to the view/feature that uses them.                                                                                                  |     |
| 92  | suggestion | `AuthResult` is a hand-rolled discriminated union (Success/Failure/Cancelled). The repo has `AStar.Dev.Functional.Extensions` with `Result<T>`. Consider replacing `AuthResult` with `Result<AuthenticationData>`. |     |
| 93  | suggestion | `OneDriveAccount` uses `List<string>` and `Dictionary<string, string>` — mutable collections at public API. Prefer `IReadOnlyList<string>` and `IReadOnlyDictionary<string, string>`.                              |     |
| 94  | suggestion | `SyncJob` is a `sealed record` with mutable `set` properties — undermines `record` immutability intent. Prefer `init` or convert to `class`.                                                                       |     |
| 95  | warning    | No `CancellationToken` propagated in any `AccountRepository` methods (`GetAllAsync`, `GetByIdAsync`, `UpsertAsync`, `DeleteAsync`, `SetActiveAccountAsync`) — all underlying EF Core async calls accept a token.   |     |

---

## Summary

| Severity   | Count  |
| ---------- | ------ |
| error      | 31     |
| warning    | 39     |
| suggestion | 6      |
| **Total**  | **76** |

---

## Overall Verdict: Request Changes 🔴

The project shows solid intent in places (channel-based download pipeline, structured logging setup, `IAsyncDisposable`, `[GeneratedRegex]`), but has a cluster of blocking issues that must be resolved before approval:

1. **Static service locator (`App.*`)** — untestable, SOLID violation, used in 4+ classes
2. **Three separate `new HttpClient()` allocations** — socket exhaustion in production
3. **Hardcoded `ClientId` in `AuthService`** diverges from the env-var read in `App.cs` — latent inconsistency bug
4. **Blocking async** (`GetAwaiter().GetResult()`) in `LocalizationService.Initialise` — potential UI-thread deadlock
5. **`#pragma` suppression** without documented reason
6. **Widespread blank-line-before-`return` violations** across 8+ files
7. **Per-account/item ViewModels registered as `Singleton`** — runtime state sharing bug
8. **Duplicate `SettingsViewModel` DI registration** — second registration silently overwrites the first
9. **PII (email addresses, MSAL account IDs) written to `Debug.WriteLine`** in `StartupService`
10. **Test class naming, sealing, and `Given`-prefix violations** throughout the test project
