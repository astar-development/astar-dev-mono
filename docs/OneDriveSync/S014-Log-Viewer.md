# S014 — Log Viewer

**Phase:** MVP  
**Area:** Features/LogViewer  
**Spec refs:** LG-01 to LG-07, Section 7 (Log Viewer nav item)

---

## User Story

As a user,  
I want to view sync logs filtered by account, with detail level adapted to my user type,  
So that I can diagnose issues without being overwhelmed by technical output (Casual) or can see full verbose output when I need it (Power User).

---

## Acceptance Criteria

### Access (LG-03)
- [ ] Log Viewer is accessible from the nav rail — always visible (not hidden behind a setting)

### Account Filter (LG-04)
- [ ] Dropdown filter: "All Accounts" + one entry per configured account
- [ ] Filter applies immediately to the displayed log entries

### Detail Level by User Type (LG-05, LG-06, LG-01)
- [ ] **Casual mode**: displays only `Warning` and `Error` level entries, with friendly messages (sourced from `.resx` per LO-05)
- [ ] **Power User mode**: displays all levels including `Verbose`/`Debug` — raw Serilog output
- [ ] **Casual mode** shows a tooltip on the Log Viewer: "This view is simplified. Change to Power User in Settings for full log access" (LG-06)
- [ ] Power User: per-account debug logging toggle in Account Settings (AM-05) controls whether that account emits `Verbose`/`Debug` entries

### Log File Location (LG-07)
- [ ] Log files written to `~/.local/share/AStar.Dev.OneDriveSync/logs/` (Linux)
- [ ] Log file path exposed in the Log Viewer UI: "Logs stored at: [path]" with a "Copy Path" button
- [ ] Serilog `RollingFile` sink configured with daily rotation; retention of 7 days

### In-Memory Log Sink
- [ ] `InMemoryLogSink` (Serilog sink) registered as a singleton; holds last N log entries in memory
- [ ] `LogViewerViewModel` reads from `InMemoryLogSink` — does not re-read log files from disk

### Tests
- [ ] **Unit test**: `InMemoryLogSink` — only holds last N entries; oldest evicted when N exceeded
- [ ] **Unit test**: `LogViewerViewModel` in Casual mode — only `Warning`/`Error` entries visible; `Debug` entries filtered out
- [ ] **Unit test**: `LogViewerViewModel` — account filter applied → only matching account entries visible
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `InMemoryLogSink` registered as **singleton** at Serilog pipeline build time; injected into `LogViewerViewModel` via `ILogEntryProvider`
- `LogViewerViewModel` is **scoped** — filter state resets on each navigation away and back (acceptable for MVP)
- NF-07: `InMemoryLogSink` must strip any PII before storing entries (no email/account name — synthetic ID only)
- Post-MVP (LG-08): file path/name masking is out of scope for this story

---

## Implementation Constraints

- `InMemoryLogSink` is called from the Serilog pipeline on arbitrary background threads. The internal ring buffer **must** be a `ConcurrentQueue<LogEntry>` (or guarded by `SemaphoreSlim`) — never a plain `List<T>` with a `lock`, which causes deadlocks when the sink is called from an async context.
- Notifications from `InMemoryLogSink` to `LogViewerViewModel` must use `ObserveOn(RxApp.MainThreadScheduler)` before binding to any UI-bound property.
- `x:DataType` is mandatory on every `DataTemplate` in `LogViewerView.axaml`. With `AvaloniaUseCompiledBindingsByDefault=true`, a missing `x:DataType` produces no error — bindings silently produce no output.
- Register the Log Viewer nav item in `ShellServiceExtensions` only when this story ships (NF-15); until then the nav item remains disabled.

---

## Dependencies

- S001 (project scaffolding)
- S003 (navigation shell — Log Viewer nav item)
- S005 (localisation — friendly error messages in `.resx`)
- S006 (onboarding — user type determines detail level)
- S008 (account management — per-account debug toggle)
