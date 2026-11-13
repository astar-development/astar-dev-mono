using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Closes items of the attention queue once the user has settled them (#1406).</summary>
public interface ISyncAttentionCloser
{
    /// <summary>Removes a conflict. Closing one that is already gone succeeds, so a repeated click is harmless.</summary>
    /// <param name="id">The conflict.</param>
    /// <param name="resolution">How the user settled it; logged.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the error that stopped the write.</returns>
    Task<Result<Unit, ErrorResponse>> CloseAsync(ConflictId id, AttentionResolution resolution, CancellationToken cancellationToken);

    /// <summary>Removes a held set. Closing one that is already gone succeeds.</summary>
    /// <param name="id">The held set.</param>
    /// <param name="resolution">How the user settled it; logged.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the error that stopped the write.</returns>
    Task<Result<Unit, ErrorResponse>> CloseAsync(HeldDeletionId id, AttentionResolution resolution, CancellationToken cancellationToken);
}
