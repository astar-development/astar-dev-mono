# Code Review Report

**Project:** `apps/desktop/AStar.Dev.OneDrive.Sync.Client`
**Scope:** `Infrastructure/` (all files and subfolders)
**Date:** 2026-04-26
**Reviewer:** c-sharp-reviewer subagent

---

## Summary

| Severity   | Count  |
| ---------- | ------ |
| Error      | 12     |
| Warning    | 47     |
| Suggestion | 9      |
| **Total**  | **68** |

**Verdict: Request Changes**

---

## Errors

### `Infrastructure/OneDrive/OneDriveClientOptions.cs:1`

`#nullable enable` directive present. Repo uses `Directory.Build.props` to enable nullable globally. Per-file directives must not appear.
**Fix:** Remove `#nullable enable`. Done

### `Infrastructure/Graph/GraphService.cs:12`

`private const string RootPathMarker = "root:";` declared but never referenced. The magic string `"root:"` is used inline at line 120: `Items[$"root:{remotePath}"]`.
**Fix:** Delete the dead constant or use it: `Items[$"{RootPathMarker}{remotePath}"]`. Done.

### `Infrastructure/Graph/GraphService.cs:21`

`private readonly Dictionary<string, DriveContext> _cache = [];` — plain `Dictionary`, not thread-safe. `GraphService` is a singleton; concurrent account syncs produce data races.
**Fix:** Use `ConcurrentDictionary<string, DriveContext>`. Done

### `Infrastructure/Sync/SyncScheduler.cs:51,99`

`if(_runningFlag == 1)` reads a non-volatile `int` without `Interlocked.Read`. JIT/CPU may cache a stale value — data race.
**Fix:** Declare `private volatile int _runningFlag;`. Added Interlocked.Read

### `Infrastructure/Sync/SyncService.cs:40–44`

`catch(Exception ex)` swallows `OperationCanceledException`. Cancellation raises `SyncState.Error` to the UI — incorrect semantics.
**Fix:** Done.

```csharp
catch (OperationCanceledException)
{
    RaiseProgress(account.Id.Id, 0, 0, "Sync cancelled", SyncState.Idle);
}
catch (Exception ex)
{
    Serilog.Log.Error(ex, "...");
    RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
}
```

### `Infrastructure/Sync/UploadService.cs:127`

`chunk.ToArray()` inside `UploadChunkWithRetryAsync` allocates a new `byte[]` on every retry. For a 10 MB chunk with 5 retries = 50 MB of unnecessary allocations.
**Fix:** Done.

```csharp
var array = MemoryMarshal.TryGetArray(chunk, out var segment) ? segment : new ArraySegment<byte>(chunk.ToArray());
using var content = new ByteArrayContent(array.Array!, array.Offset, array.Count);
```

### `Infrastructure/OneDrive/OneDriveClientServiceExtensions.cs`

`AddOneDriveClient` registers `IPublicClientApplication` into DI. `AuthService` ignores it and builds its own private instance in its constructor. Result: two separate MSAL token caches — accounts authenticated in one are unknown to the other.
**Fix:** Either remove the MSAL registration from `AddOneDriveClient` (since `AuthService` manages its own), or inject `IPublicClientApplication` from DI into `AuthService` and remove its internal build.

### `Infrastructure/Theme/IThemeService.cs:3`

Two types defined in one file: `public enum AppTheme` and `public interface IThemeService`. Violates one-type-per-file rule.
**Fix:** Move `AppTheme` to its own `AppTheme.cs`. Done

### Logging — all Infrastructure files

37 log call sites use `Serilog.Log` static global with string-literal templates. `AStar.Dev.Logging.Extensions` (`LogMessage` compile-time templates) is never used. `ILogger<T>` is never injected into any service class. Per `CLAUDE.md`, compile-time log templates are mandatory wherever possible.
**Fix:** Inject `ILogger<T>` into each service; create `LogMessage` entries in `AStar.Dev.Logging.Extensions` for each template; remove all direct `Serilog.Log.*` calls from service classes.

### Functional Extensions — all Infrastructure files

`AStar.Dev.Functional.Extensions` is listed as a dependency but `Result<T>`, `Option<T>`, `Bind`, `Map`, `Match` are used nowhere. Methods like `SyncAccountAsync`, `AcquireTokenSilentAsync`, and `GetDownloadUrlAsync` return bare `Task<T>` with null/error-string patterns — textbook `Result<T>`/`Option<T>` candidates. Per `CLAUDE.md`: "ALWAYS use when practicable."

### `Infrastructure/Sync/SyncService.cs:11`

Constructor has 10 parameters — violates the 5-parameter maximum. The class handles authentication, drive state, rule evaluation, folder enumeration, conflict detection, local file scanning, download orchestration, upload detection, remote deletion detection, and persistence — violates SRP.
**Fix:** Extract focused collaborators: `RemoteDeletionDetector`, `ConflictHandler`, `SyncedItemUpdater`.

### `Infrastructure/Sync/SyncService.cs:60`

`SyncAccountInternalAsync` is 151 lines. Repo convention: ideally under 20 lines. Mixes folder enumeration, ETag comparison, conflict building, phantom-item handling, job list construction, and account update.
**Fix:** Extract each concern into a private method.

---

## Warnings

### `Infrastructure/ApplicationConfiguration/EntraIdConfiguration.cs:7`

`Scopes` declared as `string[]` — mutable array in a record violates immutability preference.
**Fix:** `IReadOnlyList<string> Scopes { get; init; }`. Done.

### `Infrastructure/ApplicationConfiguration/EntraIdConfiguration.cs:3`

No `EntraIdConfigurationFactory` static factory class with a `Create` method. Validation (null/empty `ClientId`) has no home.

### `Infrastructure/Authentication/AuthResult.cs:7`

`sealed class` with private-init properties — use case fits `record`. Loses structural equality, `ToString`, and deconstruct for free.
**Fix:** Convert to `public sealed record AuthResult`; move factory methods to `AuthResultFactory`. Done

### `Infrastructure/Authentication/AuthResult.cs:20–31`

`Success`, `Cancelled`, and `Failure` factory methods take `string` parameters with no null guards at the public boundary.
**Fix:** Add `ArgumentException.ThrowIfNullOrWhiteSpace` / `ArgumentNullException.ThrowIfNull` in each method. Done.

### `Infrastructure/Authentication/AuthService.cs:24–26`

Magic strings `"AStar.Dev.OneDrive.Sync"` and `"1.0.0"` hard-coded in `.WithClientName` / `.WithClientVersion`. `ApplicationMetadata.ApplicationName` and the assembly version are already available. Done.

### `Infrastructure/Authentication/AuthService.cs:29`

`_cacheRegistered` bool read/written in a singleton with no synchronisation. Two concurrent calls before cache registration completes will both see `false` and call `RegisterAsync` twice.
**Fix:** Use `SemaphoreSlim(1,1)` with double-check pattern.

### `Infrastructure/Authentication/AuthService.cs:45`

Magic strings `"authentication_canceled"` and `"user_canceled"` used as bare literals in `when` clause. Extract as `private const`.

### `Infrastructure/Authentication/AuthService.cs:55,59`

Error messages use `$"Authentication failed: {ex.Message}"` — leaks internal detail to UI. Log the full exception separately; surface a fixed user-facing message.

### `Infrastructure/Authentication/AuthService.cs:70,100`

Single-letter lambda parameter `a`. Use `account`. Done

### `Infrastructure/Authentication/AuthService.cs:110–113`

Missing blank line before `return accounts`. Done
**Fix:**

```csharp
var accounts = await _app.GetAccountsAsync();

return accounts.Select(account => account.HomeAccountId.Identifier).ToList().AsReadOnly();
```

### `ConfigureAwait(false)` — all Infrastructure async methods

Absent on virtually every `await` across `AuthService`, `GraphService`, `SyncService`, `DownloadWorker`, `HttpDownloader`, `UploadService`, `ParallelDownloadPipeline`. These are service-layer classes with no UI-thread requirement.

### `Infrastructure/Authentication/TokenCacheService.cs:69,78`

Two inline comments inside method body. Extract the two blocks into private methods `RegisterLinuxFallbackCacheAsync` and `RegisterNonLinuxCacheAsync`.

### `Infrastructure/Authentication/TokenCacheService.cs:94–106`

Nested ternary for platform detection. A chain of `if (OperatingSystem.Is*())` guard clauses is clearer.

### `Infrastructure/Graph/GraphService.cs:28` and `IGraphService`

Return types use `List<T>` and `Task<List<...>>` — mutable. Interface methods should return `IReadOnlyList<T>`.

### `Infrastructure/Graph/GraphService.cs:42,75`

Single-letter lambda parameter `i`. Use `item` or `driveItem`.

### `Infrastructure/Graph/GraphService.cs:163`

`EnumerateSubFolderAsync` is recursive. Deep OneDrive folder trees risk `StackOverflowException`. Document depth limitation or implement iterative BFS.

### `Infrastructure/Graph/GraphService.cs:213`

`BuildClient(accessToken)` called before checking cache — builds a new `GraphServiceClient` even on cache hit. Move after the cache check.

### `Infrastructure/OneDrive/OneDriveClientOptionsFactory.cs:12`

Missing blank line before `return new OneDriveClientOptions { ... }`.

### `Infrastructure/Shell/AppSettings.cs:9`

Inline `//` comment inside class body (`// Account-specific settings...`). Replace with `<remarks>` XML doc or delete.

### `Infrastructure/Shell/FeatureAvailabilityService.cs:15–20`

`throw` on same line as `if`. Place on its own line.

### `Infrastructure/Shell/FeatureAvailabilityService.cs` and `IFeatureAvailabilityService`

No XML documentation on any public member.

### `Infrastructure/Shell/SettingsService.cs:2–3`

Missing blank line between `using` directives and `namespace` declaration.

### `Infrastructure/Shell/SettingsService.cs:19`

Static async factory `LoadAsync()` on the concrete class. Bypasses DI container. Callers must manually call it and register the result. Document the intentional static factory pattern explicitly, or refactor to `IHostedService` / `Lazy<Task<T>>`.

### `Infrastructure/Shell/SettingsService.cs:7,13`

`_jsonOpts`, `_path` — underscore prefix violates camelCase-no-underscore rule.
**Fix:** Rename to `jsonOpts`, `path`.

### `Infrastructure/Shell/ApplicationInitializer.cs:43`

`catch(Exception ex)` catches `OperationCanceledException` and logs it as `Fatal` before re-throwing. Normal cancellation should not produce a `Fatal` log entry.
**Fix:** Add `catch (OperationCanceledException) { throw; }` before the general handler.

### `Infrastructure/Sync/DownloadWorker.cs:15`

`Action<SyncJob, bool, string?> onJobComplete` raw delegate — hard to evolve. Use `JobCompletedEventArgs` already present in the codebase.

### `Infrastructure/Sync/DownloadWorker.cs:79`

`File.Delete(job.LocalPath)` with no guard for null/empty `LocalPath`.

### `Infrastructure/Sync/HttpDownloader.cs:62,64`

Magic number `81920` appears twice. Define as `private const int BufferSize = 81920;`.

### `Infrastructure/Sync/HttpDownloader.cs:99`

`return` on same line as `if`. Rewrite:

```csharp
if (response.Headers.RetryAfter?.Date is not { } date)
    return GetBackoffDelay(attempt);
```

### `Infrastructure/Sync/LocalChangeDetector.cs:43`

Building cross-platform relative path with string interpolation. Prefer `AStar.Dev.Utilities` path helpers where applicable.

### `Infrastructure/Sync/LocalChangeDetector.cs:75`

`dirInfo.Name.StartsWith('.')` — no `StringComparison`. Use `StartsWith('.', StringComparison.Ordinal)`.

### `Infrastructure/Sync/LocalChangeDetector.cs:17`

Single-letter lambda parameter `r`. Use `rule`.

### `Infrastructure/Sync/ParallelDownloadPipeline.cs:64`

`new DownloadWorker(...)` inside `ParallelDownloadPipeline` — newing up a service. Violates DI convention.
**Fix:** Introduce `IDownloadWorkerFactory` or `Func<int, IDownloadWorker>` via DI.

### `Infrastructure/Sync/ParallelDownloadPipeline.cs:25`

`RunAsync` has 8 parameters — exceeds 5-parameter maximum.
**Fix:** Introduce `PipelineRunOptions` record.

### `Infrastructure/Sync/ParallelDownloadPipeline.cs:88`

`catch(Exception ex)` swallows `OperationCanceledException` from workers. Cancellation contract silently broken.

### `Infrastructure/Sync/SyncScheduler.cs:62–77`

Mapping from `AccountEntity` to `OneDriveAccount` duplicated verbatim in `TriggerAccountAsync` and `RunSyncPassAsync`.
**Fix:** Extract `private static OneDriveAccount ToOneDriveAccount(AccountEntity entity)`.

### `Infrastructure/Sync/SyncScheduler.cs:73,122`

Single-letter lambda parameter `f`. Use `folder`.

### `Infrastructure/Sync/SyncScheduler.cs:102`

`RunSyncPassAsync` passes `CancellationToken.None` to `accountRepository.GetAllAsync`, ignoring the `ct` parameter. Manually-triggered syncs cannot be cancelled at the repository stage.

### `Infrastructure/Sync/SyncService.cs:38,56`

Null-forgiving operator `authResult.AccessToken!` used where `AccessToken` can be `null` on `AuthResult.Success` (property is `string?`). Potential null-dereference.
**Fix:** Strengthen `AuthResult.Success` to guarantee non-null `AccessToken`, or guard explicitly.

### `Infrastructure/Sync/SyncService.cs:85–88`

Complex unnamed LINQ expression for root include rules embedded inline.
**Fix:** Extract `private static IEnumerable<SyncRuleEntity> FindRootIncludeRules(IReadOnlyList<SyncRuleEntity> rules)`.

### `Infrastructure/Sync/SyncService.cs:207`

`account.LastSyncedAt = DateTimeOffset.UtcNow;` mutates the method parameter as a side-effect. Mutation is silently lost in callers that rebuild `OneDriveAccount` each pass. Remove this line; persistence is handled by `accountRepository.UpsertAsync`.

### `Infrastructure/Sync/SyncService.cs:330–350`

`foreach(var (remoteId, knownItem) in syncedItems)` iterates without `.ToList()`. If any callee modifies `syncedItems` during iteration, `InvalidOperationException` is thrown. Apply `.ToList()` consistently (as done in `DetectRemoteDeletionsAsync`).

### `Infrastructure/Sync/UploadService.cs:50`

Method named `CreateSessionWithRetryAsync` has no retry loop. Single call, no catch.
**Fix:** Rename to `CreateSessionAsync` or implement retry consistent with `UploadChunkWithRetryAsync`.

### `Infrastructure/Sync/UploadService.cs:129`

`content.Headers.Add("Content-Length", chunk.Length.ToString(CultureInfo.CurrentCulture))` — `Content-Length` is a restricted header (throws `InvalidOperationException` on some runtimes); `CurrentCulture` is wrong for HTTP headers.
**Fix:** Use `content.Headers.ContentLength = chunk.Length;`.

### `Infrastructure/Sync/UploadService.cs:62–69`

Magic strings `"@microsoft.graph.conflictBehavior"`, `"replace"`, `"name"`, `"fileSystemInfo"` in `AdditionalData`. Extract as constants.

### Private field naming — multiple files

Underscore prefix violates camelCase-no-underscore rule. Affected fields:

- `AuthService`: `_cacheRegistered`, `_cacheService`
- `SyncScheduler`: `_timer`, `_interval`, `_runningFlag`
- `SyncEventAggregator`: `_dispatcher`
- `ThemeService`: `_lightUri`, `_darkUri`, `_systemWatcher`
- `SettingsService`: `_jsonOpts`, `_path`

---

## Suggestions

### `Infrastructure/Sync/SyncEventAggregator.cs`

Subscribes to events in constructor but never unsubscribes. If the service is replaced or disposed, event subscriptions keep it alive (event listener leak).
**Fix:** Implement `IDisposable` and unsubscribe in `Dispose()`.

### `Infrastructure/Authentication/TokenCacheService.cs`

XML `<summary>` opening sentence restates what the code already expresses. The platform-path table has documentation value; the generic preamble does not. Trim the redundant prose.

### `Infrastructure/Sync/SyncRuleEvaluator.cs`

`IsIncluded` is O(n × m) — every item checked against every rule linearly. For large file counts with many rules this may become a bottleneck. Consider pre-sorting rules by path length descending or building a trie.

### `Infrastructure/Graph/GraphService.cs:163`

`EnumerateSubFolderAsync` is recursive — risks `StackOverflowException` on deep folder hierarchies. Document depth limitation or convert to iterative BFS.

### `Infrastructure/Theme/ThemeService.cs:92–95`

Private `Disposable` nested class is a general-purpose action-disposable. Re-implementation of utility that belongs in `AStar.Dev.Utilities`. If absent there, add it.

### `Infrastructure/ViewModelBase.cs`

Placed at `Infrastructure/` root — a technical-type folder. `ViewModelBase` is a cross-cutting base; move to `Infrastructure/Shell/` or a dedicated `Common/` folder.

### `Infrastructure/ViewModelBase.cs`

Class body is empty with no XML documentation. If it exists only to alias `ObservableObject`, document the intent.

### `Infrastructure/Sync/SyncScheduler.cs:96`

`async void` timer callback with ReSharper suppression comment. Consider restructuring to a fire-and-forget wrapper to avoid `async void`:

```csharp
private void OnTimerTickAsync(object? state)
    => _ = RunSyncPassAsync(CancellationToken.None);
```

### `Infrastructure/Sync/ParallelDownloadPipeline.cs`

Channel/worker concurrency pattern (backpressure, progress reporting) tested only indirectly via `SyncService` tests. Dedicated unit tests for the pipeline behaviour are absent.

---

## Missing Test Coverage (Note Only)

| Class                        | Gap                                                                                                                       |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `LocalChangeDetector`        | No dedicated test class; hidden files, temp extensions, access-denied, and 5-second modification window untested directly |
| `ParallelDownloadPipeline`   | No dedicated test class; channel/worker concurrency pattern tested only indirectly                                        |
| `UploadService`              | No dedicated test class; chunk retry, session creation, and 429 handling untested                                         |
| `FeatureAvailabilityService` | No dedicated test class                                                                                                   |
| `TokenCacheService`          | Linux keyring timeout and plaintext fallback paths not exercised by existing tests                                        |
