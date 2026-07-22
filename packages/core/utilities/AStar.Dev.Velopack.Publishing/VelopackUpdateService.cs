using AStar.Dev.Logging.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Velopack;
using global::Velopack.Locators;
using global::Velopack.Sources;

namespace AStar.Dev.Velopack.Publishing;

/// <inheritdoc />
public sealed class VelopackUpdateService : IVelopackUpdateService
{
    private readonly UpdateManager updateManager;
    private readonly ILogger<VelopackUpdateService> logger;

    /// <summary>Creates the update service from the bound <see cref="VelopackUpdateSettings"/>.</summary>
    /// <param name="settings">The GitHub repository releases are checked against.</param>
    /// <param name="logger">The logger used to record check/download/apply outcomes.</param>
    public VelopackUpdateService(IOptions<VelopackUpdateSettings> settings, ILogger<VelopackUpdateService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
        var locator = VelopackLocator.CreateDefaultForPlatform(logger: null);
        updateManager = new UpdateManager(new GithubSource(settings.Value.GithubRepositoryUrl.AbsoluteUri, accessToken: null, prerelease: false), locator: locator);
    }

    /// <inheritdoc />
    public bool IsInstalled => updateManager.IsInstalled;

    /// <inheritdoc />
    public async Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsInstalled)
        {
            LogMessage.Information(logger, nameof(VelopackUpdateService), "Skipping update check: app is not running as a Velopack-installed instance.");

            return null;
        }

        try
        {
            var updateInfo = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

            LogMessage.Information(
                logger,
                nameof(VelopackUpdateService),
                updateInfo is null
                    ? $"Up to date. Current version: {updateManager.CurrentVersion}."
                    : $"Update available: {updateManager.CurrentVersion} -> {updateInfo.TargetFullRelease.Version}.");

            return updateInfo;
        }
        catch (Exception ex)
        {
            LogMessage.LogException(logger, nameof(VelopackUpdateService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return null;
        }
    }

    /// <inheritdoc />
    public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        await updateManager.DownloadUpdatesAsync(updateInfo, progress, cancelToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        updateManager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
    }
}
