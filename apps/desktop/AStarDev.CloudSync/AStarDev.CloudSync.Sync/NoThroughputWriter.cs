using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The writer used when no store is registered: samples are dropped.</summary>
internal sealed class NoThroughputWriter : IThroughputWriter
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> AppendAsync(TransferSample sample, CancellationToken cancellationToken) => Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
}
