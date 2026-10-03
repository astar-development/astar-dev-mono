using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

public interface ITagsQuery
{
    /// <summary>Finds, in a single query, the tags whose Wallhaven id is one of <paramref name="wallhavenTagIds"/>. Ids with no stored tag are simply absent from the result.</summary>
    /// <param name="wallhavenTagIds">The Wallhaven tag ids to look up.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default);

    /// <summary>Lists every stored tag, ordered by name.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets, in a single projection query, the flags of every stored tag.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagFlagProjection>>> GetFlagsAsync(CancellationToken cancellationToken = default);
}

public class TagsQuery(ControlDbContext context) : ITagsQuery
{
    public Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<TagEntity>>(async () => await context.Tags
                            .Where(tag => wallhavenTagIds.Contains(tag.WallhavenTagId))
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<TagEntity>>(async () => await context.Tags
                            .AsNoTracking()
                            .OrderBy(tag => tag.Name)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyList<TagFlagProjection>>> GetFlagsAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<TagFlagProjection>>(async () => await context.Tags
                            .AsNoTracking()
                            .Select(tag => new TagFlagProjection(tag.WallhavenTagId, tag.IgnoreImage, tag.IsName, tag.IsFamous))
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));
}
