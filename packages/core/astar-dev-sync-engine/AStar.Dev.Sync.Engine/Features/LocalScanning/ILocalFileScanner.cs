namespace AStar.Dev.Sync.Engine.Features.LocalScanning;

/// <summary>
///     Scans a local directory tree and yields file paths suitable for upload (SE-15).
///     Skips symlinks, hardlinks, <c>.git</c> directories, and socket files.
///     Uses <c>IAsyncEnumerable</c> so the list is never fully materialised in memory (NF-03).
/// </summary>
public interface ILocalFileScanner
{
    /// <summary>
    ///     Enumerates all uploadable file paths under <paramref name="rootPath"/>.
    ///     Special files and <c>.git</c> folders are silently skipped (logged at <c>Debug</c>).
    /// </summary>
    IAsyncEnumerable<string> ScanAsync(string rootPath, CancellationToken ct = default);
}
