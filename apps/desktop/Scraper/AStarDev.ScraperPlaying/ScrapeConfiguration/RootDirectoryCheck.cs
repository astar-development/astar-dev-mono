using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class RootDirectoryCheck(IScopedRunner scopedRunner, IFileSystem fileSystem) : IRootDirectoryCheck
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        var rootDirectory = (await scopedRunner.RunAsync<IScrapeConfigurationLookup, Exceptional<Option<string>>>(lookup => lookup.TryGetRootDirectoryAsync(cancellationToken)))
            .Map(option => option.Match(directory => directory, () => throw new InvalidOperationException("Scrape configuration not found")))
            .GetOrThrow();

        return fileSystem.Directory.Exists(rootDirectory);
    }
}
