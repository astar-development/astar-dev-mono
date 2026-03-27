# S011 — Conflict Resolution

**Phase:** MVP  
**Area:** packages/core/astar-dev-conflict-resolution + Features/ConflictResolution (desktop app)  
**Spec refs:** CR-01 to CR-08, NF-04, NF-05, Section 7 (Conflicts nav item)

---

## User Story

As a user,  
I want to see all sync conflicts in one place, select multiple at once, and apply a resolution strategy (Local Wins, Remote Wins, Keep Both, or Skip) without any conflict being silently overwritten,  
So that I have full control over which version of each file is kept.

---

## Acceptance Criteria

### Conflict Detection (CR-01)
- [ ] `IConflictDetector` in `Features/Detection/` identifies conflicts: same file modified on both sides; file deleted one side and present the other
- [ ] Detection integrated into sync engine (S010) — conflicts are raised during sync, not after
- [ ] `ConflictRecord` entity persisted to SQLite immediately on detection — survives crash (NF-05)

### Manual Resolution Required (CR-02)
- [ ] No conflict is resolved silently — all conflicts land in the persistent queue
- [ ] Sync engine skips conflicted files and continues with non-conflicted files — sync does not halt on conflict

### Resolution Strategies (CR-03, CR-04)
- [ ] **Local Wins**: local file overrides remote; remote version discarded
- [ ] **Remote Wins**: remote file downloaded; local version discarded
- [ ] **Keep Both**: conflicting copy renamed `original-name-(yyyy-MM-ddTHHmmssZ).ext` (UTC, locale-invariant per LO-06); both versions retained
- [ ] **Skip**: conflict deferred; remains in queue; persisted across sessions (CR-05)
- [ ] `IConflictResolver` executes the chosen strategy via `Result<T>` — never throws

### Cascade Resolution (CR-08)
- [ ] On resolution of a conflict, `ICascadeService` checks for other pending conflicts with the same `FilePath` across all accounts and sessions
- [ ] Matching pending conflicts have the same resolution applied automatically
- [ ] Cascade applied conflicts are logged individually at `Information`

### Conflict Queue Persistence (CR-05, NF-05)
- [ ] `IConflictStore` in `Features/Persistence/`: `AddAsync`, `GetPendingAsync`, `ResolveAsync`, `GetByFilePathAsync`
- [ ] All writes are atomic (SQLite transactions) — partial writes do not produce corrupt queue state
- [ ] Queue is durable: app crash and relaunch recovers exact same pending conflicts

### Conflicts View UI (CR-06, CR-07, Section 7)
- [ ] Cross-account conflict list, newest first, with a badge count on the nav rail icon
- [ ] Each conflict shows: file name, local modified date, remote modified date, conflict type (modified/deleted), account name
- [ ] Each conflict has a checkbox — user can select any subset
- [ ] "Select All" button selects all visible conflicts (CR-07)
- [ ] Resolution strategy buttons: "Local Wins", "Remote Wins", "Keep Both", "Skip" — apply to all selected conflicts
- [ ] Badge count updates in real time as conflicts are added/resolved

### Tests (NF-09)
- [ ] **Unit test**: `ConflictDetector` — same-file-both-sides → conflict; delete-one-side → conflict; no change → no conflict
- [ ] **Unit test**: `ConflictResolver` — "Keep Both" renames with correct UTC datetime format
- [ ] **Unit test**: `ConflictResolver` — "Skip" leaves conflict in queue; "Local Wins" removes from queue after execution
- [ ] **Unit test**: `CascadeService` — resolving conflict cascades to all matching pending conflicts by file path
- [ ] **Integration test**: conflict added, queue retrieved, resolved — queue empty; no orphaned rows
- [ ] **Integration test**: queue survives simulated crash (connection closed mid-write using SQLite WAL; verify consistency on reopen)
- [ ] **Unit test**: `ConflictsViewModel` — badge count increments on new conflict; decrements on resolution
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `IConflictStore`, `IConflictDetector`, `IConflictResolver`, `ICascadeService` registered as **singletons** — queue is app-wide state
- `ConflictsViewModel` subscribes to an `IObservable<ConflictQueueChanged>` from `IConflictStore` to update badge count reactively
- NF-16: all service methods return `Result<T>`
- NF-04: zero tolerance for silent data loss — every "Local Wins" / "Remote Wins" deletion logged at `Warning` before execution
- NF-07: file paths logged (permitted in MVP per LG-02); no account email/name in conflict logs

---

## Dependencies

- S001 (project scaffolding)
- S002 (database — conflict queue stored in SQLite)
- S003 (navigation shell — Conflicts nav item, badge count)
- S005 (localisation)
- S010 (sync engine — conflict detection integrated into sync)
