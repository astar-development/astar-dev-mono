namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface ISettingsService
{
    /// <summary>Gets the current application settings.</summary>
    AppSettings Current { get; }

    /// <summary>Loads settings from disk into <see cref="Current"/>. Safe to call multiple times; subsequent calls reload from disk.</summary>
    Task LoadAsync();

    /// <summary>Persists <see cref="Current"/> to disk and raises <see cref="SettingsChanged"/>.</summary>
    Task SaveAsync();

    /// <summary>Raised after <see cref="SaveAsync"/> completes, passing the saved <see cref="AppSettings"/> snapshot.</summary>
    event EventHandler<AppSettings>? SettingsChanged;
}
