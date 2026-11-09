using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class FakeDefaultSyncRootAssigner : IDefaultSyncRootAssigner
{
    private readonly Lock gate = new();
    private readonly List<AccountId> assignedAccounts = [];

    public Func<Result<SyncRoot, SyncRootRejection>> Assign { get; init; } = DefaultRoot;

    public IReadOnlyList<AccountId> AssignedAccounts
    {
        get
        {
            lock (gate)
                return [.. assignedAccounts];
        }
    }

    public Task<Result<SyncRoot, SyncRootRejection>> AssignAsync(AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken)
    {
        lock (gate)
            assignedAccounts.Add(accountId);

        return Task.FromResult(Assign());
    }

    private static Result<SyncRoot, SyncRootRejection> DefaultRoot() => SyncRootFactory.Create(new MockFileSystem().DirectoryInfo.New("/home/test/CloudSync/OneDrive-Test"));
}
