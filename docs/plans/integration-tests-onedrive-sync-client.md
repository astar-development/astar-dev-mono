# Integration Tests — AStar.Dev.OneDrive.Sync.Client

## Scope

Headless Avalonia + real DI container + real SQLite + WireMock for Graph API.
Priority flows: **sync pipeline** (SyncedItemRegistrar + file auto-categorisation) and **file classification rules** (repository CRUD + rule evaluation).

---

## Phase 1 — csproj additions

Add to `AStar.Dev.OneDrive.Sync.Client.Tests.Integration.csproj`:

**PackageReferences** (all versions from `Directory.Packages.props`):
- `Avalonia.Headless`
- `Avalonia.Headless.XUnit`
- `Microsoft.NET.Test.Sdk`
- `WireMock.Net`
- `NSubstitute`
- `Testably.Abstractions.Testing`
- `Microsoft.Kiota.Abstractions`
- `Microsoft.Kiota.Http.HttpClientLibrary`

**Global usings** (add `<Using>` items):
- `NSubstitute`
- `Testably.Abstractions.Testing`
- `AStar.Dev.OneDrive.Sync.Client.Data.Entities`

**`<TargetFramework>`**: match production app (`net9.0` or whatever it is — check production `.csproj`).

---

## Phase 2 — Infrastructure

Three files, all under `Infrastructure/`.

### 2a. `Infrastructure/IntegrationTestApp.cs`

Assembly-level attribute wires Avalonia headless for the whole project.  
The `TestApp` class is a bare `Application` — **no DI** on this class; DI lives in the fixture.

```csharp
[assembly: AvaloniaTestApplication(typeof(IntegrationTestApp))]

public sealed class IntegrationTestApp : Application
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<IntegrationTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
```

### 2b. `Infrastructure/IntegrationTestFixture.cs`

Implements `IAsyncLifetime`. Each test **class** gets one fixture instance.

Responsibilities:
- Start a `WireMockServer` on a random port (`WireMockServer.Start()` with `port: 0`) before tests run.
- Create a temp SQLite file path (`Path.GetTempFileName() + ".db"`).
- Call `services.AddPersistence()` unchanged, then replace the DbContext factory descriptor:
  ```csharp
  var existing = services.Single(d => d.ServiceType == typeof(IDbContextFactory<AppDbContext>));
  services.Remove(existing);
  services.AddSingleton<IDbContextFactory<AppDbContext>>(new TestDbContextFactory(tempDbPath));
  ```
- `TestDbContextFactory` lives in the test project — implements `IDbContextFactory<AppDbContext>`, builds `DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={tempDbPath}")`. No production code touched.
- Substitute OS-level services that would open native UI: `IAuthService`, `IFolderPickerService`, `IFileManagerService`.
- `IFileSystem` → `MockFileSystem` from Testably.
- Kiota HTTP base URL → `WireMockServer.Url`.
- Expose `ServiceProvider Services`, `WireMockServer WireMock`, `MockFileSystem FileSystem`.
- `DisposeAsync`: delete temp SQLite file, stop WireMock, dispose `ServiceProvider`.

### 2c. `Infrastructure/GraphApiStubs.cs`

Static helper — configures WireMock with canned Graph API responses.

Methods to start with:
- `StubDriveItemsPage(WireMockServer, IEnumerable<DriveItemStub>)` — `GET /v1.0/me/drive/root/delta` → returns a page of items.
- `StubEmptyDelta(WireMockServer)` — returns `@odata.deltaLink` with no items (sync already up to date).
- `StubAuthToken(WireMockServer)` — `POST /oauth2/v2.0/token` → returns a fake bearer token (if IAuthService is not fully substituted).

Use `WireMock.RequestBuilders.Request.Create()` / `WireMock.ResponseBuilders.Response.Create()` pattern.

---

## Phase 3 — Sync pipeline tests

Folder: `SyncPipeline/`

### `GivenSyncedItemRegistrar_WhenRegisteringNewFolders.cs`

`GivenSyncedItemRegistrar` class, `IClassFixture<IntegrationTestFixture>`.

Tests:
- `when_registering_a_folder_then_the_folder_is_created_in_the_file_system`
- `when_registering_a_folder_then_a_synced_item_entity_is_persisted`
- `when_registering_the_same_folder_twice_then_only_one_row_exists` (upsert idempotency)

Resolve `ISyncedItemRegistrar` from `fixture.Services`. Assert via `ISyncedItemRepository.GetAllAsync()` and `fixture.FileSystem.Directory.Exists(localPath)`.

### `GivenSyncedItemRegistrar_WhenRegisteringPhantomFiles.cs`

Tests:
- `when_registering_a_phantom_file_then_a_synced_item_entity_is_persisted`
- `when_registering_a_phantom_file_then_classifications_are_persisted`
- `when_registering_a_phantom_file_then_file_auto_categorisation_result_is_included`

Key assertion: after `RegisterPhantomAsync`, query `ISyncedItemRepository` and verify `SyncedItemClassificationEntity` rows exist with expected `Level1` value from `IFileAutoCategorisor`.

---

## Phase 4 — File classification tests

Folder: `FileClassification/`

### `GivenFileClassificationRuleRepository.cs`

Tests the repository against real SQLite (`AppDbContext` with temp file). Resolve via `fixture.Services`.

Tests:
- `when_adding_a_rule_then_it_can_be_retrieved`
- `when_adding_multiple_rules_then_all_are_returned`
- `when_no_rules_exist_then_an_empty_collection_is_returned`

### `GivenFileAutoCategorisor.cs`

Tests the full classification pipeline end-to-end: path in → `PathNormaliser` → `TokenAnalyser` → `Level1Deriver` → category out.

Tests (representative, expand as regressions are found):
- `when_path_contains_documents_token_then_level1_is_documents`
- `when_path_contains_photos_token_then_level1_is_photos`
- `when_path_is_empty_then_level1_is_uncategorised` (normalised gracefully per error-handling rules)
- `when_path_contains_no_known_tokens_then_level1_is_uncategorised`

Resolve `IFileAutoCategorisor` from `fixture.Services` — uses real implementations, no mocks.

---

## Phase 5 — Wiring verification test

`Infrastructure/GivenTheIntegrationTestFixture.cs`

Single smoke-test that the DI container builds and key services resolve without error:
- `when_resolving_services_then_sync_service_is_not_null`
- `when_resolving_services_then_synced_item_registrar_is_not_null`
- `when_resolving_services_then_file_auto_categorisor_is_not_null`
- `when_resolving_services_then_file_classification_rule_repository_is_not_null`

Fails fast if any registration is broken — cheaper than debugging a NullRef three tests deep.

---

## Folder structure

```
Tests.Integration/
  Infrastructure/
    IntegrationTestApp.cs
    IntegrationTestFixture.cs
    GraphApiStubs.cs
    GivenTheIntegrationTestFixture.cs
  SyncPipeline/
    GivenSyncedItemRegistrar_WhenRegisteringNewFolders.cs
    GivenSyncedItemRegistrar_WhenRegisteringPhantomFiles.cs
  FileClassification/
    GivenFileClassificationRuleRepository.cs
    GivenFileAutoCategorisor.cs
```

---

## Key risks / decisions

| Risk | Mitigation |
|---|---|
| `AddPersistence()` hard-codes SQLite path | Call `AddPersistence()` unchanged, then remove the `IDbContextFactory<AppDbContext>` descriptor and add `TestDbContextFactory` (test-project only) pointing to temp path |
| `IAuthService` MSAL calls block headless tests | Substitute returns `Result.Ok(new TokenResult(...))` |
| `IFileAutoCategorisor` pipeline order matters | Phase 5 PR wired it — resolve from DI, don't `new` it |
| WireMock port clash in parallel test runs | `WireMockServer.Start()` with `port: 0` (random) |
| Avalonia headless requires STA / UI thread | `Avalonia.Headless.XUnit` handles this; don't use `Task.Run` to escape UI thread in tests |
| SQLite temp files not cleaned up on crash | `DisposeAsync` wraps delete in try/catch; CI cleans `%TEMP%` anyway |

---

## Definition of done

- `dotnet build` — zero errors, zero warnings.
- `dotnet test --filter "FullyQualifiedName~Tests.Integration"` — all pass, none skipped.
- No `NSubstitute` stubs for services that can be real (repositories, registrar, categorisor).
- No `InMemory` EF provider — SQLite only.
- PR raised against `main` with PR template filled.
