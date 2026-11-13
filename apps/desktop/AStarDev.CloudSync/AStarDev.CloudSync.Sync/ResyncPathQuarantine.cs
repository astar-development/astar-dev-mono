using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reports <see cref="PathQuarantineState.ResyncInProgress"/> for every path of an account while the account's persisted resync pass runs (#1527).</summary>
public sealed class ResyncPathQuarantine(IRemoteIndexReader indexReader) : IPathQuarantine
{
    /// <inheritdoc />
    public Task<Result<QuarantineSnapshot, ErrorResponse>> ReadAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{indexReader.GetType().Name} {accountId} {cancellationToken} are not yet read.");
}
