# AStar.Dev.Velopack.Publishing

Shared Velopack update check/download/apply plumbing used by AStar Development desktop apps.

## Usage

```csharp
services.AddVelopackUpdates(configuration);
```

Binds `VelopackUpdateSettings` from the `Updates` configuration section (`GithubRepositoryUrl`) and registers `IVelopackUpdateService`, which wraps `Velopack.UpdateManager`:

- `IsInstalled`
- `CheckForUpdatesAsync`
- `DownloadUpdatesAsync`
- `ApplyUpdatesAndRestart`

Consumers own the "ask before download" UI flow; this package only owns the Velopack mechanics.
