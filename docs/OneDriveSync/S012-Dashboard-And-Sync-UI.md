# S012 — Dashboard & Sync UI

**Phase:** MVP  
**Area:** Features/Dashboard  
**Spec refs:** SE-03 to SE-06, SE-10, SE-13, SE-14, SE-15, EH-05, EH-06, AM-11, Section 7 (Dashboard nav item)

---

## User Story

As a user,  
I want to see all my accounts on one screen with live sync progress, ETA, and last-synced times, and be able to trigger a manual sync from there,  
So that I always know the current sync state of every account at a glance.

---

## Acceptance Criteria

### Dashboard Layout (Section 7)
- [x] Dashboard is the default landing view (first nav item)
- [x] Per-account cards showing: display name, auth status badge, last-synced timestamp (formatted per LO-07), current sync status, progress bar + ETA (when syncing)

### Manual Sync (SE-03)
- [x] "Sync Now" button per account card
- [x] "Sync Now" disabled and shows "Syncing…" while that account's sync is active (SE-06)
- [x] `ISyncEngine.StartSyncAsync(accountId, CancellationToken)` called on button press; result handled by `DashboardViewModel`

### Multi-Account Sync Warning (SE-05)
- [x] If `StartSyncAsync` returns `MultiAccountSyncWarning`, show confirmation dialog: "Syncing multiple accounts simultaneously may impact performance. Continue?"
- [x] User confirms → sync proceeds; user cancels → sync not started

### Progress Display (SE-13, SE-14)
- [x] Progress bar: account-level percentage complete ("45% ETA 3m 5s")
- [x] ETA updates in real-time via `IObservable<SyncProgress>` subscription — no polling (NF-01)
- [x] ETA reflects Graph API 429 throttling delays (ETA increases when throttled)

### Delta Token Expiry Prompt (SE-10)
- [x] On `FullResyncRequiredResult` from `ISyncEngine`: show dialog "Your sync data has expired (30+ days inactive). A full re-sync is required. Start now or postpone?"
- [x] "Start Now" → triggers full sync immediately; "Postpone" → sync deferred to next manual/scheduled trigger

### Skipped Files Notification (SE-15)
- [x] After sync completes, if `SyncReport.HasSkippedFiles` is `true`: show non-blocking toast: "Some files were skipped during sync — review the log for details"
- [ ] Toast links to Log Viewer filtered to that account

### Unmounted Drive Error (AM-11)
- [x] If sync fails with `LocalPathUnavailableError`: account card shows persistent error badge "Local folder unavailable — check your drive"
- [x] Badge dismissed when next sync succeeds

### Interrupted Sync Recovery (EH-05, EH-06)
- [x] Dashboard shows "Last sync interrupted — resume?" per affected account on startup (if `SyncState.Interrupted` detected)
- [x] "Resume" triggers `ISyncEngine.StartSyncAsync()` with resume intent; "Dismiss" clears the interrupted state
- [x] If resume fails: "Resume failed — a full re-sync is required. Start now?" presented to user

### Tests
- [x] **Unit test**: `DashboardViewModel` — `StartSyncAsync` success → account card moves to "Syncing" state
- [x] **Unit test**: `DashboardViewModel` — `MultiAccountSyncWarning` → confirmation requested; cancel → no sync started
- [x] **Unit test**: `DashboardViewModel` — `FullResyncRequiredResult` → dialog presented
- [x] **Unit test**: progress subscription — `SyncProgress` observable updates `PercentComplete` and `Eta` properties
- [x] **Unit test**: `SyncReport.HasSkippedFiles = true` → toast notification emitted; `ToastMessage` property set
- [x] **Unit test**: `SyncReport.HasSkippedFiles = false` (after a skipped-files sync) → `ToastMessage` cleared
- [x] **Unit test**: `DismissToastCommand` → `ToastMessage` set to null
- [x] **Unit test**: repository throws during `LoadAsync` → `LoadError` property set
- [x] **Unit test**: `AccountCardViewModel.EtaDisplay` — zero, boundary (1s), seconds, minutes+seconds
- [x] `dotnet build` zero errors/warnings; `dotnet test` all pass (189 tests)

---

## Technical Notes

- `DashboardViewModel` subscribes to `IObservable<SyncProgress>` and `IObservable<SyncStateChanged>` — no polling
- All observable subscriptions disposed on `DashboardViewModel` deactivation
- NF-02: UI input latency < 100 ms during active sync — progress updates must not block the UI thread; use `ObserveOn(RxApp.MainThreadScheduler)`
- NF-16: `DashboardViewModel` translates all `Result` failures to observable error state — no `try/catch`

---

## Implementation Constraints

- **`ObserveOn(RxApp.MainThreadScheduler)` for all progress subscriptions** — `IObservable<SyncProgress>` and `IObservable<SyncStateChanged>` originate from the sync engine's background thread; subscribing without `.ObserveOn(RxApp.MainThreadScheduler)` mutates bound properties off the UI thread and causes "called from wrong thread" errors.
- **`ProgressBar.Value` must bind to `double`** — `ProgressBar.Value` is a `double`-typed property; `PercentComplete` on the VM must be `double`, not `int`. A type mismatch produces a binding warning that becomes an error under `TreatWarningsAsErrors=true`.
- **Dialogs via Avalonia dialog API** — multi-account sync confirmation, delta-token expiry prompt, and resume-failed dialogs must use Avalonia's dialog infrastructure; no `MessageBox.Show()`.
- **`x:DataType` required on account card DataTemplate** — dashboard account cards use an `ItemsControl` DataTemplate; must declare `x:DataType`.
- **Dispose subscriptions on deactivation** — `DashboardViewModel` is singleton but subscriptions to per-sync observables must be scoped to the active sync lifetime, not the VM lifetime, to prevent stale progress from a previous sync appearing after a new one starts.
- **Dashboard is already registered** — `NavSection.Dashboard` is registered in S003's `RegisterAvailableFeatures()`; do not add a duplicate registration call.
---

## Dependencies

- S001 (project scaffolding)
- S003 (navigation shell — Dashboard is the default view)
- S005 (localisation — last-synced formatting)
- S008 (account management — accounts displayed on dashboard)
- S010 (sync engine — `ISyncEngine`, `ISyncProgressReporter`)
