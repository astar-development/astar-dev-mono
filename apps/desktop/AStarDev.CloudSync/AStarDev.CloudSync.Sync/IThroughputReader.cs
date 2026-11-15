using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the transfer samples (#1767).</summary>
public interface IThroughputReader
{
    /// <summary>Reads the samples that ended at or after <paramref name="since"/>, oldest first.</summary>
    /// <param name="since">The earliest end time to return.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The samples, or the error that stopped the read.</returns>
    Task<Result<IReadOnlyList<TransferSample>, ErrorResponse>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
