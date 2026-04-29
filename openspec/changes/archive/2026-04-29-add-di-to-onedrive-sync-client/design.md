## Context

`AStar.Dev.OneDrive.Sync.Client` is an Avalonia desktop app that manually orchestrates its startup in `App.axaml.cs`. The current shape:

- `App.BuildServiceProvider()` is a static method called at field-initialiser time, before Avalonia is fully running.
- `App.BootstrapAsync()` is a private method that resolves ~10 services via `_services.GetRequiredService<T>()` — a service-locator pattern.
- `SettingsService.LoadAsync()` is a static factory that news up its own instance outside the container.
- `ShellServiceExtensions.AddShell()` calls `services.BuildServiceProvider().CreateScope()` mid-registration to extract `IOptions<EntraIdConfiguration>` — this builds a second, incomplete container and triggers a compiler/analyser warning about captive dependencies.
- `SplashWindow` is `new`-ed inline; `ISyncScheduler` is stored in a `private static` field on `App`.
- `IFileSystem` is registered in two extension methods.

The repo mandate: "DI never afterthought. Language supports it? MUST implement from start."

## Goals / Non-Goals

**Goals:**

- Eliminate the service-locator `GetRequiredService` calls from `App.axaml.cs` by introducing `IAppBootstrapper`.
- Fix `SettingsService` to be a proper DI singleton with async initialisation surfaced through `ISettingsService`.
- Remove the premature `BuildServiceProvider()` in `ShellServiceExtensions`; wire `IOptions<EntraIdConfiguration>` at resolve time instead.
- Register `SplashWindow` in the container.
- Remove the `private static ISyncScheduler` field from `App`.
- Deduplicate `IFileSystem` registration.
- All changes must keep the build green (zero errors, zero warnings, `TreatWarningsAsErrors=true`).
- All changes must be covered by unit tests in `AStar.Dev.OneDrive.Sync.Client.Tests.Unit`.

**Non-Goals:**

- Switching to `Microsoft.Extensions.Hosting` / `IHost` (future work).
- Changing sync or Graph business logic.
- Touching `Program.cs` (the Avalonia entry point is not the DI composition root issue).

## Decisions

### D1 — Introduce `IAppBootstrapper`

Extract `BootstrapAsync` into a dedicated `AppBootstrapper : IAppBootstrapper` class registered as `Transient<IAppBootstrapper>`.

`App.OnFrameworkInitializationCompleted` resolves a single `IAppBootstrapper` from the container and calls `await bootstrapper.BootstrapAsync(progress)`. `App` itself becomes a thin lifecycle host.

**Rationale:** Removes the service-locator smell. `AppBootstrapper` receives all dependencies via constructor injection, making them explicit and testable. The existing `IStartupService` and `StartupService` pattern is evidence this is already the preferred approach in the codebase.

**Alternative considered:** Keep logic in `App` but inject all dependencies via constructor. Rejected — Avalonia's `App` has no DI-friendly constructor path (it is instantiated by XAML).

---

### D2 — Fix `SettingsService` with async-init via startup-task pipeline

Add `Task LoadAsync()` to `ISettingsService`. `SettingsService` remains a singleton but loads from disk during `AppBootstrapper.BootstrapAsync` by calling `await settingsService.LoadAsync()`.

Register `SettingsService` as `Singleton<ISettingsService, SettingsService>`. Remove the static `LoadAsync()` factory entirely.

**Rationale:** The repo already has a startup-task pattern (`IStartupService`). Async initialisation of a singleton via an explicit `await` in the bootstrapper (rather than a `Task` property, `Lazy<Task>`, or factory) is the simplest correct approach — no hidden deferred work, easy to test, easy to sequence.

**Alternative considered:** Register `ISettingsService` as `Func<Task<ISettingsService>>` using an async factory. Rejected — more complex, harder to mock, and unnecessary given the bootstrapper already controls startup sequencing.

---

### D3 — Remove premature `BuildServiceProvider` in `ShellServiceExtensions`

`AddOneDriveClient` currently receives an `IOptions<EntraIdConfiguration>` resolved from a partial container. Replace this by passing `IOptions<EntraIdConfiguration>` as a factory parameter resolved at the point the `HttpClient` / `OneDriveClient` is first used (i.e. lazy via the registered `IOptions<T>` mechanism).

Concretely: change `AddOneDriveClient` to accept `IServiceCollection` only, and have it register a factory delegate that resolves `IOptions<EntraIdConfiguration>` at runtime via `serviceProvider.GetRequiredService<IOptions<EntraIdConfiguration>>()` inside the factory lambda.

**Rationale:** This is how `IOptions<T>` is designed to be consumed. The current approach builds a second container at registration time, which defeats DI validation and can cause captive dependency issues.

---

### D4 — Register `SplashWindow` in the container

Add `services.AddTransient<SplashWindow>()` in `ViewExtensions.AddViews`. Resolve in `AppBootstrapper` (or in `App` before the bootstrapper runs).

---

### D5 — Remove static `Scheduler` field

`ISyncScheduler` is already registered as a singleton. `App` can hold a reference via `IAsyncDisposable` by resolving it from the container once at startup inside `AppBootstrapper` and returning it (or storing it on `AppBootstrapper` itself). `App.desktop.Exit` disposes the `ServiceProvider`, which disposes the singleton scheduler.

**Rationale:** Static mutable state is untestable and fragile. The `ServiceProvider` already owns the lifetime; no need for a separate static hold.

---

### D6 — Deduplicate `IFileSystem`

Remove `services.AddSingleton<IFileSystem, FileSystem>()` from `StartupServiceExtensions.AddStartupTasks`. It stays in `ShellServiceExtensions.AddShell`.

## Risks / Trade-offs

- **Avalonia designer** — `BuildServiceProvider` in a static initialiser could affect the Avalonia XAML designer if any ViewModel/View resolution occurs at design time. Mitigated: `AppBootstrapper` is only constructed at runtime; the designer path uses `ViewLocator` which should be unaffected.
- **Test isolation** — Tests that exercise `App` directly (none currently exist) would need a real or fake `IAppBootstrapper`. Mitigated: `IAppBootstrapper` interface makes substitution trivial.
- **Order sensitivity** — `SettingsService.LoadAsync()` must be called before `IThemeService.Apply`. The bootstrapper explicitly sequences these; no change to the actual order, just to where the orchestration lives.
