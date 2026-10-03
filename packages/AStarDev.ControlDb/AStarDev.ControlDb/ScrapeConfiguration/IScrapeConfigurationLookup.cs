using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>Read-only lookups over scrape configurations that project just the fields needed, instead of loading each configuration's whole graph of related entities.</summary>
public interface IScrapeConfigurationLookup
{
    /// <summary>Lists a header for every scrape configuration.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<ScrapeConfigurationHeader>>> ListHeadersAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the root scrape directory of the (first) scrape configuration.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>The root directory, or none when there is no scrape configuration.</returns>
    Task<Exceptional<Option<string>>> TryGetRootDirectoryAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the root and famous root scrape directories of the (first) scrape configuration.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>The root directories, or none when there is no scrape configuration.</returns>
    Task<Exceptional<Option<RootDirectories>>> TryGetRootDirectoriesAsync(CancellationToken cancellationToken = default);
}
