using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>The reader used when no store is registered: there are no samples.</summary>
internal sealed class NoThroughputReader : IThroughputReader
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TransferSample>, ErrorResponse>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) => Task.FromResult(Result.Success<IReadOnlyList<TransferSample>, ErrorResponse>([]));
}
