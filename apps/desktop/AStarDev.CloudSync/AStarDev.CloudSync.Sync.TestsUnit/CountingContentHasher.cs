using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class CountingContentHasher : IContentHasher
{
    private readonly Sha256ContentHasher inner = new();

    public int Calls { get; private set; }

    public HashSet<string> UnreadableNames { get; } = [];

    public Task<Result<ContentHash, LocalScanFailure>> HashAsync(IFileInfo file, CancellationToken cancellationToken)
    {
        Calls++;

        return UnreadableNames.Contains(file.Name)
            ? Task.FromResult(Result.Failure<ContentHash, LocalScanFailure>(LocalScanFailureFactory.CreateUnreadable()))
            : inner.HashAsync(file, cancellationToken);
    }
}
