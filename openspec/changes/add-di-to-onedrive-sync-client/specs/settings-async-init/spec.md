## ADDED Requirements

### Requirement: ISettingsService exposes an async load method
The `ISettingsService` interface SHALL declare `Task LoadAsync()` so callers can await settings initialisation through the interface without coupling to the concrete implementation.

#### Scenario: Settings loaded via interface method
- **WHEN** `ISettingsService.LoadAsync()` is called on the resolved singleton
- **THEN** `Current` SHALL be populated from disk (or default `AppSettings` on failure / missing file)

#### Scenario: Settings already loaded is idempotent
- **WHEN** `ISettingsService.LoadAsync()` is called a second time
- **THEN** it SHALL reload from disk (or replace with defaults on failure) without throwing

### Requirement: SettingsService registered as DI singleton; static factory removed
`SettingsService` SHALL be registered as `Singleton<ISettingsService, SettingsService>` in `ShellServiceExtensions.AddShell`. The static `SettingsService.LoadAsync()` factory method SHALL be removed. Callers SHALL obtain the service via injection and call `await service.LoadAsync()`.

#### Scenario: SettingsService resolved from container
- **WHEN** `ISettingsService` is resolved from the DI container
- **THEN** a single instance SHALL be returned for the lifetime of the application

#### Scenario: No direct call to static factory remains
- **WHEN** the codebase is built
- **THEN** no call site SHALL reference `SettingsService.LoadAsync()` as a static member

### Requirement: OneDriveClient configured without premature container build
`ShellServiceExtensions.AddShell` SHALL NOT call `services.BuildServiceProvider()` or `CreateScope()` during service registration. `AddOneDriveClient` SHALL receive configuration via a factory delegate that resolves `IOptions<EntraIdConfiguration>` at runtime.

#### Scenario: Container built once
- **WHEN** `App.BuildServiceProvider()` completes
- **THEN** exactly one `ServiceProvider` SHALL have been created for the application lifetime

#### Scenario: EntraId configuration resolved at runtime
- **WHEN** the OneDrive client is first used
- **THEN** `IOptions<EntraIdConfiguration>` SHALL be resolved from the single, complete container — not from a partial container built during registration

### Requirement: IFileSystem registered exactly once
`IFileSystem` SHALL be registered in one extension method only (`ShellServiceExtensions.AddShell`). The duplicate registration in `StartupServiceExtensions.AddStartupTasks` SHALL be removed.

#### Scenario: No duplicate IFileSystem registration
- **WHEN** the DI container is built
- **THEN** `IFileSystem` SHALL appear in the service descriptors exactly once
