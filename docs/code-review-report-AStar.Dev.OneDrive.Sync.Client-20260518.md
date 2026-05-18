# Code Review — `AStar.Dev.OneDrive.Sync.Client`

**Date:** 2026-05-18  
**Reviewer:** Claude Code (c-sharp-reviewer)  
**Scope:** SRP / SOC violations + functional-paradigm misuse  
**Branch at review:** `doc/improve-claude-md-subagent-verification` (HEAD `5ea6b00`)

---

## References

- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [SOLID Principles](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp)
- [AStar.Dev repo guidelines — CLAUDE.md](../CLAUDE.md)
- [AStar.Dev C# code style](../.claude/rules/c-sharp-code-style.md)
- [OWASP — Sensitive Data Exposure](https://owasp.org/www-project-top-ten/2017/A3_2017-Sensitive_Data_Exposure)

---

## Findings

### 🔴 Errors

---

#### E1 — `SyncService.cs:16` — DI bypassed: `SyncPassOrchestrator` newed up inside `SyncService`

`private readonly SyncPassOrchestrator syncPassOrchestrator = new(accountRepository, driveStateRepository, dependencies);`

`SyncPassOrchestrator` is a concrete class created inline from constructor parameters. This bypasses the DI container, makes it untestable in isolation, and means the container never manages its lifetime. The constructor already has 8 parameters — the orchestrator should be injected, not constructed.

**Fix:** Register `SyncPassOrchestrator` in the DI container and inject `ISyncPassOrchestrator` (extract interface).

---

#### E2 — `SyncService.cs:107` — `Match` used instead of `MatchAsync`; inner download error silently dropped

```csharp
await urlResult.Match(           // ← should be MatchAsync
    async url =>
    {
        var downloadResult = await httpDownloader.DownloadAsync(...);
        downloadResult.Match<Unit>(   // ← result discarded; download failure is lost
            _ => Unit.Default,
            downloadError => { RaiseProgress(...); return Unit.Default; });
    },
    error => { ...; return Task.CompletedTask; });
```

Two problems:
1. `Match` is called on a `Result<string,string>` where the success branch is `async` — should be `MatchAsync`.
2. The inner `downloadResult.Match<Unit>(...)` return value is discarded — a download failure only calls `RaiseProgress` but execution falls through without signalling the caller. No error is propagated out of `ApplyConflictOutcomeAsync`.

**Fix:**
```csharp
await urlResult.MatchAsync(
    async url =>
    {
        await httpDownloader.DownloadAsync(url, conflict.Target.LocalPath, conflict.Snapshot.RemoteModified, ct: ct)
            .TapErrorAsync(downloadError =>
            {
                Serilog.Log.Error(...);
                RaiseProgress(accountId, 0, 0, downloadError, SyncState.Error);
            });
    },
    error =>
    {
        Serilog.Log.Error(...);
        RaiseProgress(accountId, 0, 0, error, SyncState.Error);
        return Task.CompletedTask;
    });
```

---

#### E3 — `AddAccountWizardViewModel.cs:125-137` — `switch` on `Result<>` discriminated union (repo rule violation)

```csharp
switch(result)
{
    case Result<AuthResult, AuthError>.Ok successfulLoginResult: ...
    case Result<AuthResult, AuthError>.Error { Reason: AuthCancelledError }: ...
    case Result<AuthResult, AuthError>.Error { Reason: AuthFailedError failed }: ...
}
```

Repo rule (`.claude/rules/c-sharp-code-style.md`): _"Never use `is Result<T,E>.Ok` / `is not Result<T,E>.Ok` pattern matching in production code — use `Match` or `MatchAsync`."_ The switch also has no `default` arm — a new `AuthError` subtype silently does nothing.

**Fix:**
```csharp
result.Match(
    ok => UpdateSuccessfulLoginState(ok),
    error =>
    {
        switch (error)
        {
            case AuthCancelledError: SetCancelledLoginState(); break;
            case AuthFailedError failed: SetFailedLoginState(failed); break;
            default: SetFailedLoginState(new AuthFailedError("Unexpected auth error.")); break;
        }
    });
```

---

#### E4 — `ParallelDownloadPipeline.cs:61-63` — `DownloadWorker` instantiated with `new`, bypassing DI

```csharp
var workers = Enumerable.Range(1, workerCount)
    .Select(id => new DownloadWorker(id, downloader, graphService, syncRepository, fileSystem)
    .RunAsync(channel.Reader, accessToken, OnJobComplete, ct))
    .ToList();
```

`DownloadWorker` is created inline inside `ParallelDownloadPipeline`. The worker cannot be replaced or mocked in tests without replacing the whole pipeline. The `workerId` int being passed is also not injectable — this pattern makes the worker count and composition opaque.

**Fix:** Introduce `IDownloadWorkerFactory` and inject it. The factory constructs numbered workers from resolved dependencies.

---

### 🟡 Warnings

---

#### W1 — `AccountFilesViewModel.cs:147` — `is not Option<DriveId>.Some` instead of `Match`/`Bind`

```csharp
if(_driveId is not Option<DriveId>.Some driveIdSome)
{
    Serilog.Log.Warning(...);
    return;
}
var vm = new FolderTreeNodeViewModel(node, _graphService, _accessToken!, driveIdSome.Value);
```

Repo rule violation — same as E3 but for `Option<T>`. Should use `Match` or `Bind` from `AStar.Dev.Functional.Extensions`.

**Fix:**
```csharp
_driveId.Match(
    driveIdSome => RootFolders.Add(new FolderTreeNodeViewModel(node, _graphService, _accessToken!, driveIdSome)),
    ()          => Serilog.Log.Warning("[AccountFilesViewModel] Drive ID not available..."));
```

---

#### W2 — `SyncScheduler.cs:69-74` — `TapAsync` silently ignores "account not found"

```csharp
public async Task TriggerAccountAsync(string accountId, CancellationToken ct = default)
    => await accountRepository.GetByIdAsync(new AccountId(accountId), ct)
        .TapAsync(async entity => { ... });
```

`GetByIdAsync` returns `Option<AccountEntity>`. `TapAsync` on `Option.None` does nothing and returns normally — the caller has no signal that the account was not found. A manual trigger for a non-existent account silently no-ops.

**Fix:** Use `MatchAsync` and log/throw on `None`.

---

#### W3 — `UploadService.cs:50-51` — `Match()` used as boolean predicate; use `Tap`

```csharp
var uploadResult = await UploadChunksAsync(...);

if(uploadResult.Match(_ => true, _ => false))
    Serilog.Log.Information("[UploadService] Upload complete: {Path}", remotePath);
```

Using `Match` to extract a bool for a conditional is a pattern smell. The success-only side-effect should be expressed with `Tap`/`TapAsync`.

**Fix:**
```csharp
return await UploadChunksAsync(sessionUrl, localPath, fileInfo.Length, progress, ct)
    .TapAsync(itemId => Serilog.Log.Information("[UploadService] Upload complete: {Path}", remotePath));
```

---

#### W4 — `UploadService.cs:113-118` — Nullable `Match<Result?>` + null check; use `Bind`

```csharp
var earlyReturn = chunkResult.Match<Result<string, string>?>(
    itemId => itemId is not null ? new Result<string, string>.Ok(itemId) : null,
    error => new Result<string, string>.Error(error));

if(earlyReturn is not null)
    return earlyReturn;
```

The inner `Result<string?, string>` is unwrapped into a nullable `Result<string, string>?` and then null-checked. This loses the railway-oriented structure. `Bind` or `Map` keeps the chain clean.

**Fix:**
```csharp
var earlyReturn = chunkResult.Bind<string>(
    itemId => itemId is not null
        ? new Result<string, string>.Ok(itemId)
        : new Result<string, string>.Error("Upload response missing item ID."));

if (earlyReturn is Result<string, string>.Ok or Result<string, string>.Error { } err when err.Reason is not null ... )
```
Or restructure `UploadChunksAsync` to return `Result<string, string>` directly by collecting partial IDs.

---

#### W5 — `SyncService.cs` — SRP violation: auth + conflict application + file ops + progress in one class

`SyncService` is responsible for:
- Acquiring access tokens (`IAuthService.AcquireTokenSilentAsync`)
- Orchestrating sync passes (delegates to `SyncPassOrchestrator`)
- Applying conflict outcomes including file system moves (`fileSystem.File.Move`, `IHttpDownloader.DownloadAsync`, `IGraphService.GetDownloadUrlAsync`)
- Progress event management (`RaiseProgress`)

The `ApplyConflictOutcomeAsync` method mixes high-level conflict policy dispatch with low-level file operations and HTTP downloads. These are at least two separate classes: `ISyncService` (orchestration) and `IConflictApplier` (outcome execution).

The `IHttpDownloader`, `IGraphService`, and `IFileSystem` injected into `SyncService` are only used in `ApplyConflictOutcomeAsync` — confirming it belongs elsewhere.

**Fix:** Extract `ApplyConflictOutcomeAsync` and its dependencies into a dedicated `ConflictApplier` service. Inject `IConflictApplier` into `SyncService`.

---

#### W6 — `RemoteFolderEnumerator.cs` — SRP/SOC violation: enumeration + conflict resolution + DB backfill + job creation

`RemoteFolderEnumerator` performs:
1. Sync rule loading (`syncRuleRepository.GetByAccountIdAsync`)
2. Drive ID resolution (`graphService.GetDriveIdAsync`)
3. Folder ID resolution and **database backfill** (`syncRuleRepository.UpsertAsync` in `ResolveAndBackFillFolderIdAsync`)
4. Conflict detection and policy application (`HandleConflictAsync` → `ConflictResolver.Resolve`)
5. Phantom item creation (`syncedItemRepository.UpsertAsync` inside `ProcessFileItemAsync`)
6. Download job construction (`SyncJobFactory.CreateDownload`)
7. Folder directory creation (`fileSystem.Directory.CreateDirectory` in `HandleFolderAsync`)

Conflict policy application, phantom-item registration, and folder creation are separate responsibilities. The class also mutates both `downloadJobs` and `syncedItems` via side effects through deeply nested async calls, making the flow hard to follow and test.

**Fix:** Extract conflict handling into a reused `IConflictApplier`. Phantom-item registration into a dedicated step. Folder creation is a side effect that belongs in a post-enumeration step.

---

#### W7 — `DownloadWorker.cs` — SRP violation: handles three job types in one switch

```csharp
switch(job)
{
    case DownloadSyncJob downloadJob: ...
    case UploadSyncJob uploadJob:    ...
    case DeleteSyncJob deleteJob:    ...
    default: ...
}
```

A single worker handles download, upload, and delete — three distinct responsibilities with different infrastructure dependencies. Adding a new job type requires modifying this class (OCP violation). The class is also misnamed — it is not a "download" worker.

**Fix:** Extract `IJobHandler` with implementations `DownloadJobHandler`, `UploadJobHandler`, `DeleteJobHandler`. `DownloadWorker` (renamed `SyncWorker`) dispatches to the appropriate handler via a registry/factory.

---

#### W8 — `ParallelDownloadPipeline.cs` — SOC: misnamed, runs all job types, no factory abstraction

The class is named `ParallelDownloadPipeline` but it processes downloads, uploads, and deletes. Its `RunAsync` method embeds `OnJobComplete` as an inline closure that mutates shared state under a `Lock`, wires up `done`/`total` counters, invokes progress/completion callbacks, and manages channel lifecycle — all in one method.

**Fix:** Rename to `ParallelSyncPipeline`. Extract progress tracking into a separate `SyncProgressTracker`. Introduce `IDownloadWorkerFactory` (see E4).

---

#### W9 — `AccountsViewModel.cs` — SRP: domain logic and persistence inside VM

`AccountsViewModel` is responsible for:
- Displaying account cards (correct VM concern)
- Persisting new accounts to the DB (`repository.UpsertAsync`)
- Setting the active account in the DB (`repository.SetActiveAccountAsync`)
- Writing sync rules to the DB (`syncRuleRepository.UpsertAsync`)
- Computing a default local sync path (`ApplicationMetadata.ApplicationNameLowered.UserDirectory().CombinePath(...)`)
- Mapping `OneDriveAccount` → `AccountEntity` (`ToEntity` method)
- Constructing wizard VMs directly (not via factory)

`OnWizardCompletedAsync` at lines 91–135 is an orchestration method that crosses at least three abstraction layers (UI state, persistence, domain).

**Fix:** Extract account persistence/mapping into a `IAccountOnboardingService` (or similar application-layer service). VM calls service; service handles repository interaction and default-path logic.

---

#### W10 — `AccountFilesViewModel.cs:164-192` — `async void` with unreachable outer catch; untestable platform call

```csharp
private async void OnIncludeToggledAsync(object? sender, FolderTreeNodeViewModel node)
{
    try
    {
        try { ... }            // ← inner catch swallows all exceptions
        catch(Exception ex) { ... }
    }
    catch(Exception ex) { ... } // ← outer catch is unreachable
}
```

Outer catch is dead code — the inner catch swallows everything. `async void` is necessary for the event subscription pattern in Avalonia, but the nested try/catch structure is redundant and misleading.

Additionally at line 199:
```csharp
string opener = OperatingSystem.IsWindows() ? "explorer" : ...;
_ = System.Diagnostics.Process.Start(opener, path);
```

`Environment.GetFolderPath` and `Process.Start` are not abstracted — platform logic is embedded in the VM and cannot be unit-tested.

**Fix:** Remove outer try/catch. Extract OS-specific file-manager opening into `IFileManagerService` and inject it.

---

#### W11 — `GraphService.cs:252-273` — Token-keyed in-memory cache with no eviction; sensitive data in dict keys

```csharp
private readonly ConcurrentDictionary<string, DriveContext> _cache = [];

if(_cache.TryGetValue(accessToken, out var cached))
    return ...;

_cache[accessToken] = driveContext;
```

The cache is keyed by raw access token strings. Problems:
1. **Memory leak** — tokens accumulate over the lifetime of the singleton; the dict grows without bound as tokens rotate (every hour with MSAL refresh).
2. **Security** — [OWASP Sensitive Data Exposure](https://owasp.org/www-project-top-ten/2017/A3_2017-Sensitive_Data_Exposure): raw token strings should not be stored in plain memory as dictionary keys. Use a non-reversible key (e.g., `SHA256` of the token, or the account ID which is stable across token rotations).

**Fix:** Key the cache by account ID (stable, non-sensitive). Obtain account ID from the token claims or pass it as a parameter. Remove old entries when tokens are revoked/account removed.

---

#### W12 — `AddAccountWizardViewModel.cs:239-250` — Domain object constructed inside VM

```csharp
private void Finish()
{
    var account = new OneDriveAccount
    {
        Id = new AccountId(_accountId),
        Profile = AccountProfileFactory.Create(ConfirmedDisplayName, ConfirmedEmail),
        SelectedFolderIds = [...],
        FolderNames = Folders.Where(f => f.IsSelected).ToDictionary(...)
    };
    Completed?.Invoke(this, account);
}
```

The VM directly constructs a `OneDriveAccount` domain object from raw wizard state. Domain object creation is the factory's responsibility. The VM knows too much about how `OneDriveAccount` is assembled.

**Fix:** Introduce `OneDriveAccountFactory.CreateFromWizard(accountId, displayName, email, selectedFolders)` and call it from `Finish()`.

---

### 🔵 Nits (no GitHub issue)

---

#### N1 — `SyncPassOrchestrator.cs:11` — 6-parameter method; use parameter object

`OrchestrateAsync(OneDriveAccount, string, Func<SyncConflict, Task>, Action<SyncProgressEventArgs>?, Action<JobCompletedEventArgs>?, CancellationToken)` — 6 parameters. The three callbacks should be grouped into a `SyncCallbacks` parameter object.

---

#### N2 — `SyncService.cs:14` — Constructor still 8 parameters despite `SyncServiceDependencies`

`IHttpDownloader`, `IGraphService`, and `IFileSystem` are only used in `ApplyConflictOutcomeAsync`. Once that method is extracted (see W5), the constructor shrinks to a manageable size.

---

#### N3 — `ApplicationInitializer.cs` — Knows about 5 VMs; borderline SRP

`ApplicationInitializer` directly wires event subscriptions on 3 VMs, restores accounts across 4 VMs, and activates the initial account across 2 VMs. Consider a mediator or startup coordinator pattern.

---

#### N4 — `SyncRepository.cs:20-40` — Type switch in LINQ `Select`

Inline `switch(j)` inside a LINQ `Select` mixes data mapping with control flow. Extract to a `ToEntity(SyncJob)` helper method.

---

#### N5 — `SyncScheduler.cs:115` — Fragile null-guard via string length

`entity.SyncConfig.LocalSyncPath.Value.Length > 0 ? entity.SyncConfig : null` — uses string length as a proxy for "has a valid path". Should use `Option<AccountSyncConfig>` or validate via `LocalSyncPathFactory`.

---

## Summary

| Severity | Count |
|----------|-------|
| 🔴 Error | 4 |
| 🟡 Warning | 12 |
| 🔵 Nit | 5 |
| **Total** | **21** |

## Verdict: **Request Changes**

Four errors block merge — particularly the DI bypasses (E1, E4), the `MatchAsync` / silent error-drop (E2), and the prohibited `switch`-on-`Result` pattern (E3). The twelve warnings represent significant SRP and functional-paradigm debt that will compound as the codebase grows. The GraphService token-cache issue (W11) is a security concern and should be treated as an error if this application handles user credentials.
