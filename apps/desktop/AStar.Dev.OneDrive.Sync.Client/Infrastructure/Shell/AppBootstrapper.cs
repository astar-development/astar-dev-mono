using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AppBootstrapper(IDbContextFactory<AppDbContext> dbContextFactory, ISettingsService settingsService, IThemeService themeService, ISyncScheduler syncScheduler, MainWindowViewModel mainWindowViewModel) : IAppBootstrapper
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

            progress.Report("Applying theme…");
            themeService.Apply(settingsService.Current.Theme);

            progress.Report("Initialising application…");
            await mainWindowViewModel.InitialiseAsync().ConfigureAwait(false);

            progress.Report("Starting sync scheduler…");
            syncScheduler.StartSync(TimeSpan.FromMinutes(settingsService.Current.SyncIntervalMinutes));
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal(ex, "[AppBootstrapper] Fatal error during bootstrap: {Message}", ex.Message);
            throw;
        }
    }
}
