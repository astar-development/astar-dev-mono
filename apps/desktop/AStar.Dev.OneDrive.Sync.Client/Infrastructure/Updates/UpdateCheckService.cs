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
            return null;

        try
        {
            return await updateManager.CheckForUpdatesAsync();
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
