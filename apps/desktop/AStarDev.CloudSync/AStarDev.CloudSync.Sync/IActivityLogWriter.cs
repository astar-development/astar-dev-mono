using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes the activity log (#1564). Each account keeps only its newest <see cref="ActivityLogLimits.EntriesPerAccount"/> entries.</summary>
public interface IActivityLogWriter
{
    /// <summary>Stores the entries, then drops each touched account's oldest entries beyond the cap. The whole call is one transaction.</summary>
    /// <param name="entries">The entries to store.</param>
    /// <param name="cancellationToken">Cancels the write; nothing is stored then.</param>
    /// <returns>Success, or the error that stopped the write; nothing is stored on failure.</returns>
    Task<Result<Unit, ErrorResponse>> AppendAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken);
}
