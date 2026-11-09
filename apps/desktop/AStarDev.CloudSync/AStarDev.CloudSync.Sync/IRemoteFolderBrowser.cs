using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Lists remote folders for the folder tree (#1396, #1397). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteFolderBrowser
{
    /// <summary>Lists the child folders of a folder, following every page.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="parent">The folder to list; none for the drive root.</param>
    /// <param name="cancellationToken">Cancels the listing.</param>
    Task<Result<IReadOnlyList<RemoteFolder>, ProviderError>> GetChildFoldersAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken);
}
