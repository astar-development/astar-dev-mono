using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationUpdater(IScopedRunner scopedRunner, TimeProvider timeProvider) : IScrapeConfigurationUpdater
{
    /// <inheritdoc/>
    public async Task<Exceptional<Option<Unit>>> SaveAsync(ScrapeConfigurationId id, IReadOnlyList<IScrapeConfigurationSectionEdit> edits, CancellationToken cancellationToken)
    {
        return await scopedRunner.RunAsync<IUnitOfWork, Exceptional<Option<Unit>>>(unitOfWork => Try.RunAsync(async () =>
        {
            var found = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryFindAsync(id, cancellationToken)).GetOrThrow();
            if (found is not Option<ScrapeConfigurationEntity>.Some some) return Option.None<Unit>();

            var now = timeProvider.GetUtcNow();
            foreach (var edit in edits) edit.ApplyTo(some.Value, now);

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Option.Some(Unit.Instance);
        }));
    }
}
