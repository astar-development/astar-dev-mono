using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public static class SelectionTestRules
{
    public static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public static LocalRelativePath Path(string value) => LocalRelativePathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    public static SelectionRule Include(string path, SyncDirection? direction = null, string? remoteId = null) =>
        SelectionRuleFactory.Create(Account, Path(path), RemoteId(remoteId), SelectionRuleKindFactory.CreateInclude(direction ?? SyncDirectionFactory.CreateTwoWay()));

    public static SelectionRule Exclude(string path, string? remoteId = null) =>
        SelectionRuleFactory.Create(Account, Path(path), RemoteId(remoteId), SelectionRuleKindFactory.CreateExclude());

    public static SyncPairId PairAt(string path) => SyncPairIdFactory.CreateForRule(Account, Path(path));

    private static Option<RemoteItemId> RemoteId(string? value) => value is null ? Option.None<RemoteItemId>() : RemoteItemIdFactory.Create(value).Match(Option.Some, _ => Option.None<RemoteItemId>());
}
