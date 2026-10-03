using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.Downloads;

/// <inheritdoc/>
public sealed class DownloadsClearer(IScopedRunner scopedRunner, IFileSystem fileSystem) : IDownloadsClearer
{
    /// <inheritdoc/>
    public async Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default)
    {
        return await scopedRunner.RunAsync<IScrapeConfigurationLookup, IFileDetailsClearer, Exceptional<ClearedDownloads>>((lookup, detailsClearer) => Try.RunAsync(async () =>
        {
            var directories = (await lookup.TryGetRootDirectoriesAsync(cancellationToken))
                .GetOrThrow()
                .Match(found => found, () => throw new InvalidOperationException("No scrape configuration exists."));
            var fileRecords = (await detailsClearer.ClearAsync(cancellationToken)).GetOrThrow();

            // The records go first on purpose: a failure after that leaves files without records, which the next scrape simply downloads again.
            // The other order could leave records without files, and a scrape skips every wallpaper it has a record for, so those files would never come back.
            try
            {
                EmptyDirectories(directories);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new IOException($"File records cleared: {fileRecords}, but the download directories could not be emptied: {e.Message}", e);
            }

            return new ClearedDownloads(fileRecords);
        }));
    }

    private void EmptyDirectories(RootDirectories directories)
    {
        fileSystem.EmptyDirectory(directories.Root);
        fileSystem.EmptyDirectory(directories.FamousRoot);
        _ = fileSystem.Directory.CreateDirectory(directories.Root);
        _ = fileSystem.Directory.CreateDirectory(directories.FamousRoot);
    }
}
