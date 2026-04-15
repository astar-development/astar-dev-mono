## ADDED Requirements

### Requirement: AccountId strongly-typed identifier
The system SHALL define `AccountId` as a `[StrongId(typeof(string))]` partial record struct in the `AStar.Dev.OneDrive.Sync.Client.Domain` namespace. All entity properties, repository method parameters, and domain model properties that represent the Microsoft Graph account object ID MUST use `AccountId` instead of `string`.

#### Scenario: AccountId used as primary key
- **WHEN** `AccountEntity.Id` is declared
- **THEN** its CLR type SHALL be `AccountId`, and EF SHALL map it to the existing `TEXT` column via a value converter

#### Scenario: AccountId used as foreign key
- **WHEN** `SyncFolderEntity.AccountId`, `SyncJobEntity.AccountId`, or `SyncConflictEntity.AccountId` is declared
- **THEN** each property's CLR type SHALL be `AccountId` and EF SHALL map it to the existing `TEXT` FK column via a value converter

#### Scenario: AccountId propagated through repository interface
- **WHEN** `IAccountRepository.GetByIdAsync`, `DeleteAsync`, `SetActiveAccountAsync`, or `UpdateDeltaLinkAsync` is called
- **THEN** the `id` / `accountId` parameter type SHALL be `AccountId`, not `string`

#### Scenario: AccountId propagated through domain model
- **WHEN** `OneDriveAccount.Id` is accessed
- **THEN** its type SHALL be `AccountId`, not `string`

#### Scenario: Wrong ID type rejected at compile time
- **WHEN** a caller passes an `OneDriveFolderId` value to a parameter declared as `AccountId`
- **THEN** the build SHALL fail with a type error

### Requirement: OneDriveFolderId strongly-typed identifier
The system SHALL define `OneDriveFolderId` as a `[StrongId(typeof(string))]` partial record struct. All properties representing a Microsoft Graph drive-item folder ID MUST use `OneDriveFolderId`.

#### Scenario: OneDriveFolderId on SyncFolderEntity
- **WHEN** `SyncFolderEntity.FolderId` is declared
- **THEN** its CLR type SHALL be `OneDriveFolderId`, mapped to `TEXT` via a value converter

#### Scenario: OneDriveFolderId on SyncJobEntity
- **WHEN** `SyncJobEntity.FolderId` is declared
- **THEN** its CLR type SHALL be `OneDriveFolderId`, mapped to `TEXT` via a value converter

#### Scenario: OneDriveFolderId on SyncConflictEntity
- **WHEN** `SyncConflictEntity.FolderId` is declared
- **THEN** its CLR type SHALL be `OneDriveFolderId`, mapped to `TEXT` via a value converter

### Requirement: OneDriveItemId strongly-typed identifier
The system SHALL define `OneDriveItemId` as a `[StrongId(typeof(string))]` partial record struct. All properties representing a Microsoft Graph drive-item ID (non-folder) MUST use `OneDriveItemId`.

#### Scenario: OneDriveItemId on SyncJobEntity
- **WHEN** `SyncJobEntity.RemoteItemId` is declared
- **THEN** its CLR type SHALL be `OneDriveItemId`, mapped to `TEXT` via a value converter

#### Scenario: OneDriveItemId on SyncConflictEntity
- **WHEN** `SyncConflictEntity.RemoteItemId` is declared
- **THEN** its CLR type SHALL be `OneDriveItemId`, mapped to `TEXT` via a value converter

### Requirement: Source generator project references added
The system SHALL add `AStar.Dev.Source.Generators` and `AStar.Dev.Source.Generators.Attributes` as `<ProjectReference>` entries in `AStar.Dev.OneDrive.Sync.Client.csproj`.

#### Scenario: Build succeeds with generator references
- **WHEN** `dotnet build` is run against `AStar.Dev.OneDrive.Sync.Client`
- **THEN** the build SHALL succeed with zero errors and zero warnings, and all `[StrongId]`-attributed types SHALL have their generated partial implementations present
