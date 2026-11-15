using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes the transfer samples (#1767). Samples older than <see cref="ThroughputLimits.Retention"/> before the newest are dropped.</summary>
public interface IThroughputWriter
{
    /// <summary>Stores the sample, then drops the samples that have aged out.</summary>
    /// <param name="sample">The sample to store.</param>
    /// <param name="cancellationToken">Cancels the write; nothing is stored then.</param>
    /// <returns>Success, or the error that stopped the write.</returns>
    Task<Result<Unit, ErrorResponse>> AppendAsync(TransferSample sample, CancellationToken cancellationToken);
}
