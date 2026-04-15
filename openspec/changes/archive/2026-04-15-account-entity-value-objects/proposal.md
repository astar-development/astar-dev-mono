## Why

`AccountEntity` and all related entities (`SyncFolderEntity`, `SyncJobEntity`, `SyncConflictEntity`) use raw `string` for domain identifiers (`AccountId`, `OneDriveFolderId`, `OneDriveItemId`) and a raw `string` for `LocalSyncPath`. This violates the primitive-obsession rule in `c-sharp-code-style.md` and allows category errors (passing a folder ID where an account ID is expected) to compile and fail silently at runtime.

## What Changes

- Add `AStar.Dev.Source.Generators` + `AStar.Dev.Source.Generators.Attributes` project references to `AStar.Dev.OneDrive.Sync.Client.csproj`
- Introduce `AccountId` as `[StrongId(typeof(string))]` partial record struct
- Introduce `OneDriveFolderId` as `[StrongId(typeof(string))]` partial record struct
- Introduce `OneDriveItemId` as `[StrongId(typeof(string))]` partial record struct
- Introduce `LocalSyncPath` as an immutable value-object record wrapping a validated string path
- Replace all primitive usages in entities, model classes, repository interfaces/implementations, EF configurations, and ViewModels
- Add EF Core value converters for all new types (SQLite persists underlying `string`)
- Add a new EF migration

## Capabilities

### New Capabilities

- `strongly-typed-ids`: Introduce `AccountId`, `OneDriveFolderId`, and `OneDriveItemId` strongly-typed identifier structs via `[StrongId(typeof(string))]` and wire them through entities, repositories, and domain models.
- `local-sync-path-value-object`: Introduce `LocalSyncPath` immutable record with validated construction; replaces raw `string` in `AccountEntity` and `OneDriveAccount`.

### Modified Capabilities

- none

## Impact

**Entities:** `AccountEntity`, `SyncFolderEntity`, `SyncJobEntity`, `SyncConflictEntity`
**Domain models:** `OneDriveAccount`, `AccountSettings`
**Repository interfaces/impls:** `IAccountRepository`, `AccountRepository`, `ISyncRepository`, `SyncRepository`
**EF config:** `AccountEntityConfiguration`, `SyncFolderEntityConfiguration`, `SyncJobEntityConfiguration`, `SyncConflictEntityConfiguration` — value converters required
**EF migration:** new migration needed after entity changes
**ViewModels:** all files that read `.Id`, `.AccountId`, `.FolderId`, `.RemoteItemId`, `.LocalPath` as raw strings — call sites updated to unwrap via `.Value` only at persistence/display boundaries
**Project file:** `AStar.Dev.OneDrive.Sync.Client.csproj` gains two new project references
**Test project:** `AStar.Dev.OneDrive.Sync.Client.Tests.Unit` — existing tests updated; new unit tests for value objects
