using AStarDev.OneDriveSyncClient.Infrastructure.Shell;

namespace AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

internal sealed class StubSettingsService : ISettingsService
{
    public AppSettings Current { get; } = new();

    public event EventHandler<AppSettings>? SettingsChanged
    {
        add { }
        remove { }
    }

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
