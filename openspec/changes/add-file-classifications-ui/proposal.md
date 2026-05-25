## Why

File classification rules exist in the database and drive how synced files are tagged, but there is no UI to manage them — users must manipulate the SQLite database directly. A management UI lets users add and remove rules without leaving the app.

## What Changes

- Add `AddAsync` and `DeleteAsync` operations to `IFileClassificationRuleRepository` and its implementation.
- Add a `FileClassificationsViewModel` that loads all rules on startup, exposes an add command, and an inline delete command per rule.
- Add a `FileClassificationsView` Avalonia UserControl that lists current rules and provides a form to add a new one; every mutation persists immediately with no separate Save step.
- Wire the new view into the existing `SettingsView` as a new "File classifications" section (or as a dedicated settings tab/page if the section grows large).

## Capabilities

### New Capabilities

- `file-classification-rules-management`: CRUD UI for `FileClassificationRule` records — list all rules, add a new rule (Keywords + Level1 + optional Level2/Level3 + IsSpecial), delete an existing rule; all mutations persisted immediately via the repository.

### Modified Capabilities

- `file-classification`: Repository contract extends with `AddAsync` and `DeleteAsync`; existing `GetAllAsync` behaviour is unchanged.

## Impact

- `AStar.Dev.OneDrive.Sync.Client` — new View/ViewModel files, updated repository interface and implementation.
- `AStar.Dev.OneDrive.Sync.Client.Tests.Unit` — new ViewModel tests; repository tests extended.
- No API surface changes; no NuGet package changes.
