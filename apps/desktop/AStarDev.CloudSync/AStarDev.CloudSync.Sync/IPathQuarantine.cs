using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads which paths of an account a sync pass must not change. Every planner reads it once per pass and checks <see cref="QuarantineSnapshotExtensions.StateOf"/> before it plans a delete, a repair download or an upload.</summary>
public interface IPathQuarantine
{
    /// <summary>Reads the account's quarantined paths.</summary>
    /// <param name="accountId">The account the pass runs for.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The snapshot, or the error that stopped the read; a pass must not plan any change without it.</returns>
    Task<Result<QuarantineSnapshot, ErrorResponse>> ReadAsync(AccountId accountId, CancellationToken cancellationToken);
}
