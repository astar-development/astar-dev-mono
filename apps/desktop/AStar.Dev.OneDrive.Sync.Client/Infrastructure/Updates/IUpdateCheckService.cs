using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;

/// <summary>Abstracts Velopack update checking/downloading/applying so consumers stay testable.</summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Checks the configured GitHub releases feed for a newer version.
    /// Returns <c>null</c> if no update is available, the app is not installed (e.g. running unpackaged), or the check fails.
    /// </summary>
    Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads the update described by <paramref name="updateInfo"/> to the local packages directory.</summary>
    Task DownloadUpdatesAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default);

    /// <summary>Exits the app, applies the downloaded update, and restarts it.</summary>
    void ApplyUpdatesAndRestart(UpdateInfo updateInfo);
}
