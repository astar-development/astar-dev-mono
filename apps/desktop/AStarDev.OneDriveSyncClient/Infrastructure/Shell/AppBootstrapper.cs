using System.Globalization;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <inheritdoc />
public sealed class AppBootstrapper(IDbContextFactory<AppDbContext> dbContextFactory, ISettingsService settingsService, IThemeService themeService, ILocalizationService loc, ISyncScheduler syncScheduler, MainWindowViewModel mainWindowViewModel, IPersonCategoryCleanupService personCategoryCleanupService, IDefaultCategorySeedingService defaultCategorySeedingService, ILogger<AppBootstrapper> logger) : IAppBootstrapper
{
    /// <inheritdoc />
    public async Task BootstrapAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            progress.Report("Migrating database…");
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

            int removedCategories = await personCategoryCleanupService.RemoveInvalidPersonCategoriesAsync(cancellationToken)
                .MatchAsync(removed => removed, error =>
                {
                    OneDriveSyncClientMessages.PersonCategoryCleanupFailed(logger, error);

                    return 0;
                })
                .ConfigureAwait(false);

            if (removedCategories > 0)
                OneDriveSyncClientMessages.PersonCategoryCleanupCompleted(logger, removedCategories);

            int seededCategories = await defaultCategorySeedingService.SeedDefaultCategoriesAsync(cancellationToken)
                .MatchAsync(seeded => seeded, error =>
                {
                    OneDriveSyncClientMessages.DefaultCategorySeedingFailed(logger, error);

                    return 0;
                })
                .ConfigureAwait(false);

            if (seededCategories > 0)
                OneDriveSyncClientMessages.DefaultCategoriesSeeded(logger, seededCategories);

            progress.Report("Loading settings…");
            await settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);

            progress.Report("Applying locale…");
            await loc.SetCultureAsync(new CultureInfo(settingsService.Current.Locale), cancellationToken).ConfigureAwait(false);

            progress.Report("Applying theme…");
            themeService.Apply(settingsService.Current.Theme);

            progress.Report("Initialising application…");
            await mainWindowViewModel.InitialiseAsync().ConfigureAwait(false);

            progress.Report("Starting sync scheduler…");
            _ = await syncScheduler.StartSync(TimeSpan.FromMinutes(settingsService.Current.SyncIntervalMinutes))
                .MatchAsync(_ => true, error => throw new InvalidOperationException(error));
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.BootstrapFatal(logger, ex.Message, ex);
            throw;
        }
    }
}
