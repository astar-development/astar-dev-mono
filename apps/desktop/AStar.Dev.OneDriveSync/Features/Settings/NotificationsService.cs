using System;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using Microsoft.Extensions.Logging;

using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Features.Settings;

/// <summary>Persists and caches the OS-notification preference (ST-03).</summary>
internal sealed partial class NotificationsService(IAppSettingsRepository settingsRepository, ILogger<NotificationsService> logger) : INotificationsService
{
    /// <inheritdoc />
    public bool NotificationsEnabled { get; private set; } = true;

    /// <inheritdoc />
    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        var result = await settingsRepository.GetAsync(ct).ConfigureAwait(false);

        NotificationsEnabled = result is Result<AppSettings?, ErrorResponse>.Ok { Value: { } settings }
            ? settings.NotificationsEnabled
            : true;

        LogInitialised(logger, NotificationsEnabled);
    }

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> SetEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        try
        {
            var getResult = await settingsRepository.GetAsync(ct).ConfigureAwait(false);

            var settings = getResult is Result<AppSettings?, ErrorResponse>.Ok { Value: { } existing }
                ? existing
                : new AppSettings { Id = AppSettings.SingletonId };

            settings.NotificationsEnabled = enabled;

            var saveResult = await settingsRepository.SaveAsync(settings, ct).ConfigureAwait(false);

            if (saveResult is Result<AppSettings, ErrorResponse>.Error err)
                return new Result<bool, ErrorResponse>.Error(err.Reason);

            NotificationsEnabled = enabled;
            LogChanged(logger, enabled);

            return new Result<bool, ErrorResponse>.Ok(enabled);
        }
        catch (Exception ex)
        {
            LogFailed(logger, ex);

            return new Result<bool, ErrorResponse>.Error(new ErrorResponse(ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Notifications initialised — Enabled={Enabled}")]
    private static partial void LogInitialised(MelILogger logger, bool enabled);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Notifications changed — Enabled={Enabled}")]
    private static partial void LogChanged(MelILogger logger, bool enabled);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to persist notification setting")]
    private static partial void LogFailed(MelILogger logger, Exception ex);
}
