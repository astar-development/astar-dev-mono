## ADDED Requirements

### Requirement: App bootstrap sequence is encapsulated in an injectable service
The system SHALL expose an `IAppBootstrapper` interface with a single `Task BootstrapAsync(IProgress<string> progress, CancellationToken ct = default)` method. `AppBootstrapper` SHALL implement this interface and receive all startup dependencies via constructor injection.

#### Scenario: Bootstrapper runs startup sequence in order
- **WHEN** `AppBootstrapper.BootstrapAsync` is called
- **THEN** it SHALL execute steps in the order: DB migration → settings load → theme apply → service configuration → scheduler start

#### Scenario: App resolves bootstrapper from container, not service locator
- **WHEN** `App.OnFrameworkInitializationCompleted` runs
- **THEN** it SHALL resolve exactly one `IAppBootstrapper` from the container and call `BootstrapAsync` — no other `GetRequiredService<T>` calls SHALL exist in `App`

#### Scenario: Bootstrapper is registered as Transient
- **WHEN** the DI container is built
- **THEN** `IAppBootstrapper` SHALL be registered as `Transient` so each logical startup gets a fresh instance

### Requirement: SplashWindow is resolved from the DI container
The system SHALL register `SplashWindow` as `Transient` in `ViewExtensions.AddViews`. `App` SHALL resolve it from the container rather than using `new SplashWindow()`.

#### Scenario: SplashWindow resolved from container
- **WHEN** the app starts and needs to show the splash window
- **THEN** `SplashWindow` SHALL be obtained via `_services.GetRequiredService<SplashWindow>()` (or injected into the bootstrapper) — NOT via `new SplashWindow()`

### Requirement: ISyncScheduler lifecycle is managed by the service container
The system SHALL NOT hold `ISyncScheduler` in a `private static` field on `App`. The scheduler's lifetime SHALL be owned entirely by the `ServiceProvider`.

#### Scenario: Scheduler disposed on exit without static field
- **WHEN** the application exits and the `ServiceProvider` is disposed
- **THEN** the singleton `ISyncScheduler` SHALL be disposed via normal DI lifetime management — no separate static reference needed
