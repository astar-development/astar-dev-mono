using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Removes remote items (#1401, #1405). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteItemRemover
{
    /// <summary>Removes the item if <paramref name="precondition"/> holds.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="itemId">The remote item.</param>
    /// <param name="precondition">What must be true remotely; otherwise <see cref="ProviderError.PreconditionFailed"/>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<Unit, ProviderError>> RemoveAsync(AccountId accountId, RemoteItemId itemId, RemovePrecondition precondition, CancellationToken cancellationToken);
}
