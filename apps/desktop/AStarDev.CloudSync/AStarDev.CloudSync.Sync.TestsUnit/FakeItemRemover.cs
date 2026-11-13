using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeItemRemover : IRemoteItemRemover
{
    private readonly Lock gate = new();
    private readonly List<(RemoteItemId ItemId, RemovePrecondition Precondition)> calls = [];

    public Action OnRemove { get; set; } = () => { };

    public Result<Unit, ProviderError> Outcome { get; set; } = Result.Success<Unit, ProviderError>(Unit.Instance);

    public IReadOnlyList<(RemoteItemId ItemId, RemovePrecondition Precondition)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public Task<Result<Unit, ProviderError>> RemoveAsync(AccountId accountId, RemoteItemId itemId, RemovePrecondition precondition, CancellationToken cancellationToken)
    {
        OnRemove();
        lock (gate)
            calls.Add((itemId, precondition));

        return Task.FromResult(Outcome);
    }
}
