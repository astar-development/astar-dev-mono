using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class TagLinker(ITagsQuery tagsQuery, IUnitOfWork unitOfWork, IFileTagRepository fileTagRepository, TagFlagStore flagStore) : ITagLinker
{
    /// <summary>
    /// Caches resolved tags for the lifetime of this instance (one scrape run - <see cref="TagLinker"/> is
    /// Scoped). <see cref="PagesProcessor"/> batches <c>SaveChangesAsync</c> once per page, so without this,
    /// two different wallpapers on the same page introducing the same new tag would both miss
    /// <see cref="ITagsQuery.FindByWallhavenIdsAsync"/>'s database fast-path (the first one's insert isn't
    /// saved yet) and both try to add a duplicate <see cref="TagEntity"/>, failing the whole page's save on the
    /// unique-index violation.
    /// </summary>
    private readonly Dictionary<int, TagEntity> resolvedTags = [];

    /// <inheritdoc/>
    public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            var distinctTags = tags.DistinctBy(tag => tag.Id).ToList();
            await CacheExistingTagsAsync(distinctTags, cancellationToken);

            var added = new AddedRows();
            try
            {
                foreach (var tag in distinctTags)
                {
                    var tagEntity = ResolveOrAdd(tag, added);
                    added.Links.Add(fileTagRepository.Add(new FileTagEntity { FileId = fileId, TagId = tagEntity.Id })
                        .GetOrThrow());
                }
            }
            catch (Exception)
            {
                Rollback(added);

                throw;
            }

            return Unit.Instance;
        });

    private TagEntity ResolveOrAdd(Tag tag, AddedRows added)
    {
        if (resolvedTags.TryGetValue(tag.Id, out var existing)) return existing;

        var addedTag = unitOfWork.GetRepository<TagEntity, TagId>().Add(ToEntity(tag)).GetOrThrow();
        resolvedTags.Add(tag.Id, addedTag);
        added.Tags.Add(addedTag);

        return addedTag;
    }

    /// <summary>Removes what a failed link added, so nothing of it is left tracked for the page save: a link pointing at a file that is then discarded would fail that save, and a tag created for it would be stored unused. Links go first, as a tag cannot be removed while a link to it is tracked. Removing a row that was only added, never saved, cannot meaningfully fail, so the original failure is the one reported.</summary>
    private void Rollback(AddedRows added)
    {
        foreach (var link in added.Links)
        {
            _ = fileTagRepository.Delete(link);
        }

        foreach (var tag in added.Tags)
        {
            _ = unitOfWork.GetRepository<TagEntity, TagId>().Delete(tag);
            _ = resolvedTags.Remove(tag.WallhavenTagId);
        }
    }

    private static TagEntity ToEntity(Tag tag) => new()
    {
        Id = TagId.Empty,
        WallhavenTagId = tag.Id,
        Name = tag.Name,
        Alias = tag.Alias,
        CategoryId = tag.CategoryId,
        Category = tag.Category,
        Purity = tag.Purity,
        IsFamous = tag.IsFamous
    };

    /// <summary>Loads, in one query, only the uncached tags that were stored when the run started. A tag that was not stored cannot be in the database, so it needs no lookup.</summary>
    private async Task CacheExistingTagsAsync(IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> uncachedIds = [.. tags.Where(tag => !resolvedTags.ContainsKey(tag.Id) && (!flagStore.IsLoaded || flagStore.Cache.IsStored(tag.Id))).Select(tag => tag.Id)];
        if (uncachedIds.Count == 0) return;

        var existingTags = (await tagsQuery.FindByWallhavenIdsAsync(uncachedIds, cancellationToken))
            .GetOrThrow();

        foreach (var existingTag in existingTags)
        {
            resolvedTags[existingTag.WallhavenTagId] = existingTag;
        }
    }

    private sealed class AddedRows
    {
        public List<TagEntity> Tags { get; } = [];

        public List<FileTagEntity> Links { get; } = [];
    }
}
