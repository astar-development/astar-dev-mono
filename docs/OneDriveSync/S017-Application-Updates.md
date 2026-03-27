# S017 — Application Updates

**Phase:** MVP  
**Area:** Infrastructure/Updates + Features/About  
**Spec refs:** UP-01 to UP-06, Section 7 (About screen)

---

## User Story

As a user,  
I want the app to check for updates at startup, notify me with release notes when one is available, and block me gracefully if my version is over 7 days past forced-update,  
So that I stay on a supported version without the app surprising me mid-task.

---

## Acceptance Criteria

### Update Check at Startup (UP-01)
- [ ] `UpdateCheckService` checks GitHub Releases API on a background thread at startup
- [ ] Check does not block startup or UI (NF-01)
- [ ] Any release within 12 months of today is a valid latest version (UP-05); older releases are ignored
- [ ] `CancellationToken` from app lifetime — check cancelled on shutdown

### Update Notification (UP-02)
- [ ] If a newer version is available: non-blocking banner shown in the About view and on the Dashboard (or notification area)
- [ ] Banner shows: new version number, release notes summary (first paragraph of GitHub release body), "Download" button → opens GitHub Releases page in system browser
- [ ] "Dismiss" / "Remind me later" defers the notification

### Deferred Update Re-prompt (UP-03)
- [ ] Every 1 hour after deferral, the update notification is re-shown if the app is still running

### Forced Update (UP-04, UP-06)
- [ ] After 7 consecutive days of deferral: the app enters "force update" mode
- [ ] Force update mode: all navigation is blocked; a full-screen "Please update to continue" view is shown with a "Download" button
- [ ] Force update **waits for any active sync to complete** before blocking the UI (UP-06) — syncs are allowed to finish but no new syncs can start once force update is triggered
- [ ] Deferral count and first-deferral timestamp persisted in SQLite

### About Screen (Section 7)
- [ ] App version displayed from `Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()`
- [ ] Local dev: displays `0.0.1-local (Development Build)`
- [ ] Release: displays tag version (e.g., `1.2.3`)
- [ ] Shows current update status: "Up to date" / "Update available (v1.2.3)" / "Update required"
- [ ] Links: GitHub repo, release notes, "Check for updates now" manual trigger

### Tests
- [ ] **Unit test**: `UpdateCheckService` — GitHub API returns version newer than current → `UpdateAvailable` result
- [ ] **Unit test**: `UpdateCheckService` — GitHub API returns release > 12 months old → treated as no valid release
- [ ] **Unit test**: `UpdateCheckService` — GitHub API returns same version → `UpToDate` result
- [ ] **Unit test**: force update trigger — deferral count ≥ 7 days → `ForceUpdateRequired` state; active syncs complete before block
- [ ] **Unit test**: force update waits — active sync in progress → UI block deferred until `SyncCompleted` event
- [ ] `dotnet build` zero errors/warnings; `dotnet test` all pass

---

## Technical Notes

- `UpdateCheckService` registered as **singleton**; uses `HttpClient` (injected via `IHttpClientFactory`) — no direct `new HttpClient()`
- Version comparison: `System.Version` or `NuGet.Versioning.SemanticVersion` — document the choice
- GitHub Releases API: `GET https://api.github.com/repos/astar-development/[repo]/releases/latest` (or `/releases` for all)
- Deferral state stored in SQLite `AppSettings` table: `UpdateFirstDeferredAt (DateTimeOffset nullable)`, `UpdateDeferralCount (int)`
- NF-16: `UpdateCheckService` returns `Result<UpdateCheckResult>`
- NF-00: update check start/result/error logged at `Information`/`Warning`

---

## Dependencies

- S001 (project scaffolding)
- S002 (database — deferral state persisted)
- S003 (navigation shell — force update blocks navigation)
- S010 (sync engine — force update waits for active syncs)
