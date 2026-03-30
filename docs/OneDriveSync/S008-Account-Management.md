# S008 — Account Management

**Phase:** MVP  
**Area:** Features/AccountManagement  
**Spec refs:** AM-01 to AM-15, Section 7 (Accounts nav item)

---

## User Story

As a user,  
I want to add, configure, and remove OneDrive accounts through a guided wizard and a settings view,  
So that I can manage which accounts are synced and where their files are stored on my machine.

---

## Acceptance Criteria

### Add Account Wizard — 3 Steps (AM-01, AM-02)
- [ ] Step 1 — Authenticate: triggers MSAL browser flow (delegates to S007); displays spinner while waiting; shows error if auth fails with a retry option
- [ ] Step 2 — Folder Selection: shows root OneDrive folders fetched from Graph; tree is expandable to arbitrary depth; multi-select supported; "Select All" option (AM-03); default is "all folders selected"
- [ ] Step 3 — Confirm: summary of account, selected folders, and default local sync path; "Back" and "Finish" buttons
- [ ] Wizard navigation: "Next"/"Back" between steps; "Cancel" at any step dismisses without saving

### Folder Selection (AM-03, AM-04)
- [ ] Wizard step 2 fetches folders from OneDrive via `IOneDriveFolderService` (in `AStar.Dev.OneDrive.Client`)
- [ ] Folder selection is editable from the Accounts view after wizard completion (not only during wizard)
- [ ] `IOneDriveFolderService` interface in `packages/infra/astar-dev-onedrive-client/Features/FolderBrowsing/`

### Local Sync Path (AM-07, AM-06)
- [ ] Default local path set on wizard completion: `~/OneDrive/<account-display-name>/` — editable before finishing
- [ ] Each account must have a unique, non-overlapping local folder; wizard and settings UI both prevent overlapping selection
- [ ] Overlap check: if the selected path is a prefix of, or contained within, another account's path → blocked with a clear message

### Non-Empty Folder Warning (AM-10)
- [ ] If the selected local sync folder is not empty, warn: "This folder already contains files — conflicts may occur on first sync. Continue?" — two buttons: Continue / Choose Different Folder
- [ ] Warning does not block — user may confirm and continue

### Unmounted Drive Handling (AM-11)
- [ ] At sync trigger time (not at configuration time), if the local sync folder's drive/mount is unavailable → sync fails with a clear user-visible message and a log entry at `Error` level
- [ ] The failure is reported per-account on the Dashboard

### Per-Account Settings — Power User Only (AM-05, AM-12)
- [ ] Power User sees: sync interval selector (5/15/30/60 min), concurrency slider (1–10, default 5), local sync path field, debug logging toggle, "Store file metadata in database" toggle
- [ ] Casual user sees none of these — they are **hidden** (not disabled) in Casual mode
- [ ] Settings changes persist to SQLite immediately on save

### File Metadata Flag (AM-12–AM-15)
- [ ] "Store file metadata in database" toggle: default OFF, Power User only, hidden from Casual users (AM-12)
- [ ] When flag is ON, after each sync run the app writes one `SyncedFileMetadata` row per synced file (OneDrive item ID, relative path, file name, size, SHA-256 checksum, last-modified UTC, created UTC) (AM-13)
- [ ] When flag is enabled on an account with an existing sync, an immediate backfill is triggered; backfill progress is displayed in the account settings panel (AM-14)
- [ ] When flag is disabled, existing `SyncedFileMetadata` rows are retained indefinitely — no automatic deletion (AM-15)

### Remove Account (AM-08, AM-09)
- [ ] Remove account prompts: "Keep local files" or "Delete local files" — two explicit choices; no silent default
- [ ] Removal is **blocked** while a sync is active for that account; user shown: "Please cancel the active sync before removing this account"
- [ ] On removal: account row deleted from DB (cascade deletes all related rows per S002 schema)
- [ ] If "Delete local files" chosen: local folder deleted recursively with a second confirmation ("This will permanently delete X files from your computer")

### Accounts View (Section 7)
- [ ] Accounts view shows all configured accounts with: display name, email (masked to `a***@example.com` for display), auth status badge, last-synced timestamp, "Sync Now" button, "Settings" button, "Remove" button
- [ ] Auth failure badge wired to `IAuthStateService` from S007
- [ ] Last-synced formatted per LO-07 (`IRelativeTimeFormatter`)

### Tests
- [ ] **Unit test**: `AddAccountWizardViewModel` — step progression; cannot advance from Step 1 until auth succeeds
- [ ] **Unit test**: overlap detection — overlapping paths return `Failure`; non-overlapping return `Success`
- [ ] **Unit test**: non-empty folder warning — empty folder → no warning; non-empty → warning emitted
- [ ] **Unit test**: removal blocked when sync active; allowed when idle
- [ ] **Integration test**: account added, persisted, and retrieved correctly from SQLite
- [ ] **Integration test**: account removed — all cascade-deleted rows confirmed absent
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `AddAccountWizardViewModel` is **transient** (new instance per wizard open)
- `AccountsViewModel` is **scoped** (one per navigation activation — or singleton if reactive updates are needed across the nav)
- `IOneDriveFolderService` is in `AStar.Dev.OneDrive.Client` package; fetches folders using the authenticated Graph client
- NF-16: all service methods return `Result<T>`; `AddAccountWizardViewModel` translates failures to user-visible error state
- NF-00: account add/remove/config-change events logged at `Information`
- All folder tree loading on a background thread — UI never blocks (NF-01)

---

## Implementation Constraints

- **`x:DataType` required on all DataTemplates** — the account list, folder tree, and wizard step views all use `ItemsControl` or `TreeView` with DataTemplates; every DataTemplate must declare `x:DataType` or it will produce a compiled-binding failure with `AvaloniaUseCompiledBindingsByDefault=true`.
- **Avalonia `TreeView` lazy loading** — Avalonia's `TreeView` uses `ItemsSource` for the root and `TreeViewItem.ItemsSource` for children. Lazy child loading requires each folder VM to expose an observable/command triggered by `IsExpanded`, not a recursive eager fetch on construction.
- **`ObservableCollection` updates on UI thread** — auth state and account list changes originate from background threads; every `.Subscribe()` that mutates an `ObservableCollection` or a reactive property must call `.ObserveOn(RxApp.MainThreadScheduler)` first.
- **Wizard VM resolved via factory** — `AddAccountWizardViewModel` is transient; register and inject `Func<AddAccountWizardViewModel>` rather than resolving directly from `IServiceProvider` in code-behind.
- **Folder picker via `StorageProvider`** — use `TopLevel.GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync()` for the local sync folder selection; `FolderBrowserDialog` is Windows-only and will not compile on Linux.
- **Dialogs via Avalonia dialog API** — confirmation dialogs (non-empty folder warning, account removal confirmation) must use Avalonia's dialog infrastructure; no `MessageBox.Show()`.
- **Register Accounts nav item** — `ShellServiceExtensions.RegisterAvailableFeatures()` must call `service.Register(NavSection.Accounts)` when this story ships.
---

## Dependencies

- S001 (project scaffolding)
- S002 (database — accounts and settings tables)
- S003 (navigation shell — Accounts nav item)
- S005 (localisation)
- S006 (onboarding — wizard triggered from onboarding CTA)
- S007 (authentication — step 1 of wizard)
