## ADDED Requirements

### Requirement: Classification persisted at file registration
When a `FileDeltaItem` is registered or upserted in the database, the system SHALL classify the item using its remote path and persist the resulting tags as `SyncedItemClassificationEntity` rows linked to the `SyncedItemEntity`. Existing tags for the item SHALL be replaced on each registration.

#### Scenario: New file with matching rules
- **WHEN** a new `FileDeltaItem` with remote path `/Photos/red-car.jpg` is registered and rules exist for `red` and `car`
- **THEN** two `SyncedItemClassificationEntity` rows are written, linked to the new `SyncedItemEntity`

#### Scenario: New file with no matching rules
- **WHEN** a new `FileDeltaItem` is registered and no rules match its path
- **THEN** one `SyncedItemClassificationEntity` row is written with `TagName = "Unclassified"`

#### Scenario: Re-registration replaces tags
- **WHEN** a `FileDeltaItem` that was previously classified is registered again (delta update)
- **THEN** old classification rows for that item are deleted and new ones written

#### Scenario: Folder items not classified
- **WHEN** a `FolderDeltaItem` is registered
- **THEN** no `SyncedItemClassificationEntity` rows are written for that item
