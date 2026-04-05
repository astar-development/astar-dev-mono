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
- [x] `IConflictDetector` in `Features/Detection/` identifies conflicts: same file modified on both sides; file deleted one side and present the other
- [x] Detection integrated into sync engine (S010) — conflicts are raised during sync, not after
- [x] `ConflictRecord` entity persisted to SQLite immediately on detection — survives crash (NF-05)

### Manual Resolution Required (CR-02)
- [x] No conflict is resolved silently — all conflicts land in the persistent queue
- [x] Sync engine skips conflicted files and continues with non-conflicted files — sync does not halt on conflict

### Resolution Strategies (CR-03, CR-04)
- [x] **Local Wins**: local file overrides remote; remote version discarded
- [x] **Remote Wins**: remote file downloaded; local version discarded
- [x] **Keep Both**: conflicting copy renamed `original-name-(yyyy-MM-ddTHHmmssZ).ext` (UTC, locale-invariant per LO-06); both versions retained
- [x] **Skip**: conflict deferred; remains in queue; persisted across sessions (CR-05)
- [x] `IConflictResolver` executes the chosen strategy via `Result<T>` — never throws

### Cascade Resolution (CR-08)
- [x] On resolution of a conflict, `ICascadeService` checks for other pending conflicts with the same `FilePath` across all accounts and sessions
- [x] Matching pending conflicts have the same resolution applied automatically
- [x] Cascade applied conflicts are logged individually at `Information`

### Conflict Queue Persistence (CR-05, NF-05)
- [x] `IConflictStore` in `Features/Persistence/`: `AddAsync`, `GetPendingAsync`, `ResolveAsync`, `GetByFilePathAsync`
- [x] All writes are atomic (SQLite transactions) — partial writes do not produce corrupt queue state
- [x] Queue is durable: app crash and relaunch recovers exact same pending conflicts

### Conflicts View UI (CR-06, CR-07, Section 7)
- [x] Cross-account conflict list, newest first, with a badge count on the nav rail icon
- [x] Each conflict shows: file name, local modified date, remote modified date, conflict type (modified/deleted), account name
- [x] Each conflict has a checkbox — user can select any subset
- [x] "Select All" button selects all visible conflicts (CR-07)
- [x] Resolution strategy buttons: "Local Wins", "Remote Wins", "Keep Both", "Skip" — apply to all selected conflicts
- [x] Badge count updates in real time as conflicts are added/resolved

### Tests (NF-09)
- [x] **Unit test**: `ConflictDetector` — same-file-both-sides → conflict; delete-one-side → conflict; no change → no conflict
- [x] **Unit test**: `ConflictResolver` — "Keep Both" renames with correct UTC datetime format
- [x] **Unit test**: `ConflictResolver` — "Skip" leaves conflict in queue; "Local Wins" removes from queue after execution
- [x] **Unit test**: `CascadeService` — resolving conflict cascades to all matching pending conflicts by file path
- [x] **Integration test**: conflict added, queue retrieved, resolved — queue empty; no orphaned rows
- [x] **Integration test**: queue survives simulated crash (connection closed mid-write using SQLite WAL; verify consistency on reopen)
- [x] **Unit test**: `ConflictsViewModel` — badge count increments on new conflict; decrements on resolution
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `IConflictStore`, `IConflictDetector`, `IConflictResolver`, `ICascadeService` registered as **singletons** — queue is app-wide state
- `ConflictsViewModel` subscribes to an `IObservable<ConflictQueueChanged>` from `IConflictStore` to update badge count reactively
- NF-16: all service methods return `Result<T>`
- NF-04: zero tolerance for silent data loss — every "Local Wins" / "Remote Wins" deletion logged at `Warning` before execution
- NF-07: file paths logged (permitted in MVP per LG-02); no account email/name in conflict logs

---

## Implementation Constraints

- **`ObserveOn(RxApp.MainThreadScheduler)` for badge count** — `IConflictStore.ConflictQueueChanged` fires from the sync engine's background thread; `ConflictsViewModel` must call `.ObserveOn(RxApp.MainThreadScheduler)` before updating any bound property or collection.
- **`x:DataType` required on conflict list DataTemplate** — conflict row DataTemplates must declare `x:DataType="local:ConflictItemViewModel"` or the build will fail under `AvaloniaUseCompiledBindingsByDefault=true`.
- **Badge count on `NavItemViewModel`** — displaying a live badge on the nav rail icon requires adding a reactive `BadgeCount` property (using the `field` keyword + `RaiseAndSetIfChanged`) to `NavItemViewModel` and a corresponding visual element in `IconRailButton.axaml`. This is a structural extension to the S003 shell; coordinate with the shell owner before implementing.
- **`ObservableCollection` checkbox state** — "Select All" toggling via `foreach` over a large collection will fire one `CollectionChanged` per item. Use `SuppressNotifications` / batch update pattern to avoid per-item layout passes causing UI stutter (NF-02).
- **Register Conflicts nav item** — `ShellServiceExtensions.RegisterAvailableFeatures()` must call `service.Register(NavSection.Conflicts)` when this story ships.
---

## Dependencies

- S001 (project scaffolding)
- S002 (database — conflict queue stored in SQLite)
- S003 (navigation shell — Conflicts nav item, badge count)
- S005 (localisation)
- S010 (sync engine — conflict detection integrated into sync)
