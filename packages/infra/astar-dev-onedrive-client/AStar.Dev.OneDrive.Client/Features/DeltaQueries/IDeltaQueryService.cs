using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Client.Features.DeltaQueries;

/// <summary>
///     Executes Graph delta queries for incremental OneDrive sync (SE-09, SE-10, SE-12).
///     All methods return <see cref="Result{TSuccess,TError}"/> — callers never see Graph exceptions.
/// </summary>
public interface IDeltaQueryService
{
    /// <summary>
    ///     Fetches all changed items for <paramref name="folderId"/> since the last sync.
    ///     Pass <see langword="null"/> for <paramref name="deltaToken"/> to perform a full (initial) sync.
    ///     The returned <see cref="DeltaQueryResult.NextDeltaToken"/> must be persisted for the next call.
    /// </summary>
    /// <returns>
    ///     <see cref="DeltaTokenExpiredError"/> when the token has expired (HTTP 410).
    ///     <see cref="DeltaQueryThrottledError"/> when Graph API throttles after exhausting retries (HTTP 429).
    ///     <see cref="DeltaQueryFailedError"/> for any other Graph failure.
    /// </returns>
    Task<Result<DeltaQueryResult, DeltaQueryError>> GetDeltaAsync(string accessToken, string folderId, string? deltaToken, CancellationToken ct = default);
}
