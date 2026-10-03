using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Tags;

/// <summary>Reads the stored tags so they can be edited, and saves the edits.</summary>
public interface ITagCatalogue
{
    /// <summary>Lists a summary of every stored tag, ordered by name.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets the flags of each named tag, leaving every other tag as it is.</summary>
    /// <param name="flagsByWallhavenId">The new flags for each changed tag, keyed by its Wallhaven id.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default);
}
