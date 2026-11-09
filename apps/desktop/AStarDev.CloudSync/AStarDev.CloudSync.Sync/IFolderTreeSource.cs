using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Lists one level of an account's selection tree, merging the folders on disk (#1395) with the folders in the cloud (#1396) and judging each against the stored selection rules (#1397).</summary>
public interface IFolderTreeSource
{
    /// <summary>Lists the children of a folder, sorted by name ignoring case. Only that one level is read. A folder that exists on one side only is marked so. Each node's selection state comes from the stored rules, whatever the depth. If a side fails, the whole level fails and says which side; it is never listed as if that side were empty.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="parent">The folder whose children to list.</param>
    /// <param name="cancellationToken">Cancels the listing.</param>
    Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> GetChildrenAsync(AccountId accountId, FolderTreePosition parent, CancellationToken cancellationToken);
}
