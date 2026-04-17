## ADDED Requirements

### Requirement: Explicit sub-folder exclusion is persisted
When a user excludes a folder whose parent is `Included` or `Partial`, the system SHALL persist that folder as an explicit exclusion so the decision survives app restarts.

#### Scenario: Explicit exclusion written to storage
- **WHEN** a folder is toggled to `Excluded` while its parent is `Included` or `Partial`
- **THEN** a `SyncFolderEntity` row is upserted with `IsExplicitlyExcluded = true`

#### Scenario: Explicit exclusion removed when folder re-included
- **WHEN** a folder that was explicitly excluded is toggled back to `Included`
- **THEN** the `SyncFolderEntity` row is updated to `IsExplicitlyExcluded = false` (or removed)

### Requirement: Explicit exclusions are restored on application load
On loading the folder tree, the system SHALL restore explicit exclusions from storage so previously excluded sub-folders remain excluded even when their parent is `Included`.

#### Scenario: Explicitly excluded folder restores as Excluded
- **WHEN** the app loads and a folder has a persisted `IsExplicitlyExcluded = true` record
- **THEN** that folder's `SyncState` is set to `Excluded` regardless of its parent's inherited state

#### Scenario: Folder with no exclusion record inherits parent state
- **WHEN** the app loads and a folder has no `IsExplicitlyExcluded` record
- **THEN** that folder's initial `SyncState` is determined by its parent's `InheritedState`

### Requirement: SyncFolderEntity stores explicit exclusion flag
The persistence model SHALL include an `IsExplicitlyExcluded` boolean column on `SyncFolderEntity` with a default value of `false`.

#### Scenario: Existing rows unaffected by migration
- **WHEN** the database migration is applied to an existing database
- **THEN** all pre-existing `SyncFolderEntity` rows have `IsExplicitlyExcluded = false`
