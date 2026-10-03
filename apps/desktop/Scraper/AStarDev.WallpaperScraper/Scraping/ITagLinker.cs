using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Interface for linking tags to a file, storing each distinct tag only once.</summary>
public interface ITagLinker
{
    /// <summary>Links each of the tags to <paramref name="fileId"/>, creating any tag not already stored.</summary>
    /// <param name="fileId">The id of the already-persisted <see cref="FileEntity"/> to link the tags to.</param>
    /// <param name="tags">The tags to link, as returned by <see cref="ITagFetcher.FetchTagsAsync"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> indicating success or the captured failure.</returns>
    Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken);
}
