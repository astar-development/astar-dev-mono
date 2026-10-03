using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>The repository for <see cref="ScrapeConfigurationEntity"/> aggregates, loaded through <paramref name="query"/> so their sub-entities are included.</summary>
/// <param name="context">The context the aggregates are tracked by.</param>
/// <param name="query">The query used to include the sub-entities of an aggregate.</param>
internal sealed class ScrapeConfigurationRepository(ControlDbContext context, IQuery<ScrapeConfigurationEntity> query) : IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>
{
    /// <inheritdoc/>
    public Task<Exceptional<Option<ScrapeConfigurationEntity>>> TryFindAsync(ScrapeConfigurationId key, CancellationToken cancellationToken = default) =>
        Try.RunAsync(async () => (Option<ScrapeConfigurationEntity>)await query.Apply(context.ScrapeConfigurations).FirstOrDefaultAsync(scrapeConfiguration => scrapeConfiguration.Id == key, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc/>
    public Task<Exceptional<Option<ScrapeConfigurationEntity>>> TryGetFirstAsync(CancellationToken cancellationToken = default)
    => Try.RunAsync(async () => (Option<ScrapeConfigurationEntity>)await query.Apply(context.ScrapeConfigurations).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false));

    /// <inheritdoc/>
    public Exceptional<ScrapeConfigurationEntity> Add(ScrapeConfigurationEntity aggregate) =>
        Try.Run(() =>
        {
            _ = context.ScrapeConfigurations.Add(aggregate);
            return aggregate;
        });

    /// <inheritdoc/>
    public Exceptional<Unit> Delete(ScrapeConfigurationEntity aggregate) =>
        Try.Run(() =>
        {
            _ = context.ScrapeConfigurations.Remove(aggregate);
            return Unit.Instance;
        });
}
