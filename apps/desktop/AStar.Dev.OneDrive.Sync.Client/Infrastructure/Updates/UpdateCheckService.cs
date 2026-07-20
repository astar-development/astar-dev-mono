using AStar.Dev.Logging.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Sources;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;

/// <inheritdoc />
public sealed class UpdateCheckService : IUpdateCheckService
{
    private readonly UpdateManager updateManager;
    private readonly ILogger<UpdateCheckService> logger;

    public UpdateCheckService(IOptions<UpdateSettings> settings, ILogger<UpdateCheckService> logger)
    {
        this.logger = logger;
        updateManager = new UpdateManager(new GithubSource(settings.Value.GithubRepositoryUrl, accessToken: string.Empty, prerelease: false));
    }

    /// <inheritdoc />
    public async Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!updateManager.IsInstalled)
        {
            LogMessage.Information(logger, nameof(UpdateCheckService), "Skipping update check: app is not running as a Velopack-installed instance.");

            return null;
        }

        try
        {
            var updateInfo = await updateManager.CheckForUpdatesAsync();

            LogMessage.Information(
                logger,
                nameof(UpdateCheckService),
                updateInfo is null
                    ? $"Up to date. Current version: {updateManager.CurrentVersion}."
                    : $"Update available: {updateManager.CurrentVersion} -> {updateInfo.TargetFullRelease.Version}.");

            return updateInfo;
        }
        catch (Exception ex)
        {
            LogMessage.LogException(logger, nameof(UpdateCheckService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default)
        => await updateManager.DownloadUpdatesAsync(updateInfo, cancelToken: cancellationToken);

    /// <inheritdoc />
    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
        => updateManager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
}
