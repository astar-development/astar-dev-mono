using AStarDev.ControlDb;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>A tags query over an in-memory list, whose results can be replaced by a failure.</summary>
internal sealed class FakeTagsQuery : ITagsQuery
{
    public List<TagEntity> Tags { get; } = [];

    public Option<Exception> Failure { get; set; } = Option.None<Exception>();

    public Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<IReadOnlyList<TagEntity>>(Tags.ToList()));

    public Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<IReadOnlyList<TagEntity>>(Tags.Where(tag => wallhavenTagIds.Contains(tag.WallhavenTagId)).ToList()));

    public Task<Exceptional<IReadOnlyList<TagFlagProjection>>> GetFlagsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<IReadOnlyList<TagFlagProjection>>(Tags.Select(tag => new TagFlagProjection(tag.WallhavenTagId, tag.IgnoreImage, tag.IsName, tag.IsFamous)).ToList()));

    private Exceptional<T> Result<T>(T value) => Failure is Option<Exception>.Some failure ? failure.Value : value;
}
