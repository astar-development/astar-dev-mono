# Code Review — `AStar.Dev.OneDrive.Sync.Client`

**Date:** 2026-06-20
**Reviewer:** Claude Code (c-sharp-reviewer)
**Scope:** SRP / SOC violations, SOLID, race conditions, performance, null-safety, design
**Branch at review:** `bug/633-surface-upload-failures-to-ui`

---

## References

- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [SOLID Principles](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp)
- [Thread safety — Dictionary\<K,V\>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2#thread-safety)
- [OWASP — Sensitive Data Exposure](https://owasp.org/www-project-top-ten/2017/A3_2017-Sensitive_Data_Exposure)
- [AStar.Dev repo guidelines — CLAUDE.md](../CLAUDE.md)
- [AStar.Dev C# code style](../.claude/rules/c-sharp-code-style.md)
- [Clean Architecture — dependency rule](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [async void anti-pattern](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)

---

## Findings

### 🔴 Errors

---

#### E1 — `Infrastructure/Sync/Pipeline/SyncPassOrchestrator.cs:85` + `Infrastructure/Sync/Pipeline/SyncJobExecutor.cs:39` — Race condition on shared `Dictionary<string, SyncedItemEntity>`

`SyncPassOrchestrator.OrchestrateAsync` starts `producerTask` (which runs deletion detectors that read/write `context.SyncedItems`) concurrently with `SyncJobExecutor.ExecuteAsync` (whose `onJobCompleted` callback writes `syncedItems[id] = entity` from worker threads). `Dictionary<K,V>` is not thread-safe; concurrent reads and writes can throw `InvalidOperationException` at runtime or silently corrupt state.

**Sequence:**
1. `producerTask` runs `RemoteDeletionDetector.DetectAndApplyAsync` → reads/removes from `syncedItems`
2. Simultaneously, pipeline workers complete download/upload jobs and call `onJobCompleted` → writes `syncedItems[id] = entity`
3. Both run on different threads with no synchronisation primitive on the dictionary

**Fix:** Replace `Dictionary<string, SyncedItemEntity>` with `ConcurrentDictionary<string, SyncedItemEntity>` in `RemoteEnumerationContext`, or gate all access behind the existing `Lock` object from `SyncProgressTracker`. Passing the mutable dictionary across producer/consumer abstraction boundaries is the root cause; a `ConcurrentDictionary` is the minimal safe fix.

```csharp
// RemoteEnumerationContext.cs
public ConcurrentDictionary<string, SyncedItemEntity> SyncedItems { get; set; } = new(StringComparer.OrdinalIgnoreCase);
```

All callers that read `.Values` or iterate must account for concurrent mutation by snapshotting first: `syncedItems.Values.ToList()`.

---

#### E2 — `Domain/CategoryResolutionService.cs:9` — Domain class takes EF Core `IDbContextFactory<AppDbContext>` — infrastructure leaks into domain

`CategoryResolutionService` lives in `Domain/` but its constructor takes `IDbContextFactory<AppDbContext>` directly. Domain services must not reference infrastructure types. This violates the Clean Architecture dependency rule and prevents the domain from being tested without a real (or mocked) database factory.

**Fix:** Move `CategoryResolutionService` to `Infrastructure/` (or `Data/`) alongside the other EF-backed services, or extract an `ICategoryRepository` interface in the domain and implement it in `Infrastructure/`.

```csharp
// Domain/ICategoryResolutionService.cs — already exists; the implementation belongs in Infrastructure/
// Move: Domain/CategoryResolutionService.cs → Infrastructure/Sync/CategoryResolutionService.cs
```

---

#### E3 — `Accounts/AccountFilesViewModel.cs:184` — `async void` event handler loses exceptions

`OnIncludeToggledAsync` is declared `async void`. Any exception thrown inside it after the first `await` is posted to the synchronisation context and cannot be caught by callers. In Avalonia this results in unhandled exceptions that crash the process.

**Fix:** Change the delegate signature or wrap in a fire-and-forget helper that catches and surfaces errors gracefully.

```csharp
// Before
private async void OnIncludeToggledAsync(object? sender, FolderTreeNodeViewModel node)

// After — use a fire-and-forget helper that surfaces errors via LoadError
private void OnIncludeToggledAsync(object? sender, FolderTreeNodeViewModel node)
    => _ = HandleIncludeToggledAsync(node);

private async Task HandleIncludeToggledAsync(FolderTreeNodeViewModel node)
{
    try { ... }
    catch (Exception ex) { OneDriveSyncClientMessages.FolderSelectionPersistFailed(logger, ...); }
}
```

---

#### E4 — `Infrastructure/Sync/Pipeline/SyncJobExecutor.cs:39` — Duplicate DB call: `GetAllCategoriesAsync` called twice per sync pass

`SyncPassOrchestrator.RunProducerAsync` loads classification mappings at line 83 (`classificationRepository.GetAllCategoriesAsync`) and passes them to `DownloadJobBuilder.BuildOneAsync`. Then `SyncJobExecutor.ExecuteAsync` makes a second identical call at line 39 (`fileClassificationRepository.GetAllCategoriesAsync`). On a large sync (300k files) the table is loaded twice on every run.

**Fix:** Pass the already-loaded `mappings` from `SyncPassOrchestrator` through to `ISyncJobExecutor.ExecuteAsync`, eliminating the redundant load from `SyncJobExecutor`.

```csharp
// ISyncJobExecutor.cs — add mappings parameter
Task<int> ExecuteAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, IAsyncEnumerable<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<FileClassificationCategory> mappings, Action<SyncProgressEventArgs> onProgress, Func<JobCompletedEventArgs, Task> onJobCompleted, CancellationToken ct);

// SyncJobExecutor.cs — remove internal GetAllCategoriesAsync call, use the passed mappings
```

Remove `IFileClassificationRepository` from `SyncJobExecutor`'s constructor entirely once the parameter is added.

---

### 🟡 Warnings

---

#### W1 — `Infrastructure/Sync/Pipeline/SyncProgressTracker.cs:53` — `error!` null-forgiving on `string?` — unclear null contract

`job.Fail(error!)` is called when `success` is `false`. The parameter `error` is typed `string?`, so `error` could legally be `null` at the call site — the `!` operator suppresses the compiler's null check rather than enforcing a contract. If any caller passes `(false, null)` the null-forgiving operator hides the bug.

**Fix:** Change the signature to accept separate overloads or use a required non-null error string when `success` is `false`. At minimum, add a null guard:

```csharp
// Before
var completedJob = success ? job.Complete() : job.Fail(error!);

// After
var completedJob = success
    ? job.Complete()
    : job.Fail(error ?? throw new ArgumentNullException(nameof(error), "Error message required when success is false."));
```

---

#### W2 — `Infrastructure/Sync/Jobs/UploadService.cs:150` — `CultureInfo.CurrentCulture` used for HTTP `Content-Length` header

```csharp
content.Headers.Add("Content-Length", chunk.Length.ToString(CultureInfo.CurrentCulture));
```

HTTP headers must use invariant formatting. `CultureInfo.CurrentCulture` is correct for `int` (no decimal or thousands separator), but it diverges from the invariant rule and is inconsistent with `Content-Range` on line 149 which also lacks explicit culture. TreatWarningsAsErrors will not catch this but it's a latent correctness risk.

**Fix:**
```csharp
content.Headers.Add("Content-Length", chunk.Length.ToString(CultureInfo.InvariantCulture));
```

---

#### W3 — `Infrastructure/Sync/Pipeline/SyncJobExecutor.cs` — SRP violation: mixes orchestration, entity persistence, and classification

`SyncJobExecutor` is responsible for:
1. Enqueuing jobs in batches to the repository
2. Routing jobs through the `ISyncPipeline`
3. Creating `SyncedItemEntity` from completed jobs (`SyncedItemEntityFactory.Create*`)
4. Classifying files and persisting classifications (`UpsertWithClassificationsAsync`)

Responsibilities 3 and 4 duplicate what `SyncedItemRegistrar` already does for phantom registrations. The classification+persist path (`UpsertWithClassificationsAsync`) has been copy-pasted from `SyncedItemRegistrar.RegisterPhantomAsync`.

**Fix:** Extract the entity-creation-and-persist step into a dedicated `ISyncedItemPersister` (or extend `ISyncedItemRegistrar`), so `SyncJobExecutor` only cares about orchestration. Remove `IFileAutoCategorisor`, `ICategoryResolutionService`, and the inline `UpsertWithClassificationsAsync` method.

---

#### W4 — `Data/Repositories/SyncedItemRepository.cs:160` — `DuplicatesOnly` search loads entire account into memory

```csharp
var candidates = await db.SyncedItems
    .Where(...)
    .Select(i => new { i.Id, i.SizeInBytes, FileName = i.RemotePath.Substring(...) })
    .ToListAsync(cancellationToken);

var duplicateIds = candidates
    .GroupBy(i => new { i.SizeInBytes, i.FileName })
    .Where(g => g.Count() > 1)
    ...
    .ToHashSet();
```

For accounts with tens of thousands of synced items this materialises the full set client-side. SQLite supports `GROUP BY … HAVING COUNT(*) > 1` which should be used instead to push the grouping to the database.

**Fix:** Use a subquery or raw SQL grouping so only duplicate IDs are returned from the DB.

---

#### W5 — `Infrastructure/Graph/GraphService.cs:50–79`, `82–118` — Duplicated pagination logic across `GetRootFoldersAsync` and `GetChildFoldersAsync`

Both methods implement identical `while (page?.Value is not null) { … WithUrl(page.OdataNextLink!) … }` loops. `GraphFolderEnumerator` also contains the same pattern.

**Fix:** Extract a `PaginateAsync<T>` helper:

```csharp
private static async Task<List<T>> PaginateAsync<T>(
    Task<DriveItemCollectionResponse?> firstPage,
    Func<DriveItem, T?> selector,
    Func<string, Task<DriveItemCollectionResponse?>> nextPage,
    CancellationToken ct)
```

---

#### W6 — `Accounts/AccountFilesViewModel.cs:107` — `CancellationToken.None` passed inside cancellable `LoadAsync`

```csharp
var loadedRuleStates = await syncRuleService.GetRuleStatesAsync(account.Id, CancellationToken.None);
```

`LoadAsync` is cancellable (invoked from UI with a CT), but `GetRuleStatesAsync` ignores cancellation. If the user navigates away while loading the cancellation token is lost for this call.

**Fix:** Store a `CancellationToken` parameter (or use `CancellationTokenSource` tied to the view lifecycle) and pass it through.

---

#### W7 — `Infrastructure/Sync/Detection/RemoteFolderEnumerator.cs:47` — O(n²) include-rule deduplication

```csharp
var rootIncludeRules = includeRules
    .Where(rule => !includeRules.Any(other =>
        other.RemotePath != rule.RemotePath &&
        rule.RemotePath.StartsWith(other.RemotePath + "/", ...)))
    .ToList();
```

For `n` rules this is O(n²) comparisons. In practice rule counts are small, but the pattern should be replaced with a sorted-tree or prefix-trie check.

---

#### W8 — `Infrastructure/Sync/Pipeline/SyncPassResult.cs` / `SyncPassResultFactory.cs` — No unit tests for new types

`SyncPassResult` and `SyncPassResultFactory` are new untracked files (git status). No test class (`GivenASyncPassResult.cs`) exists for them. The public factory must have tests per repo convention.

---

### 🔵 Suggestions

---

#### S1 — `Infrastructure/Sync/Pipeline/SyncPassOrchestrator.cs:18` — `OrchestrateAsync` mixes multiple abstraction levels

The method manages: drive state load/update, channel setup, producer task, first-job signalling, executor invocation, last-sync-time write, and progress raising. This is at least three different concerns. Extract the drive-state lifecycle into a dedicated helper to keep `OrchestrateAsync` at one level of abstraction.

---

#### S2 — `Infrastructure/Sync/Pipeline/SyncWorker.cs:41` — Explicit cast `(Option<string>)error!` is opaque

```csharp
await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, (Option<string>)error!, ct);
```

The explicit cast plus null-forgiving operator obscures intent. `error` is `string?` and `null` was assigned at line 29. At the point of this call `error` will be non-null (set by the error branch), but the pattern is confusing and hides the assumption.

**Fix:**
```csharp
await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, Option.Some(error!), ct);
```

---

#### S3 — `Infrastructure/Sync/Jobs/UploadService.cs:55` — Misleading method name `CreateSessionWithRetryAsync`

The method creates one session with no retry — the "retry" in its name is inaccurate. Only `UploadChunkWithRetryAsync` actually retries.

**Fix:** Rename to `CreateUploadSessionAsync`.

---

#### S4 — `Infrastructure/Sync/Jobs/HttpDownloader.cs:144` + `UploadService.cs:95` — Per-call buffer allocation; consider `ArrayPool<byte>`

```csharp
byte[] buffer = new byte[81920];   // HttpDownloader
byte[] buffer = new byte[ChunkSize10Mb];  // UploadService
```

For frequent sync passes `ArrayPool<byte>.Shared.Rent/Return` eliminates repeated large allocations and GC pressure. Low priority given sync runs are infrequent, but worth addressing at the buffer-size boundaries.

---

#### S5 — `Infrastructure/Sync/SyncService.cs:99` — Raw exception message exposed in UI progress events

```csharp
RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
```

`ex.Message` may contain file paths, stack fragments, or internal identifiers that should not be surfaced to the UI. This also makes localisation impossible for these error messages.

**Fix:** Map exception types to localisation keys before raising progress, or log the full message and surface only a generic key.

---

## Summary

| Severity | Count |
|----------|-------|
| 🔴 Error | 4 |
| 🟡 Warning | 8 |
| 🔵 Suggestion | 5 |
| **Total** | **17** |

---

## Verdict

**Request changes.**

E1 (race condition on `Dictionary`) and E3 (`async void`) are active bugs that can cause data corruption and crashes under normal usage. E2 (domain/infrastructure coupling) blocks unit-testing the domain without EF Core. E4 (duplicate DB call) fires on every sync pass. All four must be resolved before merge.
