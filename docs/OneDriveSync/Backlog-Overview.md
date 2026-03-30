# OneDrive Sync — Backlog Overview

**Spec:** [ONEDRIVE-SPEC.md](../ONEDRIVE-SPEC.md)
**App:** `apps/desktop/AStar.Dev.OneDriveSync`
**Last updated:** 2026-03-27

---

## MVP Stories (delivery order)

| #    | Story                                                                                          | Dependencies                       | Spec Refs                                  | Completed |
| ---- | ---------------------------------------------------------------------------------------------- | ---------------------------------- | ------------------------------------------ | --------- |
| S001 | [Project Scaffolding & Solution Structure](S001-Project-Scaffolding-And-Solution-Structure.md) | None                               | Section 9                                  | ✅        |
| S002 | [Database Foundation (SQLite / EF Core / Migrations)](S002-Database-Foundation.md)             | S001                               | Section 8, DB-01–07, AM-12–15, EH-07–08, NF-17 |           |
| S003 | [Navigation Shell & Application Lifecycle](S003-Navigation-Shell-And-App-Lifecycle.md)         | S001, S002                         | Section 7, AL-01–02, NF-15                 |           |
| S004 | [Theming (Light / Dark / Auto)](S004-Theming.md)                                               | S001, S002, S003                   | TH-01–06                                   |           |
| S005 | [Localisation Foundation](S005-Localisation-Foundation.md)                                     | S001, S002, S003                   | LO-01–07                                   |           |
| S006 | [Onboarding & Help](S006-Onboarding-And-Help.md)                                               | S001, S002, S003, S005             | OH-01–06, Section 5                        |           |
| S007 | [Authentication & Token Management](S007-Authentication-And-Token-Management.md)               | S001, S002                         | AU-01–05, NF-06                            |           |
| S008 | [Account Management](S008-Account-Management.md)                                               | S001, S002, S003, S005, S006, S007 | AM-01–15                                   |           |
| S009 | [OneDrive Client Package](S009-OneDrive-Client-Package.md)                                     | S001, S007                         | Section 9, SE-09, SE-10, SE-12, AM-03      |           |
| S010 | [Sync Engine Core](S010-Sync-Engine-Core.md)                                                   | S001, S002, S007, S009             | SE-01–15, EH-01–08, NF-01–05               |           |
| S011 | [Conflict Resolution](S011-Conflict-Resolution.md)                                             | S001, S002, S003, S005, S010       | CR-01–08, NF-04–05                         |           |
| S012 | [Dashboard & Sync UI](S012-Dashboard-And-Sync-UI.md)                                           | S001, S003, S005, S008, S010       | SE-03–06, SE-10, SE-13–15, EH-05–06, AM-11 |           |
| S013 | [Activity View](S013-Activity-View.md)                                                         | S001, S003, S005, S010             | Section 7 (Activity)                       |           |
| S014 | [Log Viewer](S014-Log-Viewer.md)                                                               | S001, S003, S005, S006, S008       | LG-01–07                                   |           |
| S015 | [Settings View](S015-Settings-View.md)                                                         | S001, S002, S003, S004, S005, S006 | Section 5/7, TH-01, LO-03, ST-03           |           |
| S016 | [System Tray & Background Operation](S016-System-Tray-And-Background-Operation.md)             | S001, S003, S008, S010, S015       | ST-01–05                                   |           |
| S017 | [Application Updates & About Screen](S017-Application-Updates.md)                              | S001, S002, S003, S010             | UP-01–06, Section 7 (About)                |           |

---

## Post-MVP Stories

| #    | Story                                                                                   | Dependencies | Spec Refs          |
| ---- | --------------------------------------------------------------------------------------- | ------------ | ------------------ |
| S018 | [Resumable Downloads](S018-Post-MVP-Resumable-Downloads.md)                             | S009, S010   | SE-16              |
| S019 | [Accessibility & Advanced Theming](S019-Post-MVP-Accessibility-And-Advanced-Theming.md) | S004, S003   | TH-07–09, NF-11–12 |
| S020 | [Additional Locale Translations](S020-Post-MVP-Additional-Localisation.md)              | S005         | LO-08              |

---

## Dependency Graph

```
S001
 ├─► S002
 │    ├─► S003
 │    │    ├─► S004
 │    │    ├─► S005
 │    │    │    └─► S006 ─────────────────────────────► S008
 │    │    ├─► S006                                        │
 │    │    └─► S015                                        │
 │    └─► S007 ──────────────────────► S009               │
 │         │                            └─► S010          │
 │         └──────────────────────────────► S010          │
 │                                           ├─► S011     │
 │                                           ├─► S012 ◄───┘
 │                                           ├─► S013
 │                                           └─► S016
 └─► S017 (also depends on S002, S003, S010)
```

## Suggested delivery sequence based on dependencies:

1. S001 → S002 → S003 — Get the shell compiling and navigable first
2. S004 + S005 in parallel — Theming and localisation are independent once the shell exists
3. S006 + S007 in parallel — Onboarding and auth don't depend on each other
4. S009 — OneDrive Client package (unblocks S008 and S010)
5. S008 — Account management (needs S007 + S009)
6. S010 — Sync engine core (the heaviest story — worth starting early once S009 is done)
7. S011 + S012 + S013 + S014 in parallel — All depend on S010 but are independent of each other
8. S015 → S016 → S017 — Settings, then tray, then updates (light sequential chain)

S010 is the highest-risk story given its scope — if you want to de-risk early, consider starting S009 and S010 as soon as S001/S002 are done, even before the full UI layer is in place.

---

## Out-of-Scope Reminder (Non-Goals from Spec)

- Entra ID / work accounts
- File-level sync selection (folders only)
- Real-time file system watching
- Automatic conflict resolution
- Telemetry / crash reporting
- Self-updating AppImage (MVP uses manual download)
- Windows / macOS packaging in MVP
- Shared OneDrive folders

---

## Spec Requirements Coverage

| Section                     | Requirements        | Story                    |
| --------------------------- | ------------------- | ------------------------ |
| 6.1 Onboarding & Help       | OH-01–06            | S006                     |
| 6.2 Account Management      | AM-01–15            | S008 (AM-12–15 schema in S002) |
| 6.3 Sync Engine             | SE-01–15 (MVP)      | S010, S012               |
| 6.3 Sync Engine             | SE-16 (Post-MVP)    | S018                     |
| 6.4 Conflict Resolution     | CR-01–08            | S011                     |
| 6.5 Authentication          | AU-01–05            | S007                     |
| 6.6 Error Handling          | EH-01–08 (MVP)      | S010 (engine), S012 (UI) |
| 6.7 System Tray             | ST-01–05 (MVP)      | S016                     |
| 6.8 App Lifecycle           | AL-01–02 (MVP)      | S003                     |
| 6.9 App Updates             | UP-01–06 (MVP)      | S017                     |
| 6.10 Theming                | TH-01–06 (MVP)      | S004                     |
| 6.10 Theming                | TH-07–09 (Post-MVP) | S019                     |
| 6.11 Localisation           | LO-01–07 (MVP)      | S005                     |
| 6.11 Localisation           | LO-08 (Post-MVP)    | S020                     |
| 6.12 Logging                | LG-01–07 (MVP)      | S014                     |
| Section 8 Data Architecture | DB-01–07, NF-17     | S002                     |
| Navigation Shell            | Section 7           | S003                     |
| OneDrive Client             | Section 9           | S009                     |
