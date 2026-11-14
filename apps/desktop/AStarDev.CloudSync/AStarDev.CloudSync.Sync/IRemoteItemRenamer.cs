using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Renames a remote item in place, in the same folder (#1615). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteItemRenamer
{
    /// <summary>Gives the item a new name. It never replaces an item that already has that name.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="itemId">The remote item.</param>
    /// <param name="newName">The new name, without a path.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or <see cref="ProviderError.NameConflict"/> when the name is taken.</returns>
    Task<Result<Unit, ProviderError>> RenameAsync(AccountId accountId, RemoteItemId itemId, string newName, CancellationToken cancellationToken);
}
