# S010 — Sync Engine Core (Orchestration, State Tracking, Scheduling)

**Phase:** MVP  
**Area:** packages/core/astar-dev-sync-engine  
**Spec refs:** SE-01 to SE-15, EH-01 to EH-08, NF-01, NF-03, NF-04, NF-05, Section 9 (Sync Engine structure)

---

## User Story

As a user,  
I want the app to reliably sync my OneDrive folders bi-directionally — on demand and on a timer — with progress reporting, ETA, and resilience to network issues and interruptions,  
So that my files stay in sync without manual effort and I always know what's happening.

---

## Acceptance Criteria

### Bi-directional Sync (SE-01)
- [x] Sync detects and applies: remote-only additions, local-only additions, remote modifications, local modifications, remote deletions, local deletions
- [x] No silent data loss — every destructive action is logged before execution (NF-04)

### Concurrency Gate (SE-02, SE-06)
- [x] `SyncGate` enforces: max 1 sync per account at a time (SE-06); second trigger for same account is rejected with `SyncAlreadyRunningError`
- [x] Power User configurable max concurrent file transfers per account: 1–10 (default 5); hard ceiling 10 regardless of input
- [x] `SemaphoreSlim` used for transfer slot management — never `Thread.Sleep`

### Multi-Account Concurrent Sync Warning (SE-05)
- [x] If any account is already syncing and a second account's sync is triggered, `ISyncEngine.StartSyncAsync()` returns a `MultiAccountSyncWarning` signal
- [x] The desktop app (not the sync engine) decides to prompt the user — sync engine just signals; UI story (S012) handles the prompt

### Delta Sync (SE-09)
- [x] Incremental sync uses Graph delta queries via `IDeltaQueryService` (from `AStar.Dev.OneDrive.Client`)
- [x] `DeltaToken` persisted per account/folder in SQLite after each successful sync via `ISyncStateStore.SaveDeltaTokenAsync`; used on next run via `GetDeltaTokenAsync`

### Delta Token Expiry (SE-10)
- [x] On `DeltaTokenExpiredError`, engine raises a `FullResyncRequired` event — does not auto-proceed
- [x] `ISyncEngine` returns `FullResyncRequiredResult` to the caller; UI (S012) presents the user choice

### Full Re-sync Optimisation (SE-11)
- [x] During full re-sync: files where local `LastModified` + `Size` match remote `LastModifiedDateTime` + `Size` are **skipped** — no download/upload
- [x] Skipped file count reported in `SyncReport`

### Folder Rename Detection (SE-12)
- [x] `DeltaItem` with type `FolderRenamed` triggers a local folder rename (not delete+recreate)
- [x] Rename operation logged at `Information`; on failure (permissions, path conflict), logged at `Error` and surfaced in `SyncReport`

### Progress Reporting (SE-13, SE-14)
- [x] `ISyncProgressReporter` publishes: `PercentComplete`, `EtaSeconds`, `FilesProcessed`, `FilesTotal`
- [x] ETA recalculated after each file completes and after each Graph 429 delay — updates in real time
- [x] Progress publication via `IObservable<SyncProgress>` — UI subscribes without polling (NF-01)

### Special File Skipping (SE-15)
- [x] Symlinks, hardlinks, `.git` directories, socket files skipped during local scan
- [x] Each skip logged at `Debug`; post-sync `SyncReport` includes `SkippedFileCount`
- [x] `SyncReport` flag: `HasSkippedFiles` — UI uses this to show "some files were skipped — review the log" (wired in S012)

### State Tracking (EH-04, EH-05, EH-06)
- [x] `ISyncStateStore` persists sync state to SQLite: `Running`, `Interrupted`, `Completed`, `Failed` per account
- [x] On next launch/scheduled trigger, `Interrupted` state detected; resume attempted from the last completed file checkpoint
- [x] If resume impossible (checkpoint state corrupt), `ISyncEngine` returns `ResumeFailed` — UI (S012) shows explanation

### Error Resilience (EH-01)
- [x] Network failure during sync: exponential backoff (2ˢ seconds, max 5 retries, cap 60s)
- [x] After backoff exhausted: sync state set to `Interrupted`; engine stops and waits for next trigger
- [x] `CancellationToken` from `IAppLifetime` honours app shutdown during backoff (no wait past shutdown)

### Disk Space Check (EH-03)
- [x] Before starting sync: estimate download size from delta results; compare against available disk space
- [x] If available space < estimated download size + 10% buffer: return `InsufficientDiskSpaceError`; do not start
- [ ] If a write fails mid-sync with `IOException` (disk full): set state to `Failed`, log `Error`, surface in `SyncReport`

### DB Backup (EH-07)
- [x] `IDbBackupService.BackupAsync()` called before the first DB write in each sync run
- [x] Failure to backup: logged at `Warning`; sync proceeds (backup is best-effort)

### Scheduler (SE-04, SE-06)
- [x] `ISyncScheduler` runs scheduled syncs per account based on configured interval (5/15/30/60 min)
- [x] Scheduler honours `CancellationToken` from app lifetime — clean shutdown
- [x] If a scheduled sync is already running for the account, scheduler skips that tick (not queued)

### Sync Report
- [x] `SyncReport` record: `AccountId`, `StartedAt`, `CompletedAt`, `FilesDownloaded`, `FilesUploaded`, `FilesSkipped`, `ConflictsDetected`, `HasErrors`, `ErrorMessages`, `WasFullResyncTriggered`, `HasSkippedFiles`

### Tests (NF-09, NF-10)
- [x] **Unit test**: `SyncGate` — concurrent sync for same account rejected; different accounts permitted
- [x] **Unit test**: full re-sync skips files with matching timestamp + size
- [x] **Unit test**: folder rename via delta triggers local rename (not delete+create)
- [x] **Unit test**: exponential backoff — network error → retries at correct intervals; cancellation stops retries
- [x] **Unit test**: disk space check — insufficient space returns `InsufficientDiskSpaceError`
- [x] **Unit test**: special file skipping — symlinks, `.git` dirs excluded from local scan
- [x] **Unit test**: ETA calculation — updates correctly after each file and after 429 delay
- [x] **Unit test**: multi-account warning — `HadMultiAccountWarning` true only when another account was already syncing
- [ ] **Integration test**: interrupted sync state persists; detected on next trigger; resume attempted
- [x] `System.IO.Abstractions` used for all file I/O — real file system not touched in tests (NF-10)
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `ISyncEngine`, `ISyncScheduler`, `ISyncStateStore`, `ISyncProgressReporter` registered as **singletons** in the desktop app DI
- `IFileTransferService` registered as **transient** — new instance per file transfer slot
- `CancellationToken` threading: all background operations accept token from `IAppLifetime.ApplicationStopping`
- NF-16: all engine public methods return `Result<T>` — engine never throws into callers
- NF-00: every sync lifecycle event (start, pause, resume, complete, fail, file skip) logged
- Memory stability (NF-03): file list processed as an `IAsyncEnumerable` — never materialised entirely in memory

---

## Implementation Constraints

- **`ConfigureAwait(false)` on all `await` calls** — the sync engine is a package with no UI dependency; every `await` must use `.ConfigureAwait(false)`.
- **`IAsyncEnumerable` — never materialise** — the delta file list must remain as `IAsyncEnumerable<DeltaItem>` through the entire pipeline. Calling `.ToListAsync()` or `.ToArrayAsync()` on the full result defeats NF-03 (memory stability) and is a bug.
- **No UI thread access in the engine** — the sync engine must not reference `RxApp.MainThreadScheduler` or `Dispatcher.UIThread`. Publish scheduler-agnostic observables; the desktop app layer (S012) applies `.ObserveOn(RxApp.MainThreadScheduler)` at the subscription site.
- **`SemaphoreSlim` for async concurrency** — `SyncGate` and the transfer slot manager must use `SemaphoreSlim`; `lock`/`Monitor` are not awaitable and will deadlock if `await` is called while holding them.
- **`CancellationToken` propagated to every `await`** — every async call in the engine must accept and forward the `CancellationToken` from `IAppLifetime.ApplicationStopping`. An `await` without a cancellation token is a shutdown-correctness bug.
---

## Dependencies

- S001 (project scaffolding)
- S002 (database — sync state, delta tokens, sync history)
- S007 (authentication — token for Graph calls)
- S009 (OneDrive Client — delta queries, file download/upload)
