## 1. Project Setup

- [x] 1.1 Add `<ProjectReference>` for `AStar.Dev.Source.Generators` to `AStar.Dev.OneDrive.Sync.Client.csproj`
- [x] 1.2 Add `<ProjectReference>` for `AStar.Dev.Source.Generators.Attributes` to `AStar.Dev.OneDrive.Sync.Client.csproj`
- [x] 1.3 Run `dotnet build AStar.Dev.OneDrive.Sync.Client` and confirm zero errors before continuing

## 2. Strongly-Typed ID Value Objects

- [x] 2.1 Create `Domain/AccountId.cs` — `[StrongId(typeof(string))] partial record struct AccountId`
- [x] 2.2 Create `Domain/OneDriveFolderId.cs` — `[StrongId(typeof(string))] partial record struct OneDriveFolderId`
- [x] 2.3 Create `Domain/OneDriveItemId.cs` — `[StrongId(typeof(string))] partial record struct OneDriveItemId`

## 3. LocalSyncPath Value Object

- [x] 3.1 Create `Domain/LocalSyncPath.cs` — immutable `record` with private constructor and `Value` property
- [x] 3.2 Create `Domain/LocalSyncPathFactory.cs` — static factory with `Create(string rawPath)` returning `Result<LocalSyncPath>`; validate null/empty/whitespace

## 4. Entity Updates

- [x] 4.1 Update `AccountEntity.Id` from `string` to `AccountId`
- [x] 4.2 Update `AccountEntity.LocalSyncPath` from `string` to `LocalSyncPath`
- [x] 4.3 Update `SyncFolderEntity.AccountId` from `string` to `AccountId`
- [x] 4.4 Update `SyncFolderEntity.FolderId` from `string` to `OneDriveFolderId`
- [x] 4.5 Update `SyncJobEntity.AccountId` from `string` to `AccountId`
- [x] 4.6 Update `SyncJobEntity.FolderId` from `string` to `OneDriveFolderId`
- [x] 4.7 Update `SyncJobEntity.RemoteItemId` from `string` to `OneDriveItemId`
- [x] 4.8 Update `SyncConflictEntity.AccountId` from `string` to `AccountId`
- [x] 4.9 Update `SyncConflictEntity.FolderId` from `string` to `OneDriveFolderId`
- [x] 4.10 Update `SyncConflictEntity.RemoteItemId` from `string` to `OneDriveItemId`

## 5. EF Core Value Converters

- [x] 5.1 Update `AccountEntityConfiguration` — register `AccountId` converter for `Id`; register `LocalSyncPath` converter for `LocalSyncPath`
- [x] 5.2 Update `SyncFolderEntityConfiguration` — register `AccountId` converter for `AccountId`; register `OneDriveFolderId` converter for `FolderId`
- [x] 5.3 Update `SyncJobEntityConfiguration` — register `AccountId`, `OneDriveFolderId`, `OneDriveItemId` converters
- [x] 5.4 Update `SyncConflictEntityConfiguration` — register `AccountId`, `OneDriveFolderId`, `OneDriveItemId` converters

## 6. Domain Model Updates

- [x] 6.1 Update `OneDriveAccount.Id` from `string` to `AccountId`
- [x] 6.2 Update `OneDriveAccount.LocalSyncPath` from `string` to `LocalSyncPath`
- [x] 6.3 Update `OneDriveAccount.SelectedFolderIds` from `List<string>` to `List<OneDriveFolderId>`
- [x] 6.4 Update `OneDriveAccount.FolderNames` keys from `string` to `OneDriveFolderId`

## 7. Repository Interface and Implementation Updates

- [x] 7.1 Update `IAccountRepository.GetByIdAsync` — `id` param `string` → `AccountId`
- [x] 7.2 Update `IAccountRepository.DeleteAsync` — `id` param `string` → `AccountId`
- [x] 7.3 Update `IAccountRepository.SetActiveAccountAsync` — `id` param `string` → `AccountId`
- [x] 7.4 Update `IAccountRepository.UpdateDeltaLinkAsync` — `accountId` param `string` → `AccountId`; `folderId` param `string` → `OneDriveFolderId`
- [x] 7.5 Update `AccountRepository` implementation to match updated interface
- [x] 7.6 Update `ISyncRepository` and `SyncRepository` — all `accountId` / `folderId` / `jobId` / `conflictId` params to use typed IDs
- [x] 7.7 Update `SyncRepository` projection mappings (`.Id`, `.AccountId`, `.FolderId`, `.RemoteItemId`)

## 8. Call-site Updates (ViewModels and Services)

- [x] 8.1 Fix all compiler errors in `Accounts/` ViewModels — update `.Id` / `.AccountId` usages
- [x] 8.2 Fix all compiler errors in `Dashboard/` ViewModels
- [x] 8.3 Fix all compiler errors in `Activity/` ViewModels
- [x] 8.4 Fix all compiler errors in `Conflicts/` — `ConflictResolver`, `ConflictItemViewModel`
- [x] 8.5 Fix all compiler errors in `Infrastructure/Sync/` — `SyncService`, `DownloadWorker`, `LocalChangeDetector`, `SyncScheduler`
- [x] 8.6 Fix all compiler errors in `Infrastructure/Authentication/` — `AuthService`, `AuthResult`
- [x] 8.7 Fix all compiler errors in `Home/`, `Onboarding/`, `Settings/` ViewModels
- [x] 8.8 Fix all compiler errors in `Startup/StartupService.cs`

## 9. EF Migration

- [x] 9.1 Run `dotnet ef migrations add StronglyTypedIds --project AStar.Dev.OneDrive.Sync.Client`
- [x] 9.2 Verify generated migration SQL is a no-op (no `ALTER TABLE` or data changes)
- [ ] 9.3 Run `dotnet ef database update` against a local test database and confirm success

## 10. Tests

- [x] 10.1 Add unit tests for `LocalSyncPathFactory.Create` — valid path, null, empty, whitespace
- [x] 10.2 Verify existing unit tests in `AStar.Dev.OneDrive.Sync.Client.Tests.Unit` still pass
- [x] 10.3 Update any test helpers or builders that construct entities with raw string IDs

## 11. Final Verification

- [x] 11.1 `dotnet build` — zero errors, zero warnings
- [x] 11.2 `dotnet test` — all tests pass
- [ ] 11.3 Request human code review before committing
