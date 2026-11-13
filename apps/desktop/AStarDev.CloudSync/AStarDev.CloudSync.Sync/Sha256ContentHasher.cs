using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class Sha256ContentHasher : IContentHasher
{
    /// <inheritdoc />
    public Task<Result<ContentHash, LocalScanFailure>> HashAsync(IFileInfo file, CancellationToken cancellationToken) => throw new NotImplementedException($"{file} {cancellationToken} are not yet hashed.");
}
