using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeItemRenamer : IRemoteItemRenamer
{
    private readonly Lock gate = new();
    private readonly List<(RemoteItemId ItemId, string NewName)> calls = [];

    public Result<Unit, ProviderError> Outcome { get; set; } = Result.Success<Unit, ProviderError>(Unit.Instance);

    public WriteGate? Gate { get; set; }

    public IReadOnlyList<(RemoteItemId ItemId, string NewName)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public async Task<Result<Unit, ProviderError>> RenameAsync(AccountId accountId, RemoteItemId itemId, string newName, CancellationToken cancellationToken)
    {
        lock (gate)
            calls.Add((itemId, newName));

        if (Gate is { } waiting)
            await waiting.PassAsync().WaitAsync(cancellationToken);

        return Outcome;
    }
}
