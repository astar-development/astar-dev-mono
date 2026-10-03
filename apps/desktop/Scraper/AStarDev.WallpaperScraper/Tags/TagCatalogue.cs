using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scoping;

namespace AStarDev.WallpaperScraper.Tags;

/// <inheritdoc/>
public sealed class TagCatalogue(IScopedRunner scopedRunner) : ITagCatalogue
{
    /// <inheritdoc/>
    public async Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default) =>
        await scopedRunner.RunAsync<ITagsQuery, Exceptional<IReadOnlyList<TagSummary>>>(async query =>
            (await query.ListAsync(cancellationToken)).Match(
                tags => (Exceptional<IReadOnlyList<TagSummary>>)new Success<IReadOnlyList<TagSummary>>([.. tags.Select(TagSummary.From)]),
                exception => exception));

    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default)
    {
        if (flagsByWallhavenId.Count == 0) return Unit.Instance;

        return await scopedRunner.RunAsync<ITagsQuery, IUnitOfWork, IIgnoredWallpapers, Exceptional<Unit>>((query, unitOfWork, ignoredWallpapers) => Try.RunAsync(() => unitOfWork.InTransactionAsync(async () =>
        {
            var tags = (await query.FindByWallhavenIdsAsync([.. flagsByWallhavenId.Keys], cancellationToken)).GetOrThrow();
            var ignoreFlagRemoved = false;
            foreach (var tag in tags)
            {
                var flags = flagsByWallhavenId[tag.WallhavenTagId];
                ignoreFlagRemoved |= tag.IgnoreImage && !flags.IgnoreImage;
                tag.IgnoreImage = flags.IgnoreImage;
                tag.IsName = flags.IsName;
                tag.IsFamous = flags.IsFamous;
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            // A wallpaper remembered as ignored may have been ignored for a tag that is no longer flagged, so every one is evaluated again on the next scrape.
            // A tag newly flagged to ignore cannot make a remembered wallpaper eligible, so that change leaves them alone.
            // Forgetting shares the transaction with the flag save: if it fails, the flags are rolled back and the user can save again.
            if (ignoreFlagRemoved) _ = (await ignoredWallpapers.ForgetAllAsync(cancellationToken)).GetOrThrow();

            return Unit.Instance;
        }, cancellationToken)));
    }
}
