## Why

Synced files have no semantic metadata beyond their path. Downstream apps (e.g. a file search UI) need to find files by user-defined concepts like "red", "car", or "landscape" — but cannot query path strings meaningfully. Classification translates folder/filename structure into queryable tags at sync time.

## What Changes

- New `FileClassificationRule` config block in `appsettings.json` — global, not per-account
- New `FileClassifier` service — pure path-token matching, no DB dependency
- New `SyncedItemClassificationEntity` DB table — one-to-many off `SyncedItemEntity`
- `SyncedItemRegistrar` extended to classify files at registration time
- EF Core migration for the new table
- Classification is read-only from the sync client's perspective — no routing, priority, or policy changes

## Capabilities

### New Capabilities

- `file-classification`: Tokenise a remote file path, match tokens against configured keyword rules, persist resulting tags to the database alongside the synced item

### Modified Capabilities

- `synced-item-registration`: `SyncedItemRegistrar` gains a classification step — tags are written when a file item is registered or upserted

## Impact

- **`apps/desktop/AStar.Dev.OneDrive.Sync.Client`**: new domain types, new DB entity, new service, extended registrar, new migration
- **`appsettings.json`**: new `FileClassificationRules` config section
- **No changes** to sync pipeline logic, conflict resolution, upload/delete paths, or any other app
- **Additive only** — existing behaviour unchanged; classification is a new side-effect of file registration
