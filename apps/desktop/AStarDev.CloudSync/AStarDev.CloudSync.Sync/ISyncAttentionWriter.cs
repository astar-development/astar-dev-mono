using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes the attention queue (#1405). The whole call is one transaction, so a crash leaves no partial rows.</summary>
public interface ISyncAttentionWriter
{
    /// <summary>Stores the items as an upsert. A conflict at a path the account already has updates that row's kind and snapshots and keeps its id and first detection time; a held set for a pair and direction the account already has replaces that row's paths.</summary>
    /// <param name="items">The items to store.</param>
    /// <param name="cancellationToken">Cancels the write; nothing is stored then.</param>
    /// <returns>Success, or the error that stopped the write; nothing is stored on failure.</returns>
    Task<Result<Unit, ErrorResponse>> RecordAsync(IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken);
}
