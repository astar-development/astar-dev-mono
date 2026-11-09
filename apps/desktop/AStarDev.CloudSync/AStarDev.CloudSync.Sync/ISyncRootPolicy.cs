using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides whether a folder may be an account's sync root: no account's root may be the same as, inside or contain another's (#1394).</summary>
public interface ISyncRootPolicy
{
    /// <summary>Checks a candidate root. The account's own current root is ignored, so it can be re-saved or replaced by a sub-folder of itself. Symlinks are resolved where the file system reports them, otherwise the check is best effort.</summary>
    /// <param name="accountId">The account the root is for.</param>
    /// <param name="candidate">The folder to check.</param>
    /// <param name="existing">Every account's current root.</param>
    /// <returns>The normalised root, or the reason it is rejected, naming the clashing account.</returns>
    Result<SyncRoot, SyncRootRejection> Check(AccountId accountId, IDirectoryInfo candidate, IReadOnlyList<AccountSyncRoot> existing);
}
