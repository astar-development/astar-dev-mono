
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scoping;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Replaces the stored scrape configuration settings aggregate with an imported document.</summary>
/// <param name="scopedRunner">Runs each import in its own scope, with its own unit of work.</param>
/// <param name="timeProvider">The source of the time recorded as the created and modified time of the imported categories.</param>
public sealed class ScrapeConfigurationImporter(IScopedRunner scopedRunner, TimeProvider timeProvider) : IScrapeConfigurationImporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document, CancellationToken cancellationToken = default)
    {
        return await scopedRunner.RunAsync<IUnitOfWork, Exceptional<Unit>>(unitOfWork => Try.RunAsync(async () =>
        {
            var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

            // Map first: an unmappable document must not cost the user their current configuration.
            var replacement = document.ToEntity(timeProvider.GetUtcNow());
            var current = (await dbContext.TryGetFirstAsync(cancellationToken)).GetOrThrow();

            // Exports leave the API keys out by default, so a file without them must not blank the keys already stored.
            _ = current.Match(existing => KeepStoredApiKeysWhereFileHasNone(replacement, existing), () => Unit.Instance);

            return await ReplaceAsync(new ImportTransaction(unitOfWork, dbContext, cancellationToken), current, replacement);
        }));
    }

    // Delete and add commit together, so a failure while saving the replacement leaves the current configuration in place.
    private static Task<Unit> ReplaceAsync(ImportTransaction transaction, Option<ScrapeConfigurationEntity> current, ScrapeConfigurationEntity replacement)
        => transaction.UnitOfWork.InTransactionAsync(async () =>
        {
            await current.MatchAsync(
                async existing =>
                {
                    _ = transaction.Repository.Delete(existing).GetOrThrow();
                    _ = await transaction.UnitOfWork.SaveChangesAsync(transaction.CancellationToken);
                },
                () => { });

            _ = transaction.Repository.Add(replacement).GetOrThrow();
            _ = await transaction.UnitOfWork.SaveChangesAsync(transaction.CancellationToken);

            return Unit.Instance;
        }, transaction.CancellationToken);

    private static Unit KeepStoredApiKeysWhereFileHasNone(ScrapeConfigurationEntity replacement, ScrapeConfigurationEntity existing)
    {
        if (string.IsNullOrEmpty(replacement.ApiKey)) replacement.ApiKey = existing.ApiKey;
        if (string.IsNullOrEmpty(replacement.UserConfiguration.ApiKey)) replacement.UserConfiguration.ApiKey = existing.UserConfiguration.ApiKey;

        return Unit.Instance;
    }

    /// <summary>What one import runs against: the unit of work its changes commit through, the repository it changes and the token that cancels it.</summary>
    private sealed record ImportTransaction(IUnitOfWork UnitOfWork, IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> Repository, CancellationToken CancellationToken);
}
