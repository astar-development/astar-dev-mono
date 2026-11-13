using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates remote folders (#1401). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteFolderCreator
{
    /// <summary>Returns the named child folder, creating it if needed. Idempotent: an existing folder, a "name already exists" or a 412 race returns the folder, not a failure.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="parent">The parent folder, or none for the root of the drive.</param>
    /// <param name="name">The folder name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<Result<RemoteFolder, ProviderError>> EnsureFolderAsync(AccountId accountId, Option<RemoteItemId> parent, string name, CancellationToken cancellationToken);
}
