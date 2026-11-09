using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Saves the sync root of an account (#1394).</summary>
public interface ISyncRootWriter
{
    /// <summary>Checks the candidate against every other account's root and the reserved folders, and saves it, as one step. Two changes that would cross each other can never both succeed: the later one is rejected. A rejected change leaves the stored root unchanged.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="candidate">The folder to use as the account's root.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The saved root, or the reason it was rejected.</returns>
    Task<Result<SyncRoot, SyncRootRejection>> SetAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken);
}
