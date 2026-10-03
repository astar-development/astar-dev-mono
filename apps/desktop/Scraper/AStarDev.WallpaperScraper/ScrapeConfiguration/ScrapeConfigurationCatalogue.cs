using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scoping;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationCatalogue(IScopedRunner scopedRunner) : IScrapeConfigurationCatalogue
{
    /// <inheritdoc/>
    public async Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync(CancellationToken cancellationToken = default) =>
        (await scopedRunner.RunAsync<IScrapeConfigurationLookup, Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>>(async lookup =>
            (await lookup.ListHeadersAsync(cancellationToken)).Match(
                headers => (Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>)new Success<IReadOnlyList<ScrapeConfigurationSummary>>([.. headers.Select(ScrapeConfigurationSummary.From)]),
                exception => exception)));

    /// <inheritdoc/>
    public Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id, CancellationToken cancellationToken = default) =>
        scopedRunner.RunAsync<IUnitOfWork, Exceptional<Option<ScrapeConfigurationEntity>>>(unitOfWork =>
            unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryFindAsync(id, cancellationToken));
}
