## ADDED Requirements

### Requirement: LocalSyncPath value object with validated construction
The system SHALL define `LocalSyncPath` as an immutable `record` with a private constructor and a static `LocalSyncPathFactory.Create(string rawPath)` method returning `Result<LocalSyncPath>`. Construction SHALL fail with a `ValidationError` when `rawPath` is null, empty, or whitespace-only.

#### Scenario: Valid path creates LocalSyncPath
- **WHEN** `LocalSyncPathFactory.Create("C:\\Users\\user\\OneDrive")` is called with a non-empty string
- **THEN** the result SHALL be `Result<LocalSyncPath>.Success` and `localSyncPath.Value` SHALL equal the normalised path string

#### Scenario: Null path rejected
- **WHEN** `LocalSyncPathFactory.Create(null!)` is called
- **THEN** the result SHALL be `Result<LocalSyncPath>.Failure` containing a `ValidationError`

#### Scenario: Empty path rejected
- **WHEN** `LocalSyncPathFactory.Create(string.Empty)` is called
- **THEN** the result SHALL be `Result<LocalSyncPath>.Failure` containing a `ValidationError`

#### Scenario: Whitespace-only path rejected
- **WHEN** `LocalSyncPathFactory.Create("   ")` is called
- **THEN** the result SHALL be `Result<LocalSyncPath>.Failure` containing a `ValidationError`

### Requirement: LocalSyncPath replaces raw string on AccountEntity and OneDriveAccount
`AccountEntity.LocalSyncPath` and `OneDriveAccount.LocalSyncPath` SHALL be typed as `LocalSyncPath`, not `string`. EF SHALL map `AccountEntity.LocalSyncPath` to the existing `TEXT` column via a value converter.

#### Scenario: LocalSyncPath on AccountEntity persisted to SQLite
- **WHEN** an `AccountEntity` with a valid `LocalSyncPath` is saved via EF
- **THEN** the `LocalSyncPath` column SHALL contain the underlying string value with no schema change

#### Scenario: LocalSyncPath on AccountEntity loaded from SQLite
- **WHEN** an `AccountEntity` is read from the database
- **THEN** the `LocalSyncPath` property SHALL be a valid `LocalSyncPath` instance whose `Value` matches the stored string

### Requirement: LocalSyncPath unit tests cover all construction branches
The `AStar.Dev.OneDrive.Sync.Client.Tests.Unit` project SHALL contain tests covering valid construction, null rejection, empty rejection, and whitespace rejection.

#### Scenario: All branches tested
- **WHEN** the test suite runs
- **THEN** every branch of `LocalSyncPathFactory.Create` SHALL be exercised by at least one test
