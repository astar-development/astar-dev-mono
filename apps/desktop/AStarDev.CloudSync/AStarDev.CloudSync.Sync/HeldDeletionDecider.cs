using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class HeldDeletionDecider : IHeldDeletionDecider
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> ConfirmAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{nameof(ConfirmAsync)} {accountId} {id} {cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RestoreAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{nameof(RestoreAsync)} {accountId} {id} {cancellationToken}");
}
