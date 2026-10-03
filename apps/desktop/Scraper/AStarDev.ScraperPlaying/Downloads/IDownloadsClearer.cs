using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Downloads;

/// <summary>Clears everything the scraper has downloaded: the file records and the files saved under the two root directories.</summary>
public interface IDownloadsClearer
{
    /// <summary>
    /// Removes every file record, then empties the configured base save directory and famous directory, keeping the directories themselves.
    /// The records are cleared first so a failure part way leaves untracked files rather than records pointing at missing files. Nothing else is touched.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>What was removed, or the failure.</returns>
    Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default);
}
