using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeFolderTreeSource : IFolderTreeSource
{
    private readonly List<FolderTreePosition> requested = [];
    private readonly List<AccountId> requestedAccounts = [];

    public Func<FolderTreePosition, Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> Levels { get; set; } = _ => Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>([]);

    public IReadOnlyList<FolderTreePosition> Requested => requested;

    public IReadOnlyList<AccountId> RequestedAccounts => requestedAccounts;

    public Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> GetChildrenAsync(AccountId accountId, FolderTreePosition parent, CancellationToken cancellationToken)
    {
        requested.Add(parent);
        requestedAccounts.Add(accountId);

        return Task.FromResult(Levels(parent));
    }
}
