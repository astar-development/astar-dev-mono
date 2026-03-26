using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Sync.Engine;

/// <summary>
/// Orchestrates bi-directional file sync for a single account (SE-01).
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Runs a full sync cycle for the given account: detects changes,
    /// applies concurrency limits (SE-02), and transfers files in both directions (SE-01).
    /// </summary>
    /// <param name="accountId">The account to sync.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the sync report on success, or an error message on failure.</returns>
    Task<Result<SyncReport, ErrorResponse>> SyncAsync(string accountId, CancellationToken ct = default);
}
