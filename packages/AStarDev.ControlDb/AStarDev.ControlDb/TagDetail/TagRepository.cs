using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TagDetail;

/// <summary>The repository for <see cref="TagEntity"/> aggregates.</summary>
/// <param name="context">The context the aggregates are tracked by.</param>
internal sealed class TagRepository(ControlDbContext context) : IRepository<TagEntity, TagId>
{
    /// <inheritdoc/>
    public Task<Exceptional<Option<TagEntity>>> TryFindAsync(TagId key, CancellationToken cancellationToken = default) =>
        Try.RunAsync(async () => (Option<TagEntity>)await context.Tags.FirstOrDefaultAsync(tag => tag.Id == key, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc/>
    public Task<Exceptional<Option<TagEntity>>> TryGetFirstAsync(CancellationToken cancellationToken = default)
    => Try.RunAsync(async () => (Option<TagEntity>)await context.Tags.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false));

    /// <inheritdoc/>
    public Exceptional<TagEntity> Add(TagEntity aggregate) =>
        Try.Run(() =>
        {
            _ = context.Tags.Add(aggregate);
            return aggregate;
        });

    /// <inheritdoc/>
    public Exceptional<Unit> Delete(TagEntity aggregate) =>
        Try.Run(() =>
        {
            _ = context.Tags.Remove(aggregate);
            return Unit.Instance;
        });
}
