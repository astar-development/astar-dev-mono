# S018 — [Post-MVP] Resumable Downloads

**Phase:** Post-MVP  
**Area:** packages/infra/astar-dev-onedrive-client — Features/FileOperations  
**Spec refs:** SE-16

---

## User Story

As a Power User syncing large files on an unreliable connection,  
I want interrupted downloads to resume from where they left off instead of restarting from byte 0,  
So that I don't waste bandwidth retrying large files after transient network drops.

---

## Acceptance Criteria

- [ ] `IFileDownloader.DownloadAsync()` signature extended with an optional `byteOffset` parameter (interface change is backward-compatible — default `0`)
- [ ] For files > a configurable threshold (default 50 MB), interrupted downloads resume via HTTP range requests (`Range: bytes=N-`)
- [ ] Partial download progress (byte offset) stored in SQLite per in-progress file; cleared on completion or cancellation
- [ ] On sync resume (EH-05): `ISyncStateStore` provides byte offset for each in-progress file; `IFileDownloader` resumes from that offset
- [ ] Tests: unit tests for range request construction; integration test for resume from offset

---

## Implementation Constraints

- All `await` calls in `IFileDownloader` and its implementation must use `ConfigureAwait(false)`. This is a package-level component; omitting `ConfigureAwait(false)` causes deadlocks when called from any synchronisation-context-bearing thread (e.g., Avalonia's UI thread) that is waiting synchronously.
- The `byteOffset` parameter added to `DownloadAsync()` must be **optional with a default of `0`** to preserve backward compatibility with all existing call sites. The interface change must not require callers to update their signatures.

---

## Dependencies

- S009 (OneDrive Client — `IFileDownloader`)
- S010 (Sync Engine — state store for in-progress byte offsets)
