using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Hashes a local file with SHA-256 (#1399). The hash is local only: a scan compares it with the baseline hash, never with a provider hash.</summary>
public interface IContentHasher
{
    /// <summary>Hashes the file's content as a stream, so a large file is never held in memory whole.</summary>
    /// <param name="file">The file.</param>
    /// <param name="cancellationToken">Cancels the hash.</param>
    /// <returns>The lower-case hex SHA-256, or why the file could not be read. It never throws for a locked or vanished file.</returns>
    Task<Result<ContentHash, LocalScanFailure>> HashAsync(IFileInfo file, CancellationToken cancellationToken);
}
