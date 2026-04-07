# OneDriveSync — Implementation Issues

PRD source: [PRD.md](PRD.md) | Generated: 2026-04-07

Legend: **[MISSING]** feature absent · **[WRONG]** implemented incorrectly · **[NO TESTS]** coverage gap · **[NO STORY]** no story covers this PRD requirement

---

## S002 — Database Foundation

**[MISSING]** EH-08: "Start Fresh" recovery option not implemented.
`DatabaseMigrationStartupTask` re-throws on DB corruption and `App.axaml.cs` sets a static error message, but no code path deletes `file-data.db` and re-runs migrations.
_Resolution_: implement `StartFreshCommand` (delete DB file via `IFileSystem`, then re-invoke `MigrateAsync`); wire to the startup error panel in `MainWindowViewModel`.

**[NO TESTS]** Unit test "file metadata only written to `SyncedFileMetadata` when AM-12 flag is ON" is explicitly deferred (unchecked AC).
_Resolution_: add test to integration suite once S008 AM-12 is implemented.

---

## S008 — Account Management

**[MISSING]** AM-04: Folder selection not editable post-wizard.
`AccountsViewModel` has no command to re-open folder selection on an existing account.
_Resolution_: add "Edit Folders" per-row button in `AccountRowViewModel`; wire to a command that opens `AddAccountWizardViewModel` pre-populated with the existing account.

**[MISSING]** AM-05 / AM-12: Per-account Power User settings UI absent (sync interval, concurrency, debug logging, metadata toggle).
`Account` entity has all backing fields; no UI panel exposes them and no show/hide logic based on user type exists.
_Resolution_: add a collapsible account settings panel in `AccountsView`, visible only when `IUserTypeService.CurrentUserType == PowerUser`.

**[MISSING]** AM-08: Remove account does not prompt "Keep local files" / "Delete local files".
`AccountsViewModel.RemoveAccountAsync` calls `_accountRepository.RemoveAsync` directly with no dialog.
_Resolution_: show a three-option Avalonia dialog before `RemoveAsync`; if "Delete" chosen, show second confirmation then recursively delete the local folder.

**[MISSING]** AM-10: Non-empty folder warning not shown during wizard.
`ILocalSyncPathService.IsNonEmpty` exists and is tested but `AddAccountWizardViewModel.FinishAsync` never calls it.
_Resolution_: call `_pathService.IsNonEmpty(LocalSyncPath)` in `FinishAsync`; if true, request confirmation via `IDialogService` before saving.

**[MISSING]** AM-11: Unmounted drive handling at sync trigger time not wired in `AccountsViewModel`.
The "Sync Now" handler in `AccountsViewModel` is a no-op stub (`onSyncNow: _ => { }`); `LocalPathUnavailableError` is handled only in `DashboardViewModel`.
_Resolution_: implement the sync-now handler in `AccountsViewModel` to call `ISyncEngine.StartSyncAsync` and surface `LocalPathUnavailableError` on the account row.

**[MISSING]** AM-13/AM-14/AM-15: File metadata write-back and AM-14 backfill not implemented.
`SyncedFileMetadata` entity and EF configuration exist; the sync engine does not write metadata rows after sync and no backfill path exists.
_Resolution_: after `SyncEngine.RunSyncAsync` completes, check `account.StoreFileMetadata` and upsert `SyncedFileMetadata` rows. For AM-14, detect the flag-flip transition and trigger the same backfill; display backfill progress in the account settings panel.

**[NO TESTS]** Non-empty folder warning: no test asserts `IDialogService.ConfirmAsync` is called when `ILocalSyncPathService.IsNonEmpty` returns `true`.
_Resolution_: add test to `GivenAnAddAccountWizardViewModel`.

---

## S010 — Sync Engine Core

**[MISSING]** EH-03 (partial): disk write failure mid-sync not handled.
`SyncEngine.ProcessDeltaItemAsync` does not wrap `IFileTransferService` calls in `catch (IOException)`.
_Resolution_: catch `IOException` in `ProcessDeltaItemAsync`, set `SyncAccountState.Failed`, log at `Error`, return a `SyncReport` with `HasErrors = true`.

**[NO TESTS]** Integration test: interrupted sync state persists, detected on next trigger, resume attempted.
`GivenAnInterruptedSync` is unit-level with mocked `ISyncStateStore`; no test hits a real SQLite DB.
_Resolution_: add an integration test using `SqliteSyncStateStore` backed by a temp-file SQLite database.

---

## S012 — Dashboard & Sync UI

**[MISSING]** SE-15 (partial): toast does not link to Log Viewer filtered to the relevant account.
`AvaloniaToastService.Show` stores the message but has no navigation callback.
_Resolution_: extend `IToastService` with an optional `NavigateTo` callback; wire in `DashboardViewModel` to call `IShellNavigator.Navigate(NavSection.LogViewer)` with the account ID pre-set.

---

## S014 — Log Viewer

**[WRONG]** All S014 ACs are unchecked in the story file, but the implementation is substantively complete (`LogViewerViewModel`, `InMemoryLogSink`, unit tests all exist). Story file not updated to reflect completion.
_Resolution_: mark completed ACs as done in [S014-Log-Viewer.md](S014-Log-Viewer.md).

**[MISSING]** LG-04: Account filter dropdown not reactive — loaded once at construction, not updated when accounts are added/removed while the view is open.
_Resolution_: subscribe to an account-change observable (or reload on navigation activation) to keep the filter dropdown in sync.

---

## S015 — Settings View

**[WRONG]** All S015 ACs are unchecked in the story file, but the implementation is complete (`SettingsViewModel`, `SettingsView.axaml`, unit tests all pass). Story file not updated.
_Resolution_: mark completed ACs as done in [S015-Settings-View.md](S015-Settings-View.md).

---

## S016 — System Tray & Background Operation

**[MISSING]** Entire story unimplemented. No `ITrayService`, `TrayService`, autostart `.desktop` file writer, or OS notification dispatcher exists anywhere in the codebase.
_Resolution_: implement `ITrayService` in `Infrastructure/Tray/`; set `Application.ShutdownMode = OnExplicitShutdown`; intercept `MainWindow.Closing`; write `.desktop` to `~/.config/autostart/`; dispatch OS notifications via `libnotify` or the Avalonia community tray package.

---

## S017 — Application Updates

**[MISSING]** Entire story unimplemented. No `UpdateCheckService`, `IUpdateCheckService`, About view content (current `AboutView.axaml` is a "Coming Soon" stub), update banner, forced-update screen, or deferral persistence in `AppSettings` exists.
_Resolution_: implement `UpdateCheckService` using `IHttpClientFactory`; add `UpdateFirstDeferredAt` / `UpdateDeferralCount` to `AppSettings`; implement forced-update shell lockdown via `IShellStateService`; build out `AboutViewModel` with version display and update status; register `NavSection.About` in `RegisterAvailableFeatures` when complete.

---

## No Story — PRD Requirements Without Coverage

**[NO STORY]** PRD §6.5 AU-03: Insecure fallback consent dialog.
`ConsentStore` entity and tests exist, but no UI flow presents the consent dialog before falling back to the encrypted local token store. `TokenValidationStartupTask` is a stub.
_Resolution_: either create a new story (S007 extension or new S021) covering the startup token-validation UX and insecure-fallback consent prompt.

**[NO STORY]** PRD §8 required integration test: "Inserting data into any non-Account table with a real email/name/Microsoft ID fails (schema enforcement)."
The test `GivenAnAccountWithAMicrosoftIdentity` exists but only checks that the `MicrosoftAccountId` is stored on the Account table; it does not assert a schema-level FK constraint failure when invalid data is attempted elsewhere.
_Resolution_: add an integration test that attempts to insert a non-Guid string as `AccountId` in `ConflictRecords` / `SyncStateRecords` and asserts constraint violation.

**[NO STORY]** PRD §8 required integration test: "Backfill populates metadata for all locally-synced files when the AM-12 flag is turned on." No test exists (AM-14 not implemented).
_Resolution_: add integration test after AM-14 is implemented.

**[NO STORY]** PRD §9 Feature-slice architecture violation — Conflict Resolution package has a `Domain/` folder (`ConflictRecord`, `ConflictType`, `ResolutionStrategy`, error types). PRD mandates grouping by business feature, not technical type.
_Resolution_: move domain types into the owning feature folders (`Features/Detection/`, `Features/Resolution/`, `Features/Persistence/`); delete the `Domain/` folder.

**[NO STORY]** PRD §9 — Duplicate `IDbBackupService` interface: one exists in `packages/core/astar-dev-sync-engine/.../Infrastructure/IDbBackupService.cs` and another in `apps/desktop/.../Infrastructure/Persistence/IDbBackupService.cs`. Two identical contracts in different assemblies.
_Resolution_: decide which assembly owns the interface (likely the desktop app's persistence layer); remove the duplicate from the sync engine package and have the engine take a dependency on the interface via injection.

**[NO STORY]** PRD §6.6 EH-07 / structured logging violation — `DatabaseMigrationStartupTask.RunAsync` contains a bare `Console.WriteLine(e)` before re-throwing. Violates NF-00 (all logging via Serilog structured templates).
_Resolution_: replace `Console.WriteLine(e)` with a `[LoggerMessage]`-decorated partial method call (e.g., `LogMigrationFailed(_logger, ex)`).

---

## Code Quality — Cross-Cutting

**[WRONG]** `AccountsViewModel.RemoveAccountAsync`: removes account from DB without re-checking `account.IsSyncActive`. UI disables the button, but if sync state changes between render and async completion, a sync could be orphaned.
_Resolution_: re-fetch account state inside `RemoveAsync`; return early with an error if `IsSyncActive` is still `true`.

**[WRONG]** `AccountsViewModel.OpenWizard()`: `wizard.WhenAnyValue(...).Subscribe(...)` subscriptions not stored; `IDisposable` leaked if wizard is cancelled quickly.
_Resolution_: store subscriptions in a `CompositeDisposable`; dispose when `CloseWizard()` is called.

**[WRONG]** `SettingsViewModel` constructor: `await dialogService.ConfirmAsync(..., CancellationToken.None)` — `CancellationToken.None` used rather than a propagated token.
_Resolution_: pass app lifetime `CancellationToken` (or `TestContext.Current.CancellationToken` in tests) to `ConfirmAsync`.

**[WRONG]** `MainWindowViewModel` (subscription on line 44): `shellNavigator.Subscribe(section => Navigate(section))` — `IDisposable` not stored. Acceptable for singleton lifetime but deviates from the pattern used by all other subscriptions in the same class.
_Resolution_: store in a `CompositeDisposable` for consistency.
