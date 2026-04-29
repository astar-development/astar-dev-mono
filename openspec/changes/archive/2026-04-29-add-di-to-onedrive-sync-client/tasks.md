## 1. Branch & pre-flight

- [x] 1.1 Create feature branch `feature/add-di-to-onedrive-sync-client`
- [x] 1.2 Run `dotnet build` and `dotnet test` on the current branch to establish a clean baseline

## 2. Fix duplicate IFileSystem registration

- [x] 2.1 Remove `services.AddSingleton<IFileSystem, FileSystem>()` from `StartupServiceExtensions.AddStartupTasks` in [apps/desktop/AStar.Dev.OneDrive.Sync.Client/Startup/StartupServiceExtensions.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Startup/StartupServiceExtensions.cs)
- [x] 2.2 Confirm registration still exists in `ShellServiceExtensions.AddShell`
- [x] 2.3 Build and verify zero errors/warnings

## 3. Fix premature BuildServiceProvider in ShellServiceExtensions

- [x] 3.1 Open [apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/OneDrive/OneDriveClientServiceExtensions.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/OneDrive/OneDriveClientServiceExtensions.cs) and change `AddOneDriveClient` signature to accept `IServiceCollection` only (remove the `IOptions<EntraIdConfiguration>` parameter)
- [x] 3.2 Inside `AddOneDriveClient`, register the OneDrive client using a factory delegate that resolves `IOptions<EntraIdConfiguration>` from the `IServiceProvider` at runtime (e.g. `services.AddSingleton<IOneDriveClientOptions>(sp => sp.GetRequiredService<IOptions<EntraIdConfiguration>>().Value ...)`)
- [x] 3.3 Remove the `using var scope = services.BuildServiceProvider().CreateScope();` block from `ShellServiceExtensions.AddShell` and update the `AddOneDriveClient` call accordingly
- [x] 3.4 Build and verify zero errors/warnings

## 4. Fix SettingsService — async-init via interface

- [x] 4.1 Add `Task LoadAsync()` to `ISettingsService` in [apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Shell/ISettingsService.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Shell/ISettingsService.cs)
- [x] 4.2 Convert the static `LoadAsync()` factory on `SettingsService` to an instance method implementing `ISettingsService.LoadAsync()` — remove the `static` keyword and remove the `new SettingsService()` instantiation inside it
- [x] 4.3 Change `ITransient<ISettingsService, SettingsService>` to `ISingleton<ISettingsService, SettingsService>` in `ShellServiceExtensions.AddShell`
- [x] 4.4 Update any other callers of the old static `SettingsService.LoadAsync()` (currently only `App.BootstrapAsync`) to use `await settingsService.LoadAsync()` on the injected instance
- [x] 4.5 Build and verify zero errors/warnings

## 5. Introduce IAppBootstrapper

- [x] 5.1 Create `IAppBootstrapper` interface in `apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Shell/IAppBootstrapper.cs` with method `Task BootstrapAsync(IProgress<string> progress, CancellationToken ct = default)`
- [x] 5.2 Create `AppBootstrapper` class in `apps/desktop/AStar.Dev.OneDrive.Sync.Client/Infrastructure/Shell/AppBootstrapper.cs` implementing `IAppBootstrapper`. Constructor parameters: `IDbContextFactory<AppDbContext>`, `ISettingsService`, `IThemeService`, `ISyncScheduler`, `MainWindowViewModel` (and any other required services). Extract the full body of `App.BootstrapAsync` into this class.
- [x] 5.3 Register `IAppBootstrapper` as `Transient<IAppBootstrapper, AppBootstrapper>` in `ShellServiceExtensions.AddShell` (or a new extension method)
- [x] 5.4 Build and verify zero errors/warnings

## 6. Register SplashWindow in DI

- [x] 6.1 Add `services.AddTransient<SplashWindow>()` to `ViewExtensions.AddViews` in [apps/desktop/AStar.Dev.OneDrive.Sync.Client/Startup/ViewExtensions.cs](apps/desktop/AStar.Dev.OneDrive.Sync.Client/Startup/ViewExtensions.cs)
- [x] 6.2 Build and verify zero errors/warnings

## 7. Simplify App.axaml.cs

- [x] 7.1 Remove `private static ISyncScheduler Scheduler` static field from `App`
- [x] 7.2 Remove the `BootstrapAsync` private method from `App`
- [x] 7.3 In `OnFrameworkInitializationCompleted`, replace `new SplashWindow()` with `_services.GetRequiredService<SplashWindow>()`
- [x] 7.4 In the `splashWindow.Opened` handler, resolve `IAppBootstrapper` from `_services` and call `await bootstrapper.BootstrapAsync(progress)`
- [x] 7.5 Remove all other `_services.GetRequiredService<T>()` calls from `App` (they now live in `AppBootstrapper`)
- [x] 7.6 In `desktop.Exit`, remove `await Scheduler.DisposeAsync()` — the `ServiceProvider` disposal handles it
- [x] 7.7 Build and verify zero errors/warnings

## 8. Tests

- [x] 8.1 Add unit tests in `AStar.Dev.OneDrive.Sync.Client.Tests.Unit` for `AppBootstrapper` — verify startup sequence order using NSubstitute mocks for all dependencies
- [x] 8.2 Add unit tests for `SettingsService.LoadAsync()` — verify `Current` is populated from valid JSON, uses defaults on missing file, uses defaults and logs warning on malformed JSON
- [x] 8.3 Add test verifying `IFileSystem` appears exactly once in the service collection built by `ShellServiceExtensions.AddShell` (integration-style DI container test)
- [x] 8.4 Add test verifying a complete `ServiceProvider` can be built from all extension methods without throwing (guards against future premature `BuildServiceProvider` regressions)
- [x] 8.5 Run `dotnet test` — all tests must pass

## 9. Final check & PR

- [x] 9.1 Run `dotnet build` — zero errors, zero warnings
- [x] 9.2 Run `dotnet test` — all tests pass
- [ ] 9.3 Request human review
- [ ] 9.4 After approval: commit to branch with Conventional Commit message, raise GitHub PR
