## Why

`AStar.Dev.OneDrive.Sync.Client` has grown organic DI anti-patterns that make the app harder to test, reason about, and extend: a static service-locator bootstrap, a premature `BuildServiceProvider()` call during registration, `SettingsService.LoadAsync()` bypassing the container entirely, and `SplashWindow` newed up directly. These block proper unit-testing of the bootstrap path and violate the repo's "DI never afterthought" rule.

## What Changes

- Remove the service-locator pattern from `App.BootstrapAsync` — resolve via constructor injection instead of repeated `_services.GetRequiredService<T>()` calls.
- Extract `IAppBootstrapper` (or equivalent) so bootstrap logic lives in a proper, injectable, testable service rather than in `App.axaml.cs`.
- Fix `SettingsService`: make async initialisation part of an `IAsyncInitialiser` pipeline registered with DI (or use `IHostedService` / startup task pattern already present) rather than calling `SettingsService.LoadAsync()` directly.
- Remove the premature `services.BuildServiceProvider().CreateScope()` inside `ShellServiceExtensions.AddShell` — wire `OneDriveClient` configuration through `IOptions<EntraIdConfiguration>` resolved at runtime instead.
- Register `SplashWindow` in the container and resolve rather than `new SplashWindow()`.
- Remove the `private static ISyncScheduler Scheduler` static field from `App`; hold lifecycle via the `IDisposable` / `IAsyncDisposable` pattern already in place.
- Deduplicate `IFileSystem` registration (registered in both `AddStartupTasks` and `AddShell`).

## Capabilities

### New Capabilities

- `app-bootstrapper`: Injectable `IAppBootstrapper` that owns the ordered async startup sequence (DB migration → settings load → theme → scheduler start). Replaces the inline `BootstrapAsync` method in `App`.
- `settings-async-init`: `ISettingsService` registration that loads from disk during the startup-task pipeline rather than via a static `LoadAsync()` call. `ISettingsService` gains a `LoadAsync()` contract so callers can await initialisation through the interface.

### Modified Capabilities

*(none — no existing spec-level behaviour changes)*

## Impact

- **`App.axaml.cs`**: simplified to wire lifecycle events and resolve `IAppBootstrapper`; drops direct `GetRequiredService` calls, static `Scheduler` field, and `new SplashWindow()`.
- **`ShellServiceExtensions.cs`**: removes premature `BuildServiceProvider()` / `CreateScope()` call; `IFileSystem` registered once.
- **`SettingsService.cs`**: static `LoadAsync` factory replaced by instance `LoadAsync` implementing new `ISettingsService` contract; registered as singleton via startup-task pattern.
- **`StartupServiceExtensions.cs`**: `IFileSystem` registration removed (lives in `ShellServiceExtensions`).
- **`SplashWindow`**: added to DI container registration.
- No public API or NuGet package surface changes — desktop app only.
