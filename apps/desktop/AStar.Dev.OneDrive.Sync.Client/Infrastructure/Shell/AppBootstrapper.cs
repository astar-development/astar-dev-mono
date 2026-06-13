using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AppBootstrapper(IDbContextFactory<AppDbContext> dbContextFactory, ISettingsService settingsService, IThemeService themeService, ILocalizationService loc, ISyncScheduler syncScheduler, MainWindowViewModel mainWindowViewModel, ILogger<AppBootstrapper> logger) : IAppBootstrapper
{
    /// <inheritdoc />
    public async Task BootstrapAsync(IProgress<string> progress, CancellationToken ct = default)
    {
        try
        {
            progress.Report("Migrating database…");
            await using var context = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            await context.Database.MigrateAsync(ct).ConfigureAwait(false);

            progress.Report("Loading settings…");
            await settingsService.LoadAsync().ConfigureAwait(false);

            progress.Report("Applying locale…");
            await loc.SetCultureAsync(new CultureInfo(settingsService.Current.Locale)).ConfigureAwait(false);

            progress.Report("Applying theme…");
            themeService.Apply(settingsService.Current.Theme);

            progress.Report("Initialising application…");
            await mainWindowViewModel.InitialiseAsync().ConfigureAwait(false);

            progress.Report("Starting sync scheduler…");
            _ = syncScheduler.StartSync(TimeSpan.FromMinutes(settingsService.Current.SyncIntervalMinutes))
                .Match(_ => true, error => throw new InvalidOperationException(error));
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.BootstrapFatal(logger, ex.Message, ex);
            throw;
        }
    }
}
