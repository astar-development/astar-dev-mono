using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>Reads and upserts the single <see cref="AppSettings" /> row via EF Core.</summary>
internal sealed partial class AppSettingsRepository(IDbContextFactory<AppDbContext> contextFactory, ILogger<AppSettingsRepository> logger) : IAppSettingsRepository
{
    /// <inheritdoc />
    public async Task<Result<AppSettings?, ErrorResponse>> GetAsync(CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var settings = await ctx.AppSettings.FindAsync([AppSettings.SingletonId], ct).ConfigureAwait(false);
            LogSettingsLoaded(logger, settings?.ThemeMode ?? "(none)", settings?.Locale ?? "(none)");

            return new Result<AppSettings?, ErrorResponse>.Ok(settings);
        }
        catch (Exception ex)
        {
            LogSettingsLoadFailed(logger, ex);

            return new Result<AppSettings?, ErrorResponse>.Error(new ErrorResponse(ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<AppSettings, ErrorResponse>> SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var existing = await ctx.AppSettings.FindAsync([AppSettings.SingletonId], ct).ConfigureAwait(false);

            if (existing is null)
                ctx.AppSettings.Add(settings);
            else
            {
                existing.ThemeMode = settings.ThemeMode;
                existing.Locale    = settings.Locale;
                existing.UserType  = settings.UserType;
            }

            _ = await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSettingsSaved(logger, settings.ThemeMode, settings.Locale);

            return new Result<AppSettings, ErrorResponse>.Ok(settings);
        }
        catch (Exception ex)
        {
            LogSettingsSaveFailed(logger, ex);

            return new Result<AppSettings, ErrorResponse>.Error(new ErrorResponse(ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "App settings loaded — ThemeMode={ThemeMode}, Locale={Locale}")]
    private static partial void LogSettingsLoaded(MelILogger logger, string themeMode, string locale);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load app settings")]
    private static partial void LogSettingsLoadFailed(MelILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "App settings saved — ThemeMode={ThemeMode}, Locale={Locale}")]
    private static partial void LogSettingsSaved(MelILogger logger, string themeMode, string locale);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to save app settings")]
    private static partial void LogSettingsSaveFailed(MelILogger logger, Exception ex);
}
