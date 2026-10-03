using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

/// <summary>A scrape configuration lookup whose results are set by the test.</summary>
internal sealed class FakeScrapeConfigurationLookup : IScrapeConfigurationLookup
{
    /// <summary>The result of <see cref="ListHeadersAsync"/>.</summary>
    public Exceptional<IReadOnlyList<ScrapeConfigurationHeader>> Headers { get; set; } = Exceptional.Success<IReadOnlyList<ScrapeConfigurationHeader>>([]);

    /// <summary>The result of <see cref="TryGetRootDirectoryAsync"/>.</summary>
    public Exceptional<Option<string>> RootDirectory { get; set; } = Exceptional.Success(Option.None<string>());

    /// <summary>The result of <see cref="TryGetRootDirectoriesAsync"/>.</summary>
    public Exceptional<Option<RootDirectories>> RootDirectoriesResult { get; set; } = Exceptional.Success(Option.None<RootDirectories>());

    /// <summary>How many times <see cref="TryGetRootDirectoriesAsync"/> has been called.</summary>
    public int RootDirectoriesLookupCount { get; private set; }

    public Task<Exceptional<IReadOnlyList<ScrapeConfigurationHeader>>> ListHeadersAsync(CancellationToken cancellationToken = default) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<Exceptional<IReadOnlyList<ScrapeConfigurationHeader>>>(cancellationToken) : Task.FromResult(Headers);

    public Task<Exceptional<Option<string>>> TryGetRootDirectoryAsync(CancellationToken cancellationToken = default) => cancellationToken.IsCancellationRequested ? Task.FromCanceled<Exceptional<Option<string>>>(cancellationToken) : Task.FromResult(RootDirectory);

    public Task<Exceptional<Option<RootDirectories>>> TryGetRootDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        RootDirectoriesLookupCount++;

        return Task.FromResult(RootDirectoriesResult);
    }
}
