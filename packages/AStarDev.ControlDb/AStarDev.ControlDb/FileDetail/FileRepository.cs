using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.FileDetail;

/// <summary>The repository for <see cref="FileEntity"/> aggregates, loaded through <paramref name="query"/> so their sub-entities are included.</summary>
/// <param name="context">The context the aggregates are tracked by.</param>
/// <param name="query">The query used to include the sub-entities of an aggregate.</param>
internal sealed class FileRepository(ControlDbContext context, IQuery<FileEntity> query) : IRepository<FileEntity, FileId>
{
    /// <inheritdoc/>
    public Task<Exceptional<Option<FileEntity>>> TryFindAsync(FileId key, CancellationToken cancellationToken = default) =>
        Try.RunAsync(async () => (Option<FileEntity>)await query.Apply(context.Files).FirstOrDefaultAsync(file => file.Id == key, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc/>
    public Task<Exceptional<Option<FileEntity>>> TryGetFirstAsync(CancellationToken cancellationToken = default)
    => Try.RunAsync(async () => (Option<FileEntity>)await query.Apply(context.Files).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false));

    /// <inheritdoc/>
    public Exceptional<FileEntity> Add(FileEntity aggregate) =>
        Try.Run(() =>
        {
            _ = context.Files.Add(aggregate);
            return aggregate;
        });

    /// <inheritdoc/>
    public Exceptional<Unit> Delete(FileEntity aggregate) =>
        Try.Run(() =>
        {
            _ = context.Files.Remove(aggregate);
            return Unit.Instance;
        });
}
