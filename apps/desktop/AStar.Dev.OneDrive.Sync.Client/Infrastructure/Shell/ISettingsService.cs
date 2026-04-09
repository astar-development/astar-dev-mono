namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task SaveAsync();
    event EventHandler<AppSettings>? SettingsChanged;
}
