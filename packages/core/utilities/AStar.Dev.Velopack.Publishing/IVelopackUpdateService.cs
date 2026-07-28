using global::Velopack;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>Abstracts Velopack update checking/downloading/applying so consumers stay testable.</summary>
public interface IVelopackUpdateService
{
    /// <summary>Gets a value indicating whether the app is running as a Velopack-installed instance.</summary>
    bool IsInstalled { get; }

    /// <summary>Gets the explicit Velopack channel this instance checks for updates, e.g. "clock-linux".</summary>
    string Channel { get; }

    /// <summary>
    /// Checks the configured GitHub releases feed for a newer version.
    /// Returns <c>null</c> if no update is available, the app is not installed (e.g. running unpackaged), or the check fails.
    /// </summary>
    Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads the update described by <paramref name="updateInfo"/> to the local packages directory.</summary>
    /// <param name="updateInfo">The update to download, as returned by <see cref="CheckForUpdatesAsync"/>.</param>
    /// <param name="progress">An optional callback invoked with the download progress, from 0-100.</param>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Exits the app, applies the downloaded update, and restarts it.</summary>
    void ApplyUpdatesAndRestart(UpdateInfo updateInfo);
}
