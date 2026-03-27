# Requirements Document: AStar.Dev OneDrive Sync Desktop Application

**Version:** 1.1 | **Date:** 2026-03-25 | **Status:** Draft — all open decisions resolved; ready for technical spikes

---

## 1. Executive Summary

A cross-platform desktop application to synchronise personal Microsoft OneDrive accounts to the local file system. Built with AvaloniaUI in the existing mono-repo. Reusable components (Graph client, sync engine, conflict resolution) extracted to `packages/` as NuGet packages. MVP targets Linux.

---

## 2. Goals

| Goal                         | Metric                                               | Target             |
| ---------------------------- | ---------------------------------------------------- | ------------------ |
| Reliable bi-directional sync | Sync runs completing without data loss               | 100%               |
| Multi-account support        | Concurrent accounts without degradation              | ≥ 5                |
| Non-technical UX             | User adds account and starts first sync without docs | < 3 minutes        |
| Background operation         | Scheduled syncs fire without manual intervention     | ✓                  |
| Conflict transparency        | No silent overwrites                                 | 100% user-resolved |

---

## 3. Non-Goals

- Entra ID / work/school accounts
- Shared OneDrive folders (files shared with the account by others)
- File-level sync selection (folders only)
- Web-based interface
- Automatic conflict resolution
- Mobile platforms
- Windows/macOS packaging in MVP

---

## 4. User Personas

**Alex (Power User)** — Developer with 3–5 personal OneDrive accounts. Wants automated sync, per-account tuning, and quick diagnostics.

**Sam (Casual User)** — Non-technical. One account. Wants set-and-forget. Must not encounter technical jargon.

---

## 5. Functional Requirements

### 5.1 Account Management

| ID    | Requirement                                                                                               | Phase |
| ----- | --------------------------------------------------------------------------------------------------------- | ----- |
| AM-01 | Add a personal Microsoft account via MSAL OAuth (system browser or embedded web view)                     | MVP   |
| AM-02 | 3-step add-account wizard: (1) authenticate, (2) optionally select folders, (3) confirm                   | MVP   |
| AM-03 | Wizard step 2 shows root OneDrive folders; UI allows expanding to arbitrary depth for subfolder selection | MVP   |
| AM-04 | Folder selection editable from main UI after wizard                                                       | MVP   |
| AM-05 | Per-account settings: sync interval, concurrency, local sync path, debug logging toggle                   | MVP   |
| AM-06 | Each account must have a unique, non-overlapping local sync folder                                        | MVP   |
| AM-07 | Platform-specific default local sync folder set on add (e.g. `~/OneDrive/<account>/`); editable           | MVP   |
| AM-08 | Remove account prompts user: keep or delete local synced folder                                           | MVP   |

### 5.2 Sync Engine

| ID    | Requirement                                                                                                                                                                                          | Phase |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| SE-01 | Bi-directional sync                                                                                                                                                                                  | MVP   |
| SE-02 | Default 8 concurrent up/downloads; configurable per account                                                                                                                                          | MVP   |
| SE-03 | Manual "Sync Now" per account from main UI                                                                                                                                                           | MVP   |
| SE-04 | Automatic sync on configurable timer; default 1 hour                                                                                                                                                 | MVP   |
| SE-05 | Multiple accounts: schedules staggered evenly (e.g. 4 accounts = every 15 mins)                                                                                                                      | MVP   |
| SE-06 | First sync downloads remote content; conflict resolution applies if local files already exist                                                                                                        | MVP   |
| SE-07 | Folder-level selection only; all files in selected folders (and descendants) are synced                                                                                                              | MVP   |
| SE-08 | Only one sync may run per account at a time. If a sync for an account is already in progress and a new one is triggered (manually or by schedule), the user is prompted to confirm before proceeding | MVP   |

### 5.3 Conflict Resolution

| ID    | Requirement                                                                                                                                                            | Phase |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| CR-01 | Conflict triggered by: same file modified both sides, or file deleted one side / present the other                                                                     | MVP   |
| CR-02 | User MUST manually resolve every conflict — no silent overwrites or deletions                                                                                          | MVP   |
| CR-03 | Resolution strategies per conflict: **Local Wins**, **Remote Wins**, **Keep Both**, **Skip**                                                                           | MVP   |
| CR-04 | "Keep Both" renames the conflicting copy as `original-name-(yyyy-MM-ddTHHmmssZ).ext` where the datetime is UTC                                                         | MVP   |
| CR-05 | "Skip" defers the conflict; the queue is persistent across sessions                                                                                                    | MVP   |
| CR-06 | The conflict list presents each conflict as a checkable item. The user can select any subset of conflicts and apply a single resolution strategy to all selected items | MVP   |
| CR-07 | A "Select All" button selects all conflicts in the current list                                                                                                        | MVP   |
| CR-08 | Once a resolution strategy is applied to a conflict, it cascades to all matching pending conflicts across sessions (same file path)                                    | MVP   |

### 5.4 Authentication & Token Management

| ID    | Requirement                                                                                                                                                                                                                                            | Phase |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----- |
| AU-01 | MSAL for personal Microsoft accounts only                                                                                                                                                                                                              | MVP   |
| AU-02 | Tokens persisted locally; no re-auth on every launch                                                                                                                                                                                                   | MVP   |
| AU-03 | On Linux, if the OS keychain is unavailable, the app presents an explicit opt-in consent dialog before falling back to an insecure local token store (machine-scoped encryption key, not plaintext). The user's consent decision is stored per account | MVP   |
| AU-04 | Token refresh is silent; user prompted only if automation fails                                                                                                                                                                                        | MVP   |

### 5.5 Crash Recovery

| ID    | Requirement                                                                     | Phase |
| ----- | ------------------------------------------------------------------------------- | ----- |
| RR-01 | Interrupted sync (crash, network drop) detected on next launch or scheduled run | MVP   |
| RR-02 | Resume from interruption point where possible                                   | MVP   |
| RR-03 | If resume impossible, user informed with clear explanation                      | MVP   |

### 5.6 System Tray & Background Operation

| ID    | Requirement                                                                                | Phase |
| ----- | ------------------------------------------------------------------------------------------ | ----- |
| ST-01 | Closing main window keeps app running as system tray process                               | MVP   |
| ST-02 | Tray context menu includes: Open, per-account Sync Now entries, and Quit                   | MVP   |
| ST-03 | OS-level notifications when app not in foreground: sync complete, conflict detected, error | MVP   |
| ST-04 | App launches at OS startup; delayed start where supported                                  | MVP   |

### 5.7 Logging & Diagnostics

| ID    | Requirement                                                                                          | Phase |
| ----- | ---------------------------------------------------------------------------------------------------- | ----- |
| LG-01 | Debug logging per account: off = errors/warnings only; on = verbose                                  | MVP   |
| LG-02 | Logs MUST NEVER contain PII or GDPR-restricted data                                                  | MVP   |
| LG-03 | Hidden log viewer accessible from anywhere via CTRL+SHIFT+ALT+L                                      | MVP   |
| LG-04 | Log viewer: all errors across all accounts, unified, scrollable, filterable                          | MVP   |
| LG-05 | Log files written to platform-appropriate paths (e.g. `~/.local/share/AStar.Dev.OneDriveSync/logs/`) | MVP   |

### 5.8 Application Updates

| ID    | Requirement                                                                                  | Phase |
| ----- | -------------------------------------------------------------------------------------------- | ----- |
| UP-01 | App checks GitHub Releases for available updates on a schedule                               | MVP   |
| UP-02 | Update notification with option to install now or defer                                      | MVP   |
| UP-03 | Deferred update re-prompts every hour                                                        | MVP   |
| UP-04 | After 7 days of deferral, update is forced; user warned before it is applied                 | MVP   |
| UP-05 | Any release published within 12 months of current date is accepted as a valid latest version | MVP   |
| UP-06 | A forced update (UP-04) waits for any active sync to complete before applying                | MVP   |

### 5.9 Theming & Internationalisation

| ID    | Requirement                                                                | Phase    |
| ----- | -------------------------------------------------------------------------- | -------- |
| TI-01 | Light, Dark, Auto (follows OS) theme modes                                 | MVP      |
| TI-02 | Every UI element themable via resource dictionaries — no hardcoded colours | MVP      |
| TI-03 | All user-facing strings externalised (i18n infrastructure from day one)    | MVP      |
| TI-04 | MVP locale: UK English (`en-GB`)                                           | MVP      |
| TI-05 | Accessible colour schemes (high contrast, colour-blind-friendly)           | Post-MVP |
| TI-06 | Additional locale translations                                             | Post-MVP |

---

## 6. Non-Functional Requirements

| ID    | Requirement                                                                                                |
| ----- | ---------------------------------------------------------------------------------------------------------- |
| NF-01 | Sync I/O runs on background threads; UI never freezes                                                      |
| NF-02 | Main UI input latency < 100ms during active sync                                                           |
| NF-03 | Memory stable during long-running syncs                                                                    |
| NF-04 | Zero tolerance for silent data loss                                                                        |
| NF-05 | Persistent conflict queue survives crashes without corruption (atomic writes)                              |
| NF-06 | Tokens encrypted at rest; insecure fallback uses machine-scoped key, never plaintext                       |
| NF-07 | No PII in logs                                                                                             |
| NF-08 | All Graph API communication over HTTPS                                                                     |
| NF-09 | TDD conventions; all reusable packages have `.Tests.Unit` projects                                         |
| NF-10 | Sync engine, conflict logic, Graph client testable in isolation (DI, interfaces, `System.IO.Abstractions`) |
| NF-11 | All interactive controls keyboard-navigable                                                                |
| NF-12 | All controls expose automation properties for screen readers                                               |

---

## 7. Architecture Notes

### Suggested Repo Layout

| Component             | Location                                                                     |
| --------------------- | ---------------------------------------------------------------------------- |
| Desktop app           | `apps/desktop/AStar.Dev.OneDriveSync/`                                       |
| Graph/OneDrive client | `packages/infra/astar-dev-onedrive-client/AStar.Dev.OneDrive.Client/`        |
| Sync engine           | `packages/core/astar-dev-sync-engine/AStar.Dev.Sync.Engine/`                 |
| Conflict resolution   | `packages/core/astar-dev-conflict-resolution/AStar.Dev.Conflict.Resolution/` |

### Key Observations

- **Framework:** `net10.0`, Avalonia `11.3.12`, ReactiveUI (MVVM), Serilog, EF Core SQLite — all already in the repo
- **CPM:** New dependencies (MSAL, Graph SDK, Kiota) must be added to `Directory.Packages.props`
- **Self-contained builds:** Desktop `Directory.Build.props` already configures trimmed, single-file output for all 6 RIDs — MSAL/Graph SDK trim compatibility needs early verification
- **New dependencies needed:** `Microsoft.Identity.Client`, `Microsoft.Graph` (or Kiota), a Linux-compatible Avalonia tray library
- **Update mechanism:** GitHub Releases API; update is distributed as an AppImage (see OD-08 below)
- **File size:** No app-level upload limit; honour OneDrive's own API limits (currently 250 GB per file for chunked upload)

### Packaging (OD-08 — Resolved: AppImage)

MVP Linux packaging is **AppImage**. Rationale:

- Matches the existing self-contained single-file build output with no changes to the build config
- No installation or root access required; works across all Linux distros and desktop environments
- Distributes directly from GitHub Releases
- OS startup registration via a `.desktop` file in `~/.config/autostart/` — no root required
- Update mechanism downloads the new AppImage and replaces the existing one in-place

---

## 8. Risks

| Risk                                                         | Impact                   | Likelihood | Mitigation                                                               |
| ------------------------------------------------------------ | ------------------------ | ---------- | ------------------------------------------------------------------------ |
| Linux system tray inconsistent across DEs (GNOME, KDE, Sway) | ST-01–04 may degrade     | High       | Spike early; accept graceful degradation (fall back to taskbar presence) |
| MSAL token cache unreliable on Linux                         | Frequent re-auth prompts | Medium     | AU-03 fallback; test both paths early                                    |
| Graph API rate limiting during large initial syncs           | Throttled first sync     | Medium     | Exponential backoff, respect `Retry-After` headers                       |
| Trim compatibility of MSAL/Graph SDK                         | Release build failures   | Medium     | Test trimmed builds early; add trimmer root descriptors as needed        |
| Forced update (UP-04) interrupts active sync                 | Data loss risk           | Low        | UP-06 requires sync to complete before update is applied                 |
| Bi-directional sync race conditions per account              | Corruption               | Medium     | SE-08 enforces single-sync-at-a-time per account                         |

---

## 9. Resolved Decisions

All open decisions from v1.0 have been resolved. Decisions are incorporated into the relevant requirements above. The table below is retained for traceability.

| ID    | Decision                                     | Resolved As                                                                                           |
| ----- | -------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| OD-01 | "Keep Both" datetime format                  | UTC `yyyy-MM-ddTHHmmssZ` — see CR-04                                                                  |
| OD-02 | Conflict resolution selection model          | Checkbox per conflict; subset or Select All; strategy applies to all checked items — see CR-06, CR-07 |
| OD-03 | Quota exceeded during upload                 | Error surfaced to user; user resolves manually; no automatic retry                                    |
| OD-04 | Include shared OneDrive folders?             | No — excluded from scope                                                                              |
| OD-05 | Update distribution mechanism                | GitHub Releases — see UP-01                                                                           |
| OD-06 | Maximum file size                            | No app-level limit; follow OneDrive API limits                                                        |
| OD-07 | Insecure token fallback — opt-in or warning? | Opt-in consent dialog; decision stored per account — see AU-03                                        |
| OD-08 | Linux packaging format for MVP               | AppImage — see Architecture Notes section 7                                                           |
| OD-09 | "Sync Now" — per account or all accounts?    | Per account; concurrent sync on same account requires user confirmation — see SE-03, SE-08            |
| DB-01 | DateTime storage type in SQLite              | All `DateTimeOffset` columns stored as Unix milliseconds (`long`) via EF Core value converters. SQLite has no native datetime type that supports `DateTimeOffset` ORDER BY / WHERE server-side; storing as `long` enables full server-side sorting and filtering while keeping C# model properties typed as `DateTimeOffset`. |
| DB-02 | EF Core entity configuration pattern         | Each entity has its own `IEntityTypeConfiguration<T>` class in a `Configurations/` folder. `OnModelCreating` calls `ApplyConfigurationsFromAssembly` — no manual registration, new configurations are picked up automatically. |
| DB-03 | Schema initialisation at startup             | `Database.MigrateAsync()` is called at application startup. This creates the database if absent and applies any pending migrations. `EnsureCreatedAsync` is not used as it bypasses migration history and cannot evolve the schema. An `IDesignTimeDbContextFactory<T>` is provided for `dotnet ef` tooling. |

---

## 10. Assumptions to Validate (Technical Spikes)

1. Microsoft Graph delta query is sufficient for incremental sync (changed files only)
2. Avalonia 11.3.12 supports system tray on Linux via a community package
3. MSAL public client flow works on Linux without a local HTTP redirect server (or one is trivial to add)
4. MSAL and Graph SDK are trim-compatible with the existing build config
5. SQLite via EF Core is the right store for conflict queue, sync state, and delta tokens
6. "Personal Microsoft account" = `consumers` tenant — no multi-tenant requirements

---

## 11. Phased Delivery

### MVP — Linux (x64 + arm64)

All requirements marked MVP above, plus all NFRs.

### Post-MVP

- Windows packaging and release
- macOS packaging and release
- Accessible colour scheme variants
- Additional locale translations
- Bandwidth throttling per account
- Entra ID / work account support
- Per-file sync progress UI

---

## Next Steps

1. **Run technical spikes** on the 6 assumptions in section 10 before sprint planning
2. **Audit new NuGet dependencies** for trim compatibility (`Microsoft.Identity.Client`, `Microsoft.Graph` / Kiota, tray library)
3. **Convert FRs to backlog items** for sprint planning
